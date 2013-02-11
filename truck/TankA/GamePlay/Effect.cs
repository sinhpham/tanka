using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TankA.GamePlay
{
    public struct EffectState
    {
        public EffectType type;
        public Vector2 position;
        public Point startFrame;
        public Point currentFrame;
        public Point endFrame;
        public byte attachedTankIndex;
        public int lastingTime;
        public bool oldEffect;
    }

    public enum EffectType
    {
        TankDestroyed,
        TankFire,
        TankInvulnerable,
        II_IncreaseTankSpeed,
        II_IncreaseAmmoSpeed,
        II_IncreaseArmor,
        II_DecreaseTBF,
    }

    class Effect : Sprite
    {
        // Constructors
        static Effect()
        {
            normalScale.Add(EffectType.TankDestroyed);
            normalScale.Add(EffectType.TankFire);
            normalScale.Add(EffectType.TankInvulnerable);
        }
        public Effect(SpriteInfo si, EffectState state, SpriteDestroyedHandler handler)
            : base(si)
        {
            this.type = state.type;
            this.position = state.position;
            this.startFrame = state.startFrame;
            this.endFrame = state.endFrame;
            this.attachedTankIndex = state.attachedTankIndex;
            scale = 1;
            if (!normalScale.Contains(type))
                scale = 0.6f;
            
            origin.X = (int)(si.frameSize.X / 2);
            origin.Y = (int)(si.frameSize.Y / 2);
            lastingTime = state.lastingTime;
            if (lastingTime == 0)
                oneTimeEffect = true;
            if (!state.oldEffect)
                SetFrame(state.startFrame);
            else
                SetFrame(state.currentFrame);
            hasBeenDestroyed = handler;
        }
        public void Update(GameTime gameTime, Dictionary<byte, Tank> tanks)
        {
            if (attachedTankIndex != 0xff)
            {
                if (!tanks.ContainsKey(attachedTankIndex))
                {
                    OnDestroyed();
                    return;
                }
                position = tanks[attachedTankIndex].Position;
            }
            if (lastingTime > 0)
                lastingTime -= gameTime.ElapsedGameTime.Milliseconds;
            if (!oneTimeEffect && lastingTime <= 0)
                OnDestroyed();

            tSinceLastFrame += gameTime.ElapsedGameTime.Milliseconds;
            if (tSinceLastFrame > spriteInfo.timePerFrame)
            {
                if (spriteInfo.currentFrame == endFrame)
                {
                    if (oneTimeEffect)
                    {
                        OnDestroyed();
                        return;
                    }
                }
                tSinceLastFrame = 0;
                ++spriteInfo.currentFrame.X;
                if (spriteInfo.currentFrame.X >= spriteInfo.sheetSize.X)
                {
                    spriteInfo.currentFrame.X = 0;
                    ++spriteInfo.currentFrame.Y;
                    if (spriteInfo.currentFrame.Y > endFrame.Y)
                    {
                        spriteInfo.currentFrame = startFrame;
                    }
                }
            }
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(this.Texture, position, this.SourceRectangle, Color.White,
                0, origin, scale, SpriteEffects.None, Map.Layer1 - 0.09f);
        }
        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            hasBeenDestroyed(this);
        }
        // Private members
        readonly EffectType type;
        Vector2 position;
        readonly Vector2 origin;
        readonly Point startFrame;
        readonly Point endFrame;
        readonly byte attachedTankIndex;
        int lastingTime;
        readonly bool oneTimeEffect;
        float scale;
        SpriteDestroyedHandler hasBeenDestroyed;

        static List<EffectType> normalScale = new List<EffectType>();
        // Properties
        public EffectState State
        {
            get
            {
                EffectState ret = new EffectState();
                ret.type = type;
                ret.position = position;
                ret.startFrame = startFrame;
                ret.currentFrame = spriteInfo.currentFrame;
                ret.endFrame = endFrame;
                ret.attachedTankIndex = attachedTankIndex;
                ret.lastingTime = lastingTime;
                ret.oldEffect = true;
                return ret;
            }
        }
    }
}
