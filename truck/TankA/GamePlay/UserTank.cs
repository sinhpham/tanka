using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TankA.Network;

namespace TankA.GamePlay
{
    class UserTank : Tank
    {
        // Constructors
        public UserTank(SpriteInfo si, Texture2D gunTexture, TankState ts, SpriteDestroyedHandler handler)
            : base(si, ts, handler)
        {
            this.gunTexture = gunTexture;
            this.playerIndex = ts.playerIndex;
        }
        // Public methods
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            spriteBatch.Draw(gunTexture, position, this.SourceRectangle, Color.White,
                0, Origin, 1, SpriteEffects.None, (float)(Map.Layer1 - 0.01));
            if (playerIndex == 0)
            {
                spriteBatch.Draw(Factory.TankMainHealthBar(), new Vector2(10, 10), mainHealthBarRectangle,
                    Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, Map.Layer2);

                spriteBatch.Draw(Factory.TankHPBackground(), new Vector2(10, 0), null, Color.White,
                    0, Vector2.Zero, 1, SpriteEffects.None, Map.Layer1);
                
                spriteBatch.DrawString(Factory.HealthFont(), healthString, new Vector2(200, 20), healthStringColor,
                    0, Vector2.Zero, 1, SpriteEffects.None, Map.Layer2);
                

                spriteBatch.Draw(Factory.MyselfColor(), this.Position, null, Color.White,
                    0, new Vector2(30, 30), 1, SpriteEffects.None, Map.Layer0 - 0.0002f);
            }
        }
        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            var gl = (IGameLogic)TankAGame.ThisGame.Services.GetService(typeof(IGameLogic));
            gl.AckUserTankDestroyed(TankIndex);
        }
        // Private members
        readonly Texture2D gunTexture;
        readonly byte playerIndex;
        // Default values for user tank.
        static readonly TankState defaultUserTankState = new TankState()
        {
            tankInfo = new TankInfo()
            {
                tankType = TankType.Body1,
                realSize = new Point(50, 50),
                gunType = GunType.Gun1,
                gunDirection = Direction.Up,
                health = 100,
                armor = 5,
                speed = 3,
                timeBetweenFires = 1000,
                ammoInfo = Ammo.DefaultAmmoInfo(AmmoType.Basic),
                invulnerableTime = 3000,
            },
            playerIndex = 0,
            tankIndex = 0xff,
        };
        // Properties
        public override TankState State
        {
            get
            {
                TankState ret = base.State;
                ret.playerIndex = playerIndex;
                return ret;
            }
        }
        public byte PlayerIndex
        {
            get { return playerIndex; }
        }
        static public TankState DefaultUserTankState
        {
            get { return defaultUserTankState; }
        }
    }
}
