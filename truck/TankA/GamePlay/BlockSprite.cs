using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TankA.Network;

namespace TankA.GamePlay
{
    public struct BlockSpriteState
    {
        public BlockSpriteType type;
        public int? health;
        public int? maxHealth;
        public Point mapPoint;
    }

    public enum BlockSpriteType
    {
        Wall,
        FireWall,
        Invulnerable,
        Invisible,
    }

    class BlockSprite : Sprite
    {
        // Constructors
        public BlockSprite(SpriteInfo spriteInfo, BlockSpriteState state, SpriteDestroyedHandler handlers)
            : base(spriteInfo)
        {
            this.position = new Vector2(state.mapPoint.X * Map.CellSize, state.mapPoint.Y * Map.CellSize);
            this.type = state.type;
            this.maxHealth = state.maxHealth;
            this.Health = state.health;
            layer = Map.Layer1 - 0.0001f * state.mapPoint.Y + 0.01f;
            if (type != BlockSpriteType.Invisible)
                blockAmmo = true;
            hasBeenDestroyed = handlers;
        }

        // Public methods
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(this.Texture, position, this.SourceRectangle, Color.White,
                0, Vector2.Zero, 1, SpriteEffects.None, layer);
        }
        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            hasBeenDestroyed(this);
        }
        // Private members
        readonly bool blockAmmo;
        readonly Vector2 position;
        readonly BlockSpriteType type;
        readonly int? maxHealth;
        readonly float layer;
        SpriteDestroyedHandler hasBeenDestroyed;
        // Properties
        public bool BlockAmmo
        {
            get { return blockAmmo; }
        }
        public override int? Health
        {
            get { return base.Health; }

            set
            {
                base.Health = value;
                if (value == null)
                    return;
                // My health has changed. Notify all clients.
                var host = (ITankAHost)TankAGame.ThisGame.Services.GetService(typeof(ITankAHost));
                if (host != null)
                    host.AckHealthChanged(this, (int)value);

                float r = (float)base.Health / (int)maxHealth;
                if (r < 0.3)
                    SetSheetRow(2);
                else if (r < 0.6)
                    SetSheetRow(1);
            }

        }
        public Point MapPoint
        {
            get
            {
                return new Point((int)position.X / Map.CellSize, (int)position.Y / Map.CellSize);
            }
        }
        public BlockSpriteType Type
        {
            get { return type; }
        }
        public BlockSpriteState State
        {
            get
            {
                var ret = new BlockSpriteState()
                {
                    type = type,
                    health = base.Health,
                    maxHealth = maxHealth,
                    mapPoint = MapPoint
                };
                return ret;
            }
        }
    }
}
