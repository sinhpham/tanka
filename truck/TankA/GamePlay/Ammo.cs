using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TankA.Network;

namespace TankA.GamePlay
{
    public struct AmmoState
    {
        public AmmoInfo ammoInfo;
        public Vector2 gunPosition;
        public Direction direction;
        public byte tankIndex;
        public byte team;
    }

    public struct AmmoInfo
    {
        public AmmoType type;
        public Point realSize;
        public byte speed;
        public int damage;
        public int range;
    }

    public enum AmmoType
    {
        Basic,
        Rocket,
        FireBall,
        PhiTieu
    }

    class Ammo : MovingSprite
    {
        // Constructors.
        static Ammo()
        {
            AmmoInfo info = new AmmoInfo()
            {
                type = AmmoType.Basic,
                speed = 12,
                damage = 30,
                range = 300
            };
            defaultAmmoInfo.Add(info.type, info);

            info.type = AmmoType.Rocket;
            info.speed = 18;
            info.damage = 80;
            info.range = 500;
            defaultAmmoInfo.Add(info.type, info);

            info.type = AmmoType.FireBall;
            info.speed = 10;
            info.damage = 80;
            info.range = 300;
            defaultAmmoInfo.Add(info.type, info);

            info.type = AmmoType.PhiTieu;
            info.speed = 20;
            info.damage = 50;
            info.range = 800;
            defaultAmmoInfo.Add(info.type, info);
        }
        public Ammo(SpriteInfo spriteInfo, AmmoState ammoState, Vector2 position)
            : base(spriteInfo, ammoState.ammoInfo.realSize, position, ammoState.ammoInfo.speed)
        {
            this.team = ammoState.team;
            this.tankIndex = ammoState.tankIndex;
            this.needCheckForCollision = true;

            switch (ammoState.direction)
            {
                case Direction.Up:
                    SetSheetRow(0);
                    break;
                case Direction.Down:
                    SetSheetRow(1);
                    break;
                case Direction.Left:
                    SetSheetRow(2);
                    break;
                case Direction.Right:
                    SetSheetRow(3);
                    break;
            }
            this.ammoType = ammoState.ammoInfo.type;
            this.damage = ammoState.ammoInfo.damage;
            MovingDirection = ammoState.direction;
            this.range = ammoState.ammoInfo.range;
            this.damagedSprites = new List<Sprite>();
        }
        public static AmmoInfo DefaultAmmoInfo(AmmoType type)
        {
            return defaultAmmoInfo[type];
        }
        // Public methods.
        public void Update(GameTime gameTime, Dictionary<Point, BlockSprite> blockMap,
            List<Ammo> ammo, Dictionary<byte, Tank> allTanks, byte myIndex)
        {
            if (invisibleTime > 0)
            {
                invisibleTime -= gameTime.ElapsedGameTime.Milliseconds;
                if (invisibleTime < 0)
                    damagedSprites.Clear();
            }
            // Collision detection.
            if (needCheckForCollision)
            {
                GetDamagedSprites(blockMap, ammo, allTanks, myIndex);
                if (damagedSprites.Count > 0)
                {
                    if (NetworkManager.Role == GameLogicMode.Client)
                    {
                        // No need to call Impact, just hide myself.
                        invisibleTime = 200;
                    }
                    else
                    {
                        foreach (Sprite s in damagedSprites)
                            Impact(s);
                        return;
                    }
                }

                range -= Speed;
                if (range <= 0)
                {
                    this.OnDestroyed();
                    return;
                }
            }
            // Move if this ammo hasn't been destroyed.
            // Only check for collision in the next update
            // if this ammo has moved in this update.
            if (base.Update(gameTime))
                needCheckForCollision = true;
            else
                needCheckForCollision = false;
        }
        public new void Draw(SpriteBatch spriteBatch)
        {
            if (invisibleTime <= 0)
            {
                spriteBatch.Draw(this.Texture, position, this.SourceRectangle, Color.White,
                    0, Origin, 1, SpriteEffects.None, Map.Layer1 - 0.01f);
            }
        }
        // Private methods.
        void GetDamagedSprites(Dictionary<Point, BlockSprite> blockMap, List<Ammo> ammo,
            Dictionary<byte, Tank> tanks, byte myIndex)
        {
            foreach (Point p in MapPoints)
                if (blockMap.ContainsKey(p) && blockMap[p].BlockAmmo)
                    damagedSprites.Add(blockMap[p]);

            GetDamagedAmmo(ammo, myIndex);
            GetDamagedTanks(tanks);
        }
        void GetDamagedAmmo(List<Ammo> ammo, byte myIndex)
        {
            for (int i = 0; i < ammo.Count; ++i)
                if (i != myIndex)
                    if (OccupiedArea.Intersects(ammo[i].OccupiedArea))
                        damagedSprites.Add(ammo[i]);
        }
        void GetDamagedTanks(Dictionary<byte, Tank> tanks)
        {
            foreach (Tank t in tanks.Values)
                if (this.OccupiedArea.Intersects(t.OccupiedArea))
                    damagedSprites.Add(t);
        }
        void Impact(Sprite impactedSprite)
        {
            this.OnDestroyed();

            if (impactedSprite.Health == null)
                return;

            Tank t = impactedSprite as Tank;
            // Cause damage to impacted sprite.
            if (t != null)
            {
                if (team == t.Team)
                    t.FreezeTime = 2000;
                else
                    if (!t.IsInvulnerable)
                    {
                        t.Health -= (int?)(damage * (1 / (1 + 0.06 * t.Armor)));
                    }
            }
            else
            {
                // All things other than tank take full damage.
                impactedSprite.Health -= damage;
            }

            if (impactedSprite.Health <= 0)
            {
                if (t != null)
                {
                    UserTank uT = t as UserTank;
                    if (uT == null)
                    {
                        // Not a user tank.
                        if (NetworkManager.Role == GameLogicMode.Undefined)
                        {
                            var netStat = (INetStat)TankAGame.ThisGame.Services.GetService(typeof(INetStat));
                            netStat.AckKill(IPAddress.Loopback, IPAddress.None);
                        }
                    }
                    // A tank has been destroyed.
                    var host = (ITankAHost)TankAGame.ThisGame.Services.GetService(typeof(ITankAHost));
                    if (host != null)
                        host.AckKill(tankIndex, t.TankIndex);
                }
            }
        }
        // Private members.
        readonly int damage;
        readonly AmmoType ammoType;
        readonly byte team;
        readonly byte tankIndex;
        bool needCheckForCollision;
        int range;
        int invisibleTime;
        List<Sprite> damagedSprites = new List<Sprite>();

        const byte maxAmmoSpeed = 20;
        // Default values for ammo.
        static Dictionary<AmmoType, AmmoInfo> defaultAmmoInfo = new Dictionary<AmmoType, AmmoInfo>();
        // Properties.
        public override byte Speed
        {
            get
            {
                return base.Speed;
            }
            set
            {
                if (value > maxAmmoSpeed)
                    value = maxAmmoSpeed;
                base.Speed = value;
            }
        }
        public AmmoState State
        {
            get
            {
                AmmoInfo ai = new AmmoInfo()
                {
                    type = ammoType,
                    realSize = RealSize,
                    speed = Speed,
                    damage = damage,
                    range = range
                };
                Vector2 gunPosition = Position;
                switch (MovingDirection)
                {
                    case Direction.Up:
                        gunPosition.Y += RealSize.Y / 2;
                        break;
                    case Direction.Down:
                        gunPosition.Y -= RealSize.Y / 2;
                        break;
                    case Direction.Left:
                        gunPosition.X += RealSize.Y / 2;
                        break;
                    case Direction.Right:
                        gunPosition.X -= RealSize.Y / 2;
                        break;
                }
                AmmoState ret = new AmmoState()
                {
                    ammoInfo = ai,
                    gunPosition = gunPosition,
                    direction = MovingDirection,
                    team = team,
                    tankIndex = tankIndex
                };
                return ret;
            }
        }
    }
}
