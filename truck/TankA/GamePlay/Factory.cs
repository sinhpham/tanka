using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace TankA.GamePlay
{
    class Factory
    {
        // Constructors
        public Factory(ContentManager contentManager, SpriteCreatedHandler created, SpriteDestroyedHandler destroyed)
        {
            Factory.created = created;
            Factory.destroyed = destroyed;
            // Clear
            Factory.tankSpriteInfo.Clear();
            Factory.gunTextures.Clear();
            Factory.ammoSpriteInfo.Clear();
            Factory.blockSpriteSI.Clear();
            Factory.effectSpriteInfo.Clear();
            Factory.itemSpriteInfo.Clear();
            gunSound.Clear();

            InitializeTankFactory(contentManager);
            InitializeAmmoFactory(contentManager);
            InitializeBlockSpriteFactory(contentManager);
            InitializeEffectFactory(contentManager);
            InitializeItemFactory(contentManager);

            tankHealthBar = contentManager.Load<Texture2D>(@"Images/GamePlay/Tank/hp_bar_overtank");
            tankMainHealthBar = contentManager.Load<Texture2D>(@"Images/GamePlay/Tank/main_hp_bar");
            tankHPbackground = contentManager.Load<Texture2D>(@"Images/GamePlay/Tank/hpbg");

            healthFont = contentManager.Load<SpriteFont>(@"Fonts/HealthFont");
            itemInfoFont = contentManager.Load<SpriteFont>(@"Fonts/ItemInfo");

            myselfColor = contentManager.Load<Texture2D>(@"Images/GamePlay/Tank/me");
            teamColor = contentManager.Load<Texture2D>(@"Images/GamePlay/Tank/team");
            enemyColor = contentManager.Load<Texture2D>(@"Images/GamePlay/Tank/enemy");

            statHeader = contentManager.Load<Texture2D>(@"Images/GamePlay/Stat_header");
            statBlue = contentManager.Load<Texture2D>(@"Images/GamePlay/Stat_blue");
            statTeam = contentManager.Load<Texture2D>(@"Images/GamePlay/stat_team");
            statFooter = contentManager.Load<Texture2D>(@"Images/GamePlay/Stat_footer");

            explosion = contentManager.Load<SoundEffect>(@"Sound/explosion2");
            SoundEffect gun = contentManager.Load<SoundEffect>(@"Sound/basic");
            gunSound.Add(AmmoType.Basic, gun);
            gun = contentManager.Load<SoundEffect>(@"Sound/fireball");
            gunSound.Add(AmmoType.FireBall, gun);
            gun = contentManager.Load<SoundEffect>(@"Sound/phitieu");
            gunSound.Add(AmmoType.PhiTieu, gun);
            gun = contentManager.Load<SoundEffect>(@"Sound/rocket");
            gunSound.Add(AmmoType.Rocket, gun);
        }
        // Private methods.
        static void InitializeTankFactory(ContentManager contentManager)
        {
            // Populate tankSpriteInfo dic.
            SpriteInfo spriteInfo = new SpriteInfo
            {
                texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Tank/bodytank"),
                frameSize = new Point(50, 50),
                isAnimated = false,
                currentFrame = new Point(0, 0),
                sheetSize = new Point(1, 4),
                timePerFrame = 0,
                health = null
            };
            tankSpriteInfo.Add(TankType.Body1, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Tank/bot");
            tankSpriteInfo.Add(TankType.Bot1, spriteInfo);

            // Populate gunTextures dic.
            gunTextures.Add(GunType.Gun1,
                contentManager.Load<Texture2D>(@"Images/GamePlay/Tank/hattank"));
        }
        static void InitializeAmmoFactory(ContentManager contentManager)
        {
            // Populate ammoSpriteInfo dic.
            SpriteInfo spriteInfo = new SpriteInfo
            {
                texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Ammo/basicammo"),
                frameSize = new Point(50, 50),
                isAnimated = false,
                currentFrame = new Point(0, 0),
                sheetSize = new Point(1, 4),
                timePerFrame = 0,
                health = 1
            };
            ammoSpriteInfo.Add(AmmoType.Basic, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Ammo/rocketammo");
            spriteInfo.frameSize = new Point(50, 50);
            spriteInfo.isAnimated = true;
            spriteInfo.sheetSize = new Point(4, 4);
            spriteInfo.timePerFrame = 100;
            ammoSpriteInfo.Add(AmmoType.Rocket, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Ammo/phitieuammo");
            ammoSpriteInfo.Add(AmmoType.PhiTieu, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Ammo/fireballammo");
            spriteInfo.isAnimated = false;
            spriteInfo.sheetSize = new Point(1, 4);
            ammoSpriteInfo.Add(AmmoType.FireBall, spriteInfo);
        }
        static void InitializeBlockSpriteFactory(ContentManager contentManager)
        {
            // Populate blockSpriteSI dic.
            SpriteInfo spriteInfo = new SpriteInfo
            {
                texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Onground/transperant"),
                frameSize = new Point(25, 25),
                isAnimated = false,
                currentFrame = new Point(0, 0),
                sheetSize = new Point(1, 1),
                timePerFrame = 0,
                health = null
            };
            blockSpriteSI.Add(BlockSpriteType.Invulnerable, spriteInfo);
            blockSpriteSI.Add(BlockSpriteType.Invisible, spriteInfo);
            spriteInfo.health = 100;
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Onground/snowwallfinal");
            spriteInfo.frameSize = new Point(25, 40);
            spriteInfo.sheetSize = new Point(1, 3);
            blockSpriteSI.Add(BlockSpriteType.Wall, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Onground/firewall");
            blockSpriteSI.Add(BlockSpriteType.FireWall, spriteInfo);
        }
        static void InitializeEffectFactory(ContentManager contentManager)
        {
            // EffectSpriteInfo.
            SpriteInfo spriteInfo = new SpriteInfo
            {
                texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Effects/fireovergun"),
                frameSize = new Point(100, 100),
                isAnimated = true,
                currentFrame = new Point(0, 0),
                sheetSize = new Point(2, 4),
                timePerFrame = 25,
                health = null
            };
            effectSpriteInfo.Add(EffectType.TankFire, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Effects/tank-ex2");
            spriteInfo.frameSize = new Point(400, 400);
            spriteInfo.sheetSize = new Point(5, 5);
            effectSpriteInfo.Add(EffectType.TankDestroyed, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Effects/tank_invul");
            spriteInfo.frameSize = new Point(100, 100);
            spriteInfo.sheetSize = new Point(2, 1);
            spriteInfo.timePerFrame = 200;
            effectSpriteInfo.Add(EffectType.TankInvulnerable, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_nitro_speedup");
            spriteInfo.frameSize = new Point(64, 64);
            spriteInfo.sheetSize = new Point(2, 1);
            effectSpriteInfo.Add(EffectType.II_IncreaseTankSpeed, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_speedup_ammo");
            effectSpriteInfo.Add(EffectType.II_IncreaseAmmoSpeed, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_IncreaseArmor");
            effectSpriteInfo.Add(EffectType.II_IncreaseArmor, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_speedup_cooldown");
            effectSpriteInfo.Add(EffectType.II_DecreaseTBF, spriteInfo);
        }
        static void InitializeItemFactory(ContentManager contentManager)
        {
            // ItemSpriteInfo.
            SpriteInfo spriteInfo = new SpriteInfo
            {
                texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_MaxTankSpeed"),
                frameSize = new Point(64, 64),
                isAnimated = true,
                currentFrame = new Point(0, 0),
                sheetSize = new Point(2, 1),
                timePerFrame = 200,
                health = null
            };
            itemSpriteInfo.Add(ItemType.MaxTankSpeed, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_nitro_speedup");
            itemSpriteInfo.Add(ItemType.IncreaseTankSpeed, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_MinTankSpeed");
            itemSpriteInfo.Add(ItemType.MinTankSpeed, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_MinimizeEnemiesTanksSpeed");
            itemSpriteInfo.Add(ItemType.MinimizeEnemiesTanksSpeed, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_MaxAmmoSpeed");
            itemSpriteInfo.Add(ItemType.MaxAmmoSpeed, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_speedup_ammo");
            itemSpriteInfo.Add(ItemType.IncreaseAmmoSpeed, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_MinAmmoSpeed");
            itemSpriteInfo.Add(ItemType.MinAmmoSpeed, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_MinimizeEnemiesAmmoSpeed");
            itemSpriteInfo.Add(ItemType.MinimizeEnemiesTanksAmmoSpeed, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_MaxArmor");
            itemSpriteInfo.Add(ItemType.MaxArmor, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_IncreaseArmor");
            itemSpriteInfo.Add(ItemType.IncreaseArmor, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_MinArmor");
            itemSpriteInfo.Add(ItemType.MinArmor, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_MinimizeEnemiesTanksArmor");
            itemSpriteInfo.Add(ItemType.MinimizeEnemiesTanksArmor, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_MinTBF");
            itemSpriteInfo.Add(ItemType.MinTBF, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_speedup_cooldown");
            itemSpriteInfo.Add(ItemType.DecreaseTBF, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_MaxTBF");
            itemSpriteInfo.Add(ItemType.MaxTBF, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_MaximizeEnemiesTBF");
            itemSpriteInfo.Add(ItemType.MaximizeEnemiesTBF, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_add50health");
            itemSpriteInfo.Add(ItemType.Add50Health, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_invul");
            itemSpriteInfo.Add(ItemType.MakeTankInvulnerable, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_rocket_ammo");
            itemSpriteInfo.Add(ItemType.ChangeAmmoToRocket, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_FireballAmmo");
            itemSpriteInfo.Add(ItemType.ChangeAmmoToFireBall, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_phitieu");
            itemSpriteInfo.Add(ItemType.ChangeAmmoToPhiTieu, spriteInfo);
            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_FreezeAllEnemiesTank");
            itemSpriteInfo.Add(ItemType.FreezeAllEnemiesTanks, spriteInfo);

            spriteInfo.texture = contentManager.Load<Texture2D>(@"Images/GamePlay/Items/it_unidentify_item");
            itemSpriteInfo.Add(ItemType.Random, spriteInfo);
        }
        // Create functions
        public static byte CreateTank(TankState state, Dictionary<byte, Tank> tanks)
        {
            SpriteInfo si = tankSpriteInfo[state.tankInfo.tankType];
            if (state.tankIndex == 0xff)
            {
                // Need auto generate tankindex. We don't need to generate tank index
                // if we are client or loading from a save game.
                byte r = (byte)TankAGame.Random.Next(200);
                // Keep trying until we find unique tank index.
                while (tanks.ContainsKey(r))
                {
                    ++r;
                    if (r > 200)
                        r = 0;
                }

                state.tankIndex = r;
            }
            if (state.playerIndex == 0xff)
            {
                // Not a user-controlled tank.
                Tank t = new Tank(si, state, destroyed);

                created(t);
                return state.tankIndex;
            }
            // Usertank.
            UserTank uT = new UserTank(si, gunTextures[state.tankInfo.gunType], state, destroyed);
            var config = TankAConfig.Instance;
            if (uT.PlayerIndex == 0)
                config.userTeam = uT.Team;
            
            created(uT);
            return state.tankIndex;
        }

        public static void CreateAmmo(AmmoState state)
        {
            SpriteInfo si = ammoSpriteInfo[state.ammoInfo.type];
            // Realsize
            Point realSize = new Point();
            switch (state.ammoInfo.type)
            {
                case AmmoType.Basic:
                    realSize.X = 5;
                    realSize.Y = 16;
                    break;
                case AmmoType.Rocket:
                    realSize.X = 16;
                    realSize.Y = 50;
                    break;
                case AmmoType.FireBall:
                    realSize.X = 40;
                    realSize.Y = 40;
                    break;
                case AmmoType.PhiTieu:
                    realSize.X = 50;
                    realSize.Y = 50;
                    break;
            }
            // Calculate ammo position
            Vector2 ammoPosition = state.gunPosition;
            state.ammoInfo.realSize = realSize;
            switch (state.direction)
            {
                case Direction.Up:
                    ammoPosition.Y -= realSize.Y / 2;
                    break;
                case Direction.Down:
                    ammoPosition.Y += realSize.Y / 2;
                    break;
                case Direction.Left:
                    ammoPosition.X -= realSize.Y / 2;
                    break;
                case Direction.Right:
                    ammoPosition.X += realSize.Y / 2;
                    break;
            }
            Ammo a = new Ammo(si, state, ammoPosition);

            created(a);
        }

        public static void CreateBlockSprite(BlockSpriteState state)
        {
            SpriteInfo si = blockSpriteSI[state.type];

            state.maxHealth = null;
            switch (state.type)
            {
                case BlockSpriteType.Wall:
                    state.maxHealth = 100;
                    break;
                case BlockSpriteType.FireWall:
                    state.maxHealth = 200;
                    break;
            }

            if (state.health == null)
            {
                // Default health.
                switch (state.type)
                {
                    case BlockSpriteType.Wall:
                        state.health = 100;
                        break;
                    case BlockSpriteType.FireWall:
                        state.health = 200;
                        break;
                }
            }
            si.health = state.health;
            BlockSprite bs = new BlockSprite(si, state, destroyed);

            created(bs);
        }

        public static void CreateEffect(EffectState state)
        {
            SpriteInfo si = effectSpriteInfo[state.type];
            Effect e = new Effect(si, state, destroyed);

            created(e);
        }

        public static Item CreateItem(ItemState state, List<Item> items)
        {
            if (state.itemIndex == 0xff)
            {
                // Need generate item index.
                state.itemIndex = (byte)TankAGame.Random.Next(200);
                bool duplicated = false;
                do
                {
                    ++state.itemIndex;
                    if (state.itemIndex > 200)
                        state.itemIndex = 0;
                    duplicated = false;
                    foreach (var item in items)
                        if (item.ItemIndex == state.itemIndex)
                        {
                            duplicated = true;
                            break;
                        }
                } while (duplicated);
            }
            SpriteInfo si;
            if (state.itemIndex % 5 == 0)
            {
                si = itemSpriteInfo[ItemType.Random];
            }
            else
            {
                si = itemSpriteInfo[state.type];
            }
            Item it = new Item(si, state, destroyed);

            created(it);
            return it;
        }

        public static Texture2D TankHealthBar()
        {
            return tankHealthBar;
        }
        public static Texture2D TankMainHealthBar()
        {
            return tankMainHealthBar;
        }
        public static Texture2D TankHPBackground()
        {
            return tankHPbackground;
        }
        public static Texture2D MyselfColor()
        {
            return myselfColor;
        }
        public static Texture2D TeamColor()
        {
            return teamColor;
        }
        public static Texture2D EnemyColor()
        {
            return enemyColor;
        }
        public static SpriteFont HealthFont()
        {
            return healthFont;
        }
        public static SpriteFont ItemInfoFont()
        {
            return itemInfoFont;
        }
        public static Texture2D StatHeader()
        {
            return statHeader;
        }
        public static Texture2D StatBlue()
        {
            return statBlue;
        }
        public static Texture2D StatTeam()
        {
            return statTeam;
        }
        public static Texture2D StatFooter()
        {
            return statFooter;
        }
        public static void PlayExplosion()
        {
            explosion.Play();
        }
        public static void PlayGunSound(AmmoType type)
        {
            gunSound[type].Play();
        }

        // Private members
        static Dictionary<TankType, SpriteInfo> tankSpriteInfo = new Dictionary<TankType, SpriteInfo>();
        static Dictionary<GunType, Texture2D> gunTextures = new Dictionary<GunType, Texture2D>();

        static Dictionary<AmmoType, SpriteInfo> ammoSpriteInfo = new Dictionary<AmmoType, SpriteInfo>();

        static Dictionary<BlockSpriteType, SpriteInfo> blockSpriteSI = new Dictionary<BlockSpriteType, SpriteInfo>();

        static Dictionary<EffectType, SpriteInfo> effectSpriteInfo = new Dictionary<EffectType, SpriteInfo>();

        static Dictionary<ItemType, SpriteInfo> itemSpriteInfo = new Dictionary<ItemType, SpriteInfo>();

        static Texture2D tankHealthBar;
        static Texture2D tankMainHealthBar;
        static Texture2D tankHPbackground;

        static Texture2D myselfColor;
        static Texture2D teamColor;
        static Texture2D enemyColor;

        static SpriteFont healthFont;
        static SpriteFont itemInfoFont;

        static Texture2D statHeader;
        static Texture2D statBlue;
        static Texture2D statTeam;
        static Texture2D statFooter;

        static SoundEffect explosion;
        static Dictionary<AmmoType, SoundEffect> gunSound = new Dictionary<AmmoType, SoundEffect>();

        static SpriteCreatedHandler created;
        static SpriteDestroyedHandler destroyed;

    }
}
