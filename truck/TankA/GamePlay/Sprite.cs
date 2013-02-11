using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TankA.Network;

namespace TankA.GamePlay
{
    public struct SpriteInfo
    {
        // Constructors
        public SpriteInfo(Texture2D texture, Point frameSize)
            : this(texture, frameSize, false, new Point(0, 0), new Point(0, 0), 0, null)
        {
        }
        public SpriteInfo(Texture2D texture, Point frameSize, int? health)
            : this(texture, frameSize, false, new Point(0, 0), new Point(0, 0), 0, health)
        {
        }
        public SpriteInfo(Texture2D texture, Point frameSize, bool isAnimated,
            Point currentFrame, Point sheetSize, int timePerFrame, int? health)
        {
            this.texture = texture;
            this.frameSize = frameSize;
            this.isAnimated = isAnimated;
            this.currentFrame = currentFrame;
            this.sheetSize = sheetSize;
            this.timePerFrame = timePerFrame;
            this.health = health;
        }
        // Public data
        public Texture2D texture;
        public Point frameSize;
        public bool isAnimated;
        public Point currentFrame;
        public Point sheetSize;
        public int timePerFrame;
        public int? health;
    }

    class Sprite
    {
        // Constructors
        public Sprite(SpriteInfo spriteInfo)
        {
            this.spriteInfo = spriteInfo;
        }
        // Public methods
        public virtual void Update(GameTime gameTime)
        {
            if (spriteInfo.isAnimated)
            {
                tSinceLastFrame += gameTime.ElapsedGameTime.Milliseconds;
                if (tSinceLastFrame > spriteInfo.timePerFrame)
                {
                    tSinceLastFrame = 0;
                    ++spriteInfo.currentFrame.X;
                    if (spriteInfo.currentFrame.X >= spriteInfo.sheetSize.X)
                        spriteInfo.currentFrame.X = 0;
                }
            }
        }
        protected virtual void OnDestroyed()
        {
            destroyed = true;
        }
        protected void SetSheetRow(int rowNumber)
        {
            Point p = new Point(0, rowNumber);
            SetFrame(p);
        }
        protected void SetFrame(Point p)
        {
            if (p.X >= spriteInfo.sheetSize.X || p.Y >= spriteInfo.sheetSize.Y)
                return;
            spriteInfo.currentFrame = p;
        }
        // Private members
        protected SpriteInfo spriteInfo;
        protected int tSinceLastFrame;
        bool destroyed;
        // Properties
        public Texture2D Texture
        {
            get { return spriteInfo.texture; }
        }
        public Rectangle SourceRectangle
        {
            get
            {
                return new Rectangle(spriteInfo.currentFrame.X * spriteInfo.frameSize.X,
                    spriteInfo.currentFrame.Y * spriteInfo.frameSize.Y,
                    spriteInfo.frameSize.X, spriteInfo.frameSize.Y);
            }
        }
        public virtual int? Health
        {
            get { return spriteInfo.health; }
            set
            {
                spriteInfo.health = value;
                if (value <= 0)
                    OnDestroyed();
            }
        }
        public int FrameWidth
        {
            get { return spriteInfo.frameSize.X; }
        }
        public int FrameHeight
        {
            get { return spriteInfo.frameSize.Y; }
        }
        public bool Destroyed
        {
            get { return destroyed; }
        }
    }
}
