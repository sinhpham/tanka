using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TankA.GamePlay
{
    abstract class MovingSprite : Sprite
    {
        // Constructors
        public MovingSprite(SpriteInfo si, Point realSize, Vector2 position, byte speed)
            : base(si)
        {
            this.position = position;
            this.speed = speed;
            this.movingDirection = Direction.None;
            this.realSize = realSize;
            this.horizontal = false;
            this.tPerMovement = 19;
            origin.X = (int)(si.frameSize.X / 2);
            origin.Y = (int)(si.frameSize.Y / 2);
        }

        // Public methods
        protected new bool Update(GameTime gameTime)
        {
            // Return true if this sprite has moved to new position.
            base.Update(gameTime);

            tSinceLastMove += gameTime.ElapsedGameTime.Milliseconds;
            if (tSinceLastMove < tPerMovement)
                return false;
            // Time for a move.
            tSinceLastMove -= tPerMovement;
            switch (movingDirection)
            {
                case Direction.Up:
                    position.Y -= speed;
                    break;
                case Direction.Down:
                    position.Y += speed;
                    break;
                case Direction.Left:
                    position.X -= speed;
                    break;
                case Direction.Right:
                    position.X += speed;
                    break;
            }
            return true;
        }
        protected virtual void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(this.Texture, position, this.SourceRectangle, Color.White,
                0, Origin, 1, SpriteEffects.None, Map.Layer1);
        }
        // Protected members
        protected Vector2 position;
        // Private members
        byte speed;
        int tSinceLastMove;
        readonly Point realSize;
        readonly Vector2 origin;
        // Time per movement, game should update faster than this.
        readonly int tPerMovement;
        bool horizontal;
        Direction movingDirection;
        // Properties
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        public List<Point> MapPoints
        {
            get
            {
                List<Point> ret = new List<Point>();
                Rectangle rec = OccupiedArea;
                int x = rec.X - rec.X % Map.CellSize, y = rec.Y - rec.Y % Map.CellSize;
                while (x < rec.X + rec.Width)
                {
                    while (y < rec.Y + rec.Height)
                    {
                        ret.Add(new Point((int)x / Map.CellSize, (int)y / Map.CellSize));
                        y += Map.CellSize;
                    }
                    y = rec.Y - rec.Y % Map.CellSize;
                    x += Map.CellSize;
                }
                return ret;
            }
        }
        public Point RealSize
        {
            get { return realSize; }
        }
        public Vector2 Origin
        {
            get { return origin; }
        }
        public virtual byte Speed
        {
            get { return speed; }
            set { speed = value; }
        }
        public Direction MovingDirection
        {
            get { return movingDirection; }
            set
            {
                movingDirection = value;

                if (movingDirection == Direction.None)
                    return;
                if (movingDirection == Direction.Up || movingDirection == Direction.Down)
                {
                    horizontal = false;
                    return;
                }
                horizontal = true;
            }
        }
        public Rectangle OccupiedArea
        {
            get
            {
                Rectangle ret = new Rectangle();
                if (horizontal)
                {
                    ret.Width = realSize.Y;
                    ret.Height = realSize.X;
                    ret.X = (int)position.X - realSize.Y / 2;
                    ret.Y = (int)position.Y - realSize.X / 2;
                }
                else
                {
                    ret.Width = realSize.X;
                    ret.Height = realSize.Y;
                    ret.X = (int)position.X - realSize.X / 2;
                    ret.Y = (int)position.Y - realSize.Y / 2;
                }
                return ret;
            }
        }
    }
}
