using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using TankA.Network;

namespace TankA.GamePlay
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right,
        None
    }

    public struct MapData
    {
        // Public members
        public List<string> BackgroundMap;
        public List<string> OngroundMap;
        public List<string> ForegroundMap;
        public List<Vector2> SpawnLocations;
    }

    public struct SaveGameData
    {
        public List<string> BackgroundMap;
        public List<string> OngroundMap;
        public List<string> ForegroundMap;
        public List<TankState> TankStates;
        public List<AmmoState> AmmoStates;
        public List<BlockSpriteState> wallStates;
        public List<EffectState> EffectStates;
        public List<ItemState> ItemStates;
        public int? nextItemTime;
    }

    delegate void SpriteCreatedHandler(Sprite s);
    delegate void SpriteDestroyedHandler(Sprite s);

    interface IControlTank
    {
        void ChangeTankInMapState(byte tankIndex, Vector2 position, Direction direction);
        Point GetTankPosition(byte tankIndex);
        IEnumerable<TankInMapState> GetAllTanksInMapState();

        void Fire(byte tankIndex, Point position);
        void FilterSurvival(List<byte> indexes);

        void AckTankHealthChanged(byte tankIndex, int health);
        void AckTankFreeze(byte tankIndex, int freezeTime);

        Dictionary<byte, Tank> AllTanksInMap();
        Vector2 RandomTankPosition();
    }

    interface IMap
    {
        string MapName();
        List<Point> ReachablePoints();

        Dictionary<Point, BlockSprite> BlockMap();
        void AckBlockSpriteHealthChanged(Point position, int health);
        void FilterRemainingBlockSprite(List<Point> points);

        List<Item> AllItemsInMap();
        void AckItemActivation(byte itemIndex, byte tankIndex);

        void NewCSRound();
    }

    class Map : IControlTank, IMap
    {
        // Constructors
        public Map(ContentManager content, bool single)
        {
            TankAGame.ThisGame.Services.AddService(typeof(IControlTank), this);
            TankAGame.ThisGame.Services.AddService(typeof(IMap), this);

            
            factory = new Factory(content, OnSpriteCreated, OnSpriteDestroyed);
            this.single = single;
            if (single)
            {
                bot = new BotManager();
            }
            gameLogic = new GameLogic();
            TankAGame.ThisGame.Services.AddService(typeof(IGameLogic), gameLogic);
            netStat = new NetStat();
            TankAGame.ThisGame.Services.AddService(typeof(INetStat), netStat);

            InitializeBackgroundAndForeground(content);

            InitializeOngroundDic();

            // Populate map lists
            for (int i = 0; i < 30; ++i)
            {
                backgroundMap.Add("0000000000000000000000000000000000000000");
                foregroundMap.Add("0000000000000000000000000000000000000000");
            }
        }
        // Public methods
        public bool LoadMap(string fileName, ContentManager content)
        {
            if (File.Exists(fileName))
            {
                // Deserialization
                MapData md = new MapData();
                TextReader tr = new StreamReader(fileName);
                XmlSerializer xs = new XmlSerializer(typeof(MapData));
                md = (MapData)xs.Deserialize(tr);
                tr.Close();

                // Convert to actual data
                backgroundMap = md.BackgroundMap;
                foregroundMap = md.ForegroundMap;
                ConstructOngroundMap(md.OngroundMap, true);

                tankSpawnLocations = md.SpawnLocations;

                mapName = fileName;
                return true;
            }
            return false;
        }

        public void UnloadContent()
        {
            TankAGame.ThisGame.Services.RemoveService(typeof(IControlTank));
            TankAGame.ThisGame.Services.RemoveService(typeof(IMap));
            TankAGame.ThisGame.Services.RemoveService(typeof(IGameLogic));
            TankAGame.ThisGame.Services.RemoveService(typeof(INetStat));
        }

        public void MoveUserTank(byte userTankIndex, Direction direction)
        {
            if (!userTanks.ContainsKey(userTankIndex))
                return;
            UserTank t = userTanks[userTankIndex];

            var client = (ITankAClient)TankAGame.ThisGame.Services.GetService(typeof(ITankAClient));
            if (client != null && direction != t.DestinedDirection)
            {
                // Tank destinedDirection changed.
                client.RequestMove(t.TankIndex, direction);
            }
            else
            {
                // Not a client or direction is not changed - make the move immediately.
                t.DestinedDirection = direction;
            }
        }
        public void FireUserTank(byte playerIndex)
        {
            if (!userTanks.ContainsKey(playerIndex))
                return;
            UserTank t = userTanks[playerIndex];

            if (!t.CanFire)
                return;

            var client = (ITankAClient)TankAGame.ThisGame.Services.GetService(typeof(ITankAClient));
            if (client != null)
            {
                client.RequestFire(t.TankIndex);
            }
            else
            {
                var host = (ITankAHost)TankAGame.ThisGame.Services.GetService(typeof(ITankAHost));
                if (host != null)
                {
                    Point pos = new Point((int)t.Position.X, (int)t.Position.Y);
                    host.AckFire(t.TankIndex, pos);
                }

                t.Fire();
            }
        }
        public void ChangeTankInMapState(byte tankIndex, Vector2 position, Direction direction)
        {
            if (!tanks.ContainsKey(tankIndex))
            {
                return;
            }
            Tank t = tanks[tankIndex];
            if (position.X != -1)
            {
                // Client - only change position when really needed.
                Vector2 myPos = t.Position;
                if (NeedCorrectPosition(myPos, position,
                    t.DestinedDirection, direction, t.Speed))
                {
                    t.DestinedDirection = direction;
                    t.Position = new Vector2(position.X, position.Y);
                }
            }
            else
            {
                // Host - always change direction.
                t.DestinedDirection = direction;
            }
        }
        public Point GetTankPosition(byte tankIndex)
        {
            Point ret = new Point(-1, -1);
            if (tanks.ContainsKey(tankIndex))
            {
                ret.X = (int)tanks[tankIndex].Position.X;
                ret.Y = (int)tanks[tankIndex].Position.Y;
            }
            return ret;
        }
        public IEnumerable<TankInMapState> GetAllTanksInMapState()
        {
            var ret = from tank in tanks.Values
                      select tank.InMapState;
            return ret;
        }
        public void Fire(byte tankIndex, Point position)
        {
            if (!tanks.ContainsKey(tankIndex))
                return;
            Tank t = tanks[tankIndex];

            if (position.X != -1)
            {
                // Need fix position.
                t.Position = new Vector2(position.X, position.Y);
            }

            t.ForceFire();
        }
        public void FilterSurvival(List<byte> indexes)
        {
            var x = from tankIndex in tanks.Keys
                                where !indexes.Contains(tankIndex)
                                select tankIndex;
            var toBeDestroyed = x.ToList();
            foreach (byte i in toBeDestroyed)
                tanks[i].Health = 0;
        }
        public void AckTankHealthChanged(byte tankIndex, int health)
        {
            if (tanks.ContainsKey(tankIndex))
                tanks[tankIndex].Health = health;
        }
        public void AckTankFreeze(byte tankIndex, int freezeTime)
        {
            tanks[tankIndex].FreezeTime = freezeTime;
        }
        public void AckBlockSpriteHealthChanged(Point position, int health)
        {
            blockMap[position].Health = health;
        }
        public void AckItemActivation(byte itemIndex, byte tankIndex)
        {
            items.Single(item => item.ItemIndex == itemIndex).ForceActive(tanks, tankIndex);
        }
        public void NewCSRound()
        {
            // Deactive all items.
            foreach (var item in items)
                item.ForceDeactive(tanks);
            // Clear all items.
            items.Clear();
            // Set all remaining tank invul.
            foreach (var tank in tanks.Values)
                tank.InvulnerableTime = 3000;
        }
        public Dictionary<byte, Tank> AllTanksInMap()
        {
            return tanks;
        }
        public List<Item> AllItemsInMap()
        {
            return items;
        }
        public Vector2 RandomTankPosition()
        {
            int i = TankAGame.Random.Next(tankSpawnLocations.Count);
            Vector2 ret;
            Rectangle rec = new Rectangle(0, 0, 50, 50);
            bool pass;
            // Verify a tank can really spawn here.
            do
            {
                if (i == tankSpawnLocations.Count)
                    i = 0;

                ret = tankSpawnLocations[i++];
                rec.X = (int)ret.X;
                rec.Y = (int)ret.Y;
                pass = true;
                foreach (var t in tanks.Values)
                    if (rec.Intersects(t.OccupiedArea))
                    {
                        pass = false;
                        break;
                    }
            } while (!pass);
            return ret;
        }
        public string MapName()
        {
            return mapName;
        }
        public List<Point> ReachablePoints()
        {
            return reachablePoints;
        }
        public Dictionary<Point, BlockSprite> BlockMap()
        {
            return blockMap;
        }
        public void ChangeBlockSpriteHealth(Point point, int health)
        {
            blockMap[point].Health = health;
        }
        public void FilterRemainingBlockSprite(List<Point> points)
        {
            var x = from cell in blockMap.Values
                    where cell.Health != null && !points.Contains(cell.MapPoint)
                    select cell.MapPoint;
            var toBeRemoved = x.ToList();
            foreach (var p in toBeRemoved)
                blockMap.Remove(p);
        }
        
        public void SaveGame(string fileName)
        {
            TextWriter tw = new StreamWriter(fileName);
            XmlSerializer xs = new XmlSerializer(typeof(SaveGameData));

            SaveGameData saveGameData = new SaveGameData();
            saveGameData.BackgroundMap = backgroundMap;
            saveGameData.ForegroundMap = foregroundMap;
            saveGameData.OngroundMap = new List<string>();

            List<StringBuilder> tempOngroundMap = new List<StringBuilder>();
            for (int i = 0; i < 30; ++i)
                tempOngroundMap.Add(new StringBuilder("0000000000000000000000000000000000000000"));


            saveGameData.TankStates = new List<TankState>();
            saveGameData.AmmoStates = new List<AmmoState>();
            saveGameData.wallStates = new List<BlockSpriteState>();
            saveGameData.EffectStates = new List<EffectState>();
            saveGameData.ItemStates = new List<ItemState>();

            // Tank data.
            foreach (Tank t in tanks.Values)
                saveGameData.TankStates.Add(t.State);
            // Ammo data.
            foreach (Ammo a in ammo)
                saveGameData.AmmoStates.Add(a.State);
            // BlockSprite data.
            foreach (BlockSprite bs in blockMap.Values)
                if (bs.Type != BlockSpriteType.Invisible && bs.Type != BlockSpriteType.Invulnerable)
                    saveGameData.wallStates.Add(bs.State);
                else
                {
                    switch (bs.Type)
                    {
                        case BlockSpriteType.Invulnerable:
                            tempOngroundMap[bs.MapPoint.Y][bs.MapPoint.X] = 'x';
                            break;
                        case BlockSpriteType.Invisible:
                            tempOngroundMap[bs.MapPoint.Y][bs.MapPoint.X] = 'y';
                            break;
                    }
                }
            foreach (var sb in tempOngroundMap)
                saveGameData.OngroundMap.Add(sb.ToString());
            // Effect data.
            foreach (Effect e in effects)
                saveGameData.EffectStates.Add(e.State);
            // Item data.
            foreach (Item it in items)
                saveGameData.ItemStates.Add(it.State);
            // Next item time.
            saveGameData.nextItemTime = gameLogic.NextItemTime;

            xs.Serialize(tw, saveGameData);
            tw.Close();
        }
        public void LoadGame(String fileName)
        {
            TextReader tr = new StreamReader(fileName);
            XmlSerializer xs = new XmlSerializer(typeof(SaveGameData));
            SaveGameData saveGameData = new SaveGameData();
            saveGameData = (SaveGameData)xs.Deserialize(tr);
            tr.Close();

            backgroundMap = saveGameData.BackgroundMap;
            foregroundMap = saveGameData.ForegroundMap;
            blockMap.Clear();
            ConstructOngroundMap(saveGameData.OngroundMap, false);

            // Recreate tanks.
            tanks.Clear();
            userTanks.Clear();
            foreach (TankState ts in saveGameData.TankStates)
                Factory.CreateTank(ts, tanks);
            // Ammo.
            ammo.Clear();
            foreach (AmmoState ammoState in saveGameData.AmmoStates)
                Factory.CreateAmmo(ammoState);
            // BlockSprite.
            foreach (BlockSpriteState bss in saveGameData.wallStates)
                Factory.CreateBlockSprite(bss);
            // Effect.
            effects.Clear();
            foreach (EffectState e in saveGameData.EffectStates)
                Factory.CreateEffect(e);
            // Item.
            items.Clear();
            foreach (ItemState itemState in saveGameData.ItemStates)
                Factory.CreateItem(itemState, items);
            // Next item time.
            gameLogic.NextItemTime = saveGameData.nextItemTime;
        }
        public void QuickSave()
        {
            SaveGame("quicksave.xml");
        }
        public void QuickLoad()
        {
            LoadGame("quicksave.xml");
        }
        // Update and draw
        public void Update(GameTime gameTime)
        {
            gameLogic.Update(gameTime);
            if (single)
                bot.Update(gameTime);
            // Do the update for exist sprites.
            for (int i = ammo.Count - 1; i >= 0; --i)
                if (ammo[i].Destroyed)
                    ammo.RemoveAt(i);
                else
                    ammo[i].Update(gameTime, blockMap, ammo, tanks, (byte)i);

            foreach (BlockSprite b in blockMap.Values)
                b.Update(gameTime);

            foreach (Tank t in tanks.Values)
                t.Update(gameTime, blockMap, tanks, t.TankIndex);

            for (int i = effects.Count - 1; i >= 0; --i)
                effects[i].Update(gameTime, tanks);

            for (int i = items.Count - 1; i >= 0; --i)
                items[i].Update(gameTime, tanks);
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw background and foreground map
            Vector2 pos;
            for (int i = 0; i < backgroundMap.Count(); ++i)
                for (int j = 0; j < backgroundMap[i].Length; ++j)
                {
                    pos.X = j * cellSize;
                    pos.Y = i * cellSize;
                    if (backgroundMap[i][j] != '0')
                    {
                        spriteBatch.Draw(charToSprite[backgroundMap[i][j]].Texture, pos,
                            charToSprite[backgroundMap[i][j]].SourceRectangle, Color.White,
                            0, Vector2.Zero, 1f, SpriteEffects.None,
                            Map.Layer0);
                    }
                    if (foregroundMap[i][j] != '0')
                    {
                        spriteBatch.Draw(charToSprite[foregroundMap[i][j]].Texture, pos,
                            charToSprite[foregroundMap[i][j]].SourceRectangle, Color.White,
                            0, Vector2.Zero, 1f, SpriteEffects.None,
                            Map.Layer2);
                    }
                }
            // Draw onground block sprites
            foreach (Ammo a in ammo)
                a.Draw(spriteBatch);
            foreach (BlockSprite b in blockMap.Values)
                b.Draw(spriteBatch);
            foreach (Tank t in tanks.Values)
                t.Draw(spriteBatch);
            foreach (Effect e in effects)
                e.Draw(spriteBatch);
            foreach (Item i in items)
                i.Draw(spriteBatch);
            // Peer info.
            TankAConfig config = TankAConfig.Instance;
            if (config.displayPeerInfo)
            {
                netStat.Draw(spriteBatch);
            }
        }
        // Private methods
        void OnSpriteCreated(Sprite s)
        {
            BlockSprite b = s as BlockSprite;
            if (b != null)
            {
                blockMap.Add(b.MapPoint, b);
                return;
            }

            Ammo a = s as Ammo;
            if (a != null)
            {
                ammo.Add(a);
                return;
            }

            Tank t = s as Tank;
            if (t != null)
            {
                tanks.Add(t.TankIndex, t);
                UserTank uT = t as UserTank;
                if (uT != null && uT.PlayerIndex != 0xfe)
                {
                    // Local user-controlled tank.
                    userTanks.Add(uT.PlayerIndex, uT);
                }
                return;
            }

            Effect e = s as Effect;
            if (e != null)
            {
                effects.Add(e);
                return;
            }

            Item i = s as Item;
            if (i != null)
            {
                items.Add(i);
            }
        }
        void OnSpriteDestroyed(Sprite s)
        {
            BlockSprite b = s as BlockSprite;
            if (b != null)
            {
                blockMap.Remove(b.MapPoint);
                return;
            }

            Tank t = s as Tank;
            if (t != null)
            {
                tanks.Remove(t.TankIndex);
                UserTank uT = t as UserTank;
                if (uT != null && uT.PlayerIndex != 0xfe)
                {
                    // Local user-controlled tank.
                    byte i = 0;
                    foreach (byte j in userTanks.Keys)
                        if (userTanks[j] == uT)
                        {
                            i = j;
                            break;
                        }
                    userTanks.Remove(i);
                }
                return;
            }

            Effect e = s as Effect;
            if (e != null)
            {
                effects.Remove(e);
                return;
            }

            Item item = s as Item;
            if (item != null)
            {
                items.Remove(item);
                return;
            }

        }
        void InitializeBackgroundAndForeground(ContentManager content)
        {
            // Background textures
            SpriteInfo si = new SpriteInfo(content.Load<Texture2D>(@"Images/GamePlay/Background/BGsnow"),
                new Point(1000, 750), null);
            Sprite snowBG = new Sprite(si);
            charToSprite.Add('A', snowBG);
            
            si.texture = content.Load<Texture2D>(@"Images/GamePlay/Background/BGfire");
            Sprite fireBG = new Sprite(si);
            charToSprite.Add('B', fireBG);

            si.texture = content.Load<Texture2D>(@"Images/GamePlay/Background/BG");
            Sprite bg = new Sprite(si);
            charToSprite.Add('C', bg);

            // Foreground textures
            SpriteInfo fSI = new SpriteInfo(content.Load<Texture2D>(@"Images/GamePlay/Foreground/tree3"),
                new Point(100, 100), null);
            Sprite tree = new Sprite(fSI);
            charToSprite.Add('a', tree);

        }
        void InitializeOngroundDic()
        {
            // Generate charToBlockSpriteType dic
            charToBlockSpriteType.Add('a', BlockSpriteType.Wall);
            charToBlockSpriteType.Add('b', BlockSpriteType.FireWall);
            charToBlockSpriteType.Add('x', BlockSpriteType.Invulnerable);
            charToBlockSpriteType.Add('y', BlockSpriteType.Invisible);
        }
        void ConstructOngroundMap(List<string> ongroundMap, bool newMap)
        {
            BlockSpriteType bt = BlockSpriteType.Wall;
            for (int i = 0; i < ongroundMap.Count; ++i)
            {
                for (int j = 0; j < ongroundMap[i].Length; ++j)
                {
                    if (newMap)
                        reachablePoints.Add(new Point(j, i));
                    if (ongroundMap[i][j] != '0')
                    {
                        bt = charToBlockSpriteType[ongroundMap[i][j]];
                        BlockSpriteState state = new BlockSpriteState()
                        {
                            type = bt,
                            health = null,
                            maxHealth = null,
                            mapPoint = new Point(j, i)
                        };
                        Factory.CreateBlockSprite(state);
                        if (newMap && (ongroundMap[i][j] == 'x' || ongroundMap[i][j] == 'y'))
                        {
                            reachablePoints.RemoveAt(reachablePoints.Count - 1);
                        }
                    }
                }

            }
        }
        bool NeedCorrectPosition(Vector2 myPosition, Vector2 hostPosition,
            Direction myDirection, Direction hostDirection, byte speed)
        {
            // Different direction - always correct my position.
            if (myDirection != hostDirection)
                return true;

            Vector2 diff = new Vector2(hostPosition.X - myPosition.X, hostPosition.Y - myPosition.Y);
            if (diff == Vector2.Zero)
                return false;
            // hostPosition should always be ahead of mine, but not too far ahead.
            switch (hostDirection)
            {
                case Direction.Up:
                    if (diff.Y >= -speed)
                        return false;
                    break;
                case Direction.Down:
                    if (diff.Y <= speed)
                        return false;
                    break;
                case Direction.Left:
                    if (diff.X >= -speed)
                        return false;
                    break;
                case Direction.Right:
                    if (diff.X <= speed)
                        return false;
                    break;
            }
            return true;
        }

        // Private members
        Dictionary<char, Sprite> charToSprite = new Dictionary<char, Sprite>();
        Dictionary<char, BlockSpriteType> charToBlockSpriteType = new Dictionary<char, BlockSpriteType>();

        List<string> backgroundMap = new List<string>();
        Dictionary<Point, BlockSprite> blockMap = new Dictionary<Point, BlockSprite>();
        List<string> foregroundMap = new List<string>();
        List<Point> reachablePoints = new List<Point>();

        Factory factory;
        BotManager bot;
        GameLogic gameLogic;
        NetStat netStat;

        Dictionary<byte, Tank> tanks = new Dictionary<byte, Tank>();
        Dictionary<byte, UserTank> userTanks = new Dictionary<byte, UserTank>();
        List<Ammo> ammo = new List<Ammo>();
        List<Effect> effects = new List<Effect>();
        List<Item> items = new List<Item>();

        List<Vector2> tankSpawnLocations = new List<Vector2>();

        string mapName;

        bool single;

        const int cellSize = 25;
        const float layer0 = 0.8f; // Bottom layer.
        const float layer1 = 0.5f; // Middle layer.
        const float layer2 = 0.2f;  // Top layer.
        // Properties
        static public int CellSize
        {
            get { return cellSize; }
        }
        static public float Layer0
        {
            get { return layer0; }
        }
        static public float Layer1
        {
            get { return layer1; }
        }
        static public float Layer2
        {
            get { return layer2; }
        }
    }
}
