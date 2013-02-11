using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TankA.Network;

namespace TankA.GamePlay
{
    public struct TankState
    {
        public TankInfo tankInfo;
        public Vector2 position;
        public byte playerIndex;
        public byte team;
        public byte tankIndex;
    }

    public struct TankInfo
    {
        public TankType tankType;
        public Point realSize;
        public GunType gunType;
        public Direction gunDirection;
        public int health;
        public byte armor;
        public byte speed;
        public int timeBetweenFires;
        public int freezeTime;
        public int invulnerableTime;
        public AmmoInfo ammoInfo;
    }

    public enum TankType
    {
        Body1,
        Tank1,
        Bot1
    }

    public enum GunType
    {
        Gun1,
        Gun2,
        None
    }

    public struct TankInMapState
    {
        public Point position;
        public Direction destinedDirection;
        public byte tankIndex;
    }

    class Tank : MovingSprite
    {
        // Constructors
        public Tank(SpriteInfo si, TankState ts, SpriteDestroyedHandler handler)
            : base(si, ts.tankInfo.realSize, ts.position, ts.tankInfo.speed)
        {
            healthBarRectangle.X = 0;
            healthBarRectangle.Width = 100;
            healthBarRectangle.Height = 25;
            mainHealthBarRectangle.X = 0;
            mainHealthBarRectangle.Width = 200;
            mainHealthBarRectangle.Height = 50;
            healthString = "100/100";
            this.Health = ts.tankInfo.health;
            
            this.GunDirection = ts.tankInfo.gunDirection;
            this.destinedDirection = Direction.None;
            this.timeBetweenFires = ts.tankInfo.timeBetweenFires;
            this.armor = ts.tankInfo.armor;
            this.freezeTime = ts.tankInfo.freezeTime;
            
            this.ammoInfo = ts.tankInfo.ammoInfo;
            this.tankType = ts.tankInfo.tankType;
            this.gunType = ts.tankInfo.gunType;
            this.team = ts.team;
            this.tankIndex = ts.tankIndex;
            this.InvulnerableTime = ts.tankInfo.invulnerableTime;

            this.hasBeenDestroyed = handler;
        }
        // Public methods
        public void Update(GameTime gameTime, Dictionary<Point, BlockSprite> blockMap, Dictionary<byte, Tank> tanks, byte myIndex)
        {
            // Determine movingDirection.
            Move();
            if (coolDownTime > 0)
                coolDownTime -= gameTime.ElapsedGameTime.Milliseconds;
            if (freezeTime > 0)
                freezeTime -= gameTime.ElapsedGameTime.Milliseconds;
            if (invulnerableTime > 0)
                invulnerableTime -= gameTime.ElapsedGameTime.Milliseconds;

            if (!base.Update(gameTime))
            {
                // No need for collision detection if current position hasn't changed.
                return;
            }

            List<Point> newPoints = MapPoints;
            bool passable = true;
            foreach (Point p in newPoints)
                if (blockMap.ContainsKey(p))
                {
                    passable = false;
                    break;
                }

            if (passable && !Collide(tanks, myIndex))
            {
                return;
            }
            else
            {
                // Reverse last move if a collision has been detected.
                switch (MovingDirection)
                {
                    case Direction.Up:
                        position.Y += Speed;
                        break;
                    case Direction.Down:
                        position.Y -= Speed;
                        break;
                    case Direction.Left:
                        position.X += Speed;
                        break;
                    case Direction.Right:
                        position.X -= Speed;
                        break;
                }
            }
        }
        public new virtual void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            if (TankAConfig.Instance.displayTankHealthBar)
            {
                spriteBatch.Draw(Factory.TankHealthBar(), position, healthBarRectangle, Color.White,
                    0, new Vector2(50, 12), 1, SpriteEffects.None, Map.Layer1 - (float)0.03);
            }
            var config = TankAConfig.Instance;
            if (team == config.userTeam)
            {
                spriteBatch.Draw(Factory.TeamColor(), position, null, Color.White,
                    0, new Vector2(30, 30), 1, SpriteEffects.None, Map.Layer0 - 0.0001f);
            }
            else
            {
                spriteBatch.Draw(Factory.EnemyColor(), position, null, Color.White,
                    0, new Vector2(30, 30), 1, SpriteEffects.None, Map.Layer0 - 0.0001f);
            }
        }
        public virtual void Fire()
        {
            if (coolDownTime > 0)
            {
                Debug.Write("No fire because of cooldown: ");
                Debug.WriteLine(coolDownTime);
                return;
            }
            coolDownTime = timeBetweenFires;

            Factory.CreateAmmo(new AmmoState()
            {
                ammoInfo = ammoInfo,
                gunPosition = GunPosition(),
                direction = GunDirection,
                tankIndex = tankIndex,
                team = team
            });

            createTankFireEffect();
            Factory.PlayGunSound(ammoInfo.type);
        }
        public virtual void ForceFire()
        {
            coolDownTime = timeBetweenFires;
            // Ignore cooldown time, use for network.
            Factory.CreateAmmo(new AmmoState()
            {
                ammoInfo = ammoInfo,
                gunPosition = GunPosition(),
                direction = GunDirection,
                tankIndex = tankIndex,
                team = team
            });

            createTankFireEffect();
            Factory.PlayGunSound(ammoInfo.type);
        }
        
        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            EffectState e = new EffectState()
            {
                type = EffectType.TankDestroyed,
                position = Position,
                startFrame = new Point(0, 0),
                endFrame = new Point(4, 4),
                attachedTankIndex = 0xff
            };
            Factory.CreateEffect(e);
            Factory.PlayExplosion();
            hasBeenDestroyed(this);
        }

        // Private methods
        void Move()
        {
            if (freezeTime > 0)
            {
                GunDirection = Direction.None;
                return;
            }

            if (destinedDirection == Direction.None ||
                destinedDirection == GunDirection ||
                IsOpposite(destinedDirection, this.GunDirection))
            {
                GunDirection = destinedDirection;
                return;
            }
            // Need to fix direction
            Vector2 newPos;
            Direction newDirection = FixDirection(out newPos);
            if (newDirection != Direction.None)
            {
                GunDirection = newDirection;
            }
            else
            {
                // Fix position
                position.X = newPos.X;
                position.Y = newPos.Y;
                GunDirection = destinedDirection;
            }
        }
        protected void createTankFireEffect()
        {
            EffectState e = new EffectState()
            {
                type = EffectType.TankFire,
                position = Position,
                attachedTankIndex = tankIndex,
            };
            switch (gunDirection)
            {
                case Direction.Up:
                    e.startFrame = new Point(0, 0);
                    e.endFrame = new Point(1, 0);
                    break;
                case Direction.Down:
                    e.startFrame = new Point(0, 1);
                    e.endFrame = new Point(1, 1);
                    break;
                case Direction.Left:
                    e.startFrame = new Point(0, 2);
                    e.endFrame = new Point(1, 2);
                    break;
                case Direction.Right:
                    e.startFrame = new Point(0, 3);
                    e.endFrame = new Point(1, 3);
                    break;
            }
            Factory.CreateEffect(e);
        }
        bool Collide(Dictionary<byte, Tank> tanks, byte myIndex)
        {
            foreach(Tank t in tanks.Values)
                if (t.TankIndex != myIndex)
                {
                    if (this.OccupiedArea.Intersects(t.OccupiedArea))
                        return true;
                }
            return false;
        }
        bool IsOpposite(Direction direction1, Direction direction2)
        {
            return (direction1 == Direction.Up && direction2 == Direction.Down)
                || (direction1 == Direction.Down && direction2 == Direction.Up)
                || (direction1 == Direction.Left && direction2 == Direction.Right)
                || (direction1 == Direction.Right && direction2 == Direction.Left);
        }
        Direction FixDirection(out Vector2 newPos)
        {
            int temp = (int)Position.X % Map.CellSize;
            newPos = Position;
            if (temp != 0)
            {
                // Need fix x
                if (temp < (int)Map.CellSize / 2)
                    if (temp < Speed)
                    {
                        newPos.X = Position.X - temp;
                        return Direction.None;
                    }
                    else
                        return Direction.Left;
                if (temp > Map.CellSize - Speed)
                {
                    newPos.X = Position.X - temp + Map.CellSize;
                    return Direction.None;
                }
                return Direction.Right;
            }
            temp = (int)Position.Y % Map.CellSize;
            if (temp != 0)
            {
                // Need fix y
                if (temp < (int)Map.CellSize / 2)
                    if (temp < Speed)
                    {
                        newPos.Y = Position.Y - temp;
                        return Direction.None;
                    }
                    else
                        return Direction.Up;
                if (temp > Map.CellSize - Speed)
                {
                    newPos.Y = Position.Y - temp + Map.CellSize;
                    return Direction.None;
                }
                return Direction.Down;
            }
            return Direction.None;
        }
        Vector2 GunPosition()
        {
            Vector2 ret = new Vector2();
            switch (GunDirection)
            {
                case Direction.Up:
                    ret.X = position.X;
                    ret.Y = (int)(position.Y - RealSize.Y / 2) - 10;
                    break;
                case Direction.Down:
                    ret.X = position.X;
                    ret.Y = (int)(position.Y + RealSize.Y / 2) + 10;
                    break;
                case Direction.Left:
                    ret.X = (int)(position.X - RealSize.Y / 2) - 10;
                    ret.Y = position.Y;
                    break;
                case Direction.Right:
                    ret.X = (int)(position.X + RealSize.Y / 2) + 10;
                    ret.Y = position.Y;
                    break;
            }
            return ret;
        }
        // Private members
        Direction gunDirection;
        Direction destinedDirection;
        TankType tankType;
        GunType gunType;
        protected int timeBetweenFires;
        protected int coolDownTime;
        byte armor;
        protected AmmoInfo ammoInfo;
        int freezeTime;
        int invulnerableTime;
        SpriteDestroyedHandler hasBeenDestroyed;

        protected readonly byte team;
        readonly byte tankIndex;

        Rectangle healthBarRectangle;
        protected Rectangle mainHealthBarRectangle;
        protected String healthString;
        protected Color healthStringColor;

        // Constrains.
        const byte maxTankSpeed = 8;
        const byte minTankSpeed = 1;
        const byte maxAmmoSpeed = 20;
        const byte minAmmoSpeed = 10;
        const byte maxArmor = 50;
        const byte minArmor = 5;
        const int maxTimeBetweenFires = 1500;
        const int minTimeBetweenFires = 300;
        const byte maxHealth = 100;
        // Properties
        public Direction GunDirection
        {
            get
            {
                return gunDirection;
            }
            set
            {
                MovingDirection = value;
                if (value != Direction.None)
                {
                    gunDirection = value;
                    switch (value)
                    {
                        case Direction.Up:
                            this.SetSheetRow(0);
                            break;
                        case Direction.Down:
                            this.SetSheetRow(1);
                            break;
                        case Direction.Left:
                            this.SetSheetRow(2);
                            break;
                        case Direction.Right:
                            this.SetSheetRow(3);
                            break;
                    }
                }
            }
        }
        public Direction DestinedDirection
        {
            get { return destinedDirection; }
            set { destinedDirection = value; }
        }
        public override byte Speed
        {
            get
            {
                return base.Speed;
            }
            set
            {
                if (value > maxTankSpeed)
                    value = maxTankSpeed;
                if (value < minTankSpeed)
                    value = minTankSpeed;

                base.Speed = value;
            }
        }
        public bool CanFire
        {
            get { return coolDownTime <= 0; }
        }
        public AmmoType AmmoType
        {
            get { return ammoInfo.type; }
            set
            {
                ammoInfo = Ammo.DefaultAmmoInfo(value);
            }
        }
        public byte AmmoSpeed
        {
            get { return ammoInfo.speed; }
            set
            {
                if (value > maxAmmoSpeed)
                    value = maxAmmoSpeed;
                if (value < minTankSpeed)
                    value = minTankSpeed;

                ammoInfo.speed = value;
            }
        }
        public byte Armor
        {
            get { return armor; }
            set
            {
                if (value > maxArmor)
                    value = maxArmor;
                if (value < minArmor)
                    value = minArmor;

                armor = value;
            }
        }
        public int TimeBetweenFires
        {
            get { return timeBetweenFires; }
            set
            {
                if (value > maxTimeBetweenFires)
                    value = maxTimeBetweenFires;
                if (value < minTimeBetweenFires)
                    value = minTimeBetweenFires;

                timeBetweenFires = value;
            }
        }
        public int FreezeTime
        {
            set
            {
                MovingDirection = Direction.None;
                freezeTime = value;
                var host = (ITankAHost)TankAGame.ThisGame.Services.GetService(typeof(ITankAHost));
                if (host != null)
                    host.AnnounceTankFreeze(tankIndex, value);
            }
        }
        public int InvulnerableTime
        {
            get { return invulnerableTime; }
            set
            {
                invulnerableTime = value;
                EffectState es = new EffectState()
                {
                    type = EffectType.TankInvulnerable,
                    startFrame = new Point(0, 0),
                    endFrame = new Point(1, 0),
                    attachedTankIndex = tankIndex,
                    lastingTime = value
                };
                Factory.CreateEffect(es);
            }
        }
        public bool IsInvulnerable
        {
            get { return invulnerableTime > 0; }
        }
        
        public byte Team
        {
            get { return team; }
        }
        public byte TankIndex
        {
            get { return tankIndex; }
        }
        
        public override int? Health
        {
            get
            {
                return base.Health;
            }
            set
            {
                if (value > maxHealth)
                    value = maxHealth;
                    
                base.Health = value;

                // Recalculate health bar.
                float r = (float)value / maxHealth * 10 + 1;
                if (r > 10)
                    r = 10;
                r = (int)(10 - (int)r);
                healthBarRectangle.Y = 25 * (int)r;
                mainHealthBarRectangle.Y = 50 * (int)r;
                healthString = value.ToString() + "/100";
                healthStringColor = Color.Red;
                if (value > 20)
                    healthStringColor = Color.Pink;
                if (value > 40)
                    healthStringColor = Color.Yellow;
                if (value > 60)
                    healthStringColor = Color.YellowGreen;
                if (value > 80)
                    healthStringColor = Color.Green;
                
                // Ack health change if I'm host.
                var host = (ITankAHost)TankAGame.ThisGame.Services.GetService(typeof(ITankAHost));
                if (host != null)
                    host.AckHealthChanged(this, (int)value);
            }
        }
        // Constrains.
        public static byte MaxTankSpeed
        {
            get { return maxTankSpeed; }
        }
        public static byte MinTankSpeed
        {
            get { return minTankSpeed; }
        }
        public static byte MaxAmmoSpeed
        {
            get { return maxAmmoSpeed; }
        }
        public static byte MinAmmoSpeed
        {
            get { return minAmmoSpeed; }
        }
        public static byte MaxArmor
        {
            get { return maxArmor; }
        }
        public static byte MinArmor
        {
            get { return minArmor; }
        }
        public static int MaxTBF
        {
            get { return maxTimeBetweenFires; }
        }
        public static int MinTBF
        {
            get { return minTimeBetweenFires; }
        }
        // States.
        public TankInMapState InMapState
        {
            get
            {
                TankInMapState ret;
                ret.destinedDirection = this.destinedDirection;
                ret.position = new Point((int)this.position.X, (int)this.position.Y);
                ret.tankIndex = tankIndex;
                return ret;
            }
        }
        public virtual TankState State
        {
            get
            {
                TankInfo ti = new TankInfo()
                {
                    tankType = tankType,
                    realSize = RealSize,
                    gunType = gunType,
                    gunDirection = gunDirection,
                    health = (int)Health,
                    armor = armor,
                    speed = Speed,
                    timeBetweenFires = timeBetweenFires,
                    freezeTime = freezeTime,
                    invulnerableTime = invulnerableTime,
                    ammoInfo = ammoInfo,
                };
                TankState ret = new TankState()
                {
                    tankInfo = ti,
                    position = Position,
                    playerIndex = 255,
                    tankIndex = tankIndex,
                    team = team,
                };
                return ret;
            }
        }
    }
}
