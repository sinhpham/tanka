using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using TankA.Network;

namespace TankA.GamePlay
{
    public enum GameLogicMode
    {
        Single,
        HostQuake,
        HostCS,
        Client,
        Undefined
    }

    interface IGameLogic
    {
        void BeginGame(GameLogicMode mode);
        void AckUserTankDestroyed(byte tankIndex);
        void NewClientJoinedGame(IPAddress clientIP, byte team);
        void ClientLeftGame(IPAddress clientIP, byte team);
    }

    class GameLogic : IGameLogic
    {
        // Constructor
        public GameLogic()
        {
            mode = GameLogicMode.Undefined;
            ict = (IControlTank)TankAGame.ThisGame.Services.GetService(typeof(IControlTank));
            map = (IMap)TankAGame.ThisGame.Services.GetService(typeof(IMap));

            InitializeItemData();

            nPlayers.Add(0, 1);
            nPlayers.Add(1, 0);
        }

        private void InitializeItemData()
        {
            availableItemCategories.Add(ItemCategory.TankSpeed);
            availableItemCategories.Add(ItemCategory.AmmoSpeed);
            availableItemCategories.Add(ItemCategory.Armor);
            availableItemCategories.Add(ItemCategory.TBF);
            availableItemCategories.Add(ItemCategory.Other);

            itemDic.Add(ItemCategory.TankSpeed, new List<ItemType>());
            itemDic[ItemCategory.TankSpeed].Add(ItemType.MaxTankSpeed);
            itemDic[ItemCategory.TankSpeed].Add(ItemType.IncreaseTankSpeed);
            itemDic[ItemCategory.TankSpeed].Add(ItemType.MinTankSpeed);
            itemDic[ItemCategory.TankSpeed].Add(ItemType.MinimizeEnemiesTanksSpeed);

            itemDic.Add(ItemCategory.AmmoSpeed, new List<ItemType>());
            itemDic[ItemCategory.AmmoSpeed].Add(ItemType.MaxAmmoSpeed);
            itemDic[ItemCategory.AmmoSpeed].Add(ItemType.IncreaseAmmoSpeed);
            itemDic[ItemCategory.AmmoSpeed].Add(ItemType.MinAmmoSpeed);
            itemDic[ItemCategory.AmmoSpeed].Add(ItemType.MinimizeEnemiesTanksAmmoSpeed);

            itemDic.Add(ItemCategory.Armor, new List<ItemType>());
            itemDic[ItemCategory.Armor].Add(ItemType.MaxArmor);
            itemDic[ItemCategory.Armor].Add(ItemType.IncreaseArmor);
            itemDic[ItemCategory.Armor].Add(ItemType.MinArmor);
            itemDic[ItemCategory.Armor].Add(ItemType.MinimizeEnemiesTanksArmor);

            itemDic.Add(ItemCategory.TBF, new List<ItemType>());
            itemDic[ItemCategory.TBF].Add(ItemType.MinTBF);
            itemDic[ItemCategory.TBF].Add(ItemType.DecreaseTBF);
            itemDic[ItemCategory.TBF].Add(ItemType.MaxTBF);
            itemDic[ItemCategory.TBF].Add(ItemType.MaximizeEnemiesTBF);

            itemDic.Add(ItemCategory.Other, new List<ItemType>());
            itemDic[ItemCategory.Other].Add(ItemType.Add50Health);
            itemDic[ItemCategory.Other].Add(ItemType.MakeTankInvulnerable);
            itemDic[ItemCategory.Other].Add(ItemType.ChangeAmmoToRocket);
            itemDic[ItemCategory.Other].Add(ItemType.ChangeAmmoToFireBall);
            itemDic[ItemCategory.Other].Add(ItemType.ChangeAmmoToPhiTieu);
            itemDic[ItemCategory.Other].Add(ItemType.FreezeAllEnemiesTanks);
        }
        public void BeginGame(GameLogicMode mode)
        {
            if (hasBegun)
                return;
            host = (ITankAHost)TankAGame.ThisGame.Services.GetService(typeof(ITankAHost));
            nextItemTime = 3000;
            hasBegun = true;
            this.mode = mode;
            var state = UserTank.DefaultUserTankState;
            switch (mode)
            {
                case GameLogicMode.Single:
                    for (int i = 0; i < 1; ++i)
                    {
                        state.team = (byte)i;
                        state.playerIndex = (byte)i;
                        state.position = ict.RandomTankPosition();
                        Factory.CreateTank(state, ict.AllTanksInMap());
                    }
                    break;
                case GameLogicMode.HostQuake:
                    NewNetworkGame();
                    break;
                case GameLogicMode.HostCS:
                    NewNetworkGame();
                    break;
            }
        }
        public void AckUserTankDestroyed(byte tankIndex)
        {
            switch (mode)
            {
                case GameLogicMode.HostQuake:
                    {
                        var clientsInfo = host.AllClientsInfo();
                        foreach (var ip in clientsInfo.Keys)
                            if (clientsInfo[ip].tankIndex == tankIndex)
                            {
                                respawnClientIP.Add(ip);
                                respawnTime.Add(3000);
                            }
                    }
                    break;
                case GameLogicMode.HostCS:
                    {
                        var cInfos = host.AllClientsInfo();
                        var info = cInfos.Single(i => i.Value.tankIndex == tankIndex);
                        nPlayersAlive[info.Value.team]--;
                        respawnClientIP.Add(info.Key);
                        if (nPlayersAlive[info.Value.team] == 0)
                        {
                            newRoundTime = 3000;
                            var netStat = (INetStat)TankAGame.ThisGame.Services.GetService(typeof(INetStat));
                            netStat.AckLose(info.Value.team);
                        }
                    }
                    break;
                case GameLogicMode.Single:
                    {
                        respawnClientIP.Add(IPAddress.Loopback);
                        respawnTime.Add(rTime);
                        var netStat = (INetStat)TankAGame.ThisGame.Services.GetService(typeof(INetStat));
                        netStat.AckKill(IPAddress.None, IPAddress.Loopback);
                    }
                    break;
            }
        }
        public void NewClientJoinedGame(IPAddress clientIP, byte team)
        {
            if (mode == GameLogicMode.HostQuake)
            {
                respawnClientIP.Add(clientIP);
                respawnTime.Add(rTime);
                return;
            }
            if (mode == GameLogicMode.HostCS)
            {
                nPlayers[team]++;
                respawnClientIP.Add(clientIP);

                Debug.Write(nPlayers.Count(x => x.Value == 0));

                if (nPlayers.Count(x => x.Value == 0) == 0)
                    newRoundTime = rTime;
            }

        }
        public void ClientLeftGame(IPAddress clientIP, byte team)
        {
            int i = respawnClientIP.FindIndex(x => x == clientIP);
            if (i != -1)
            {
                respawnClientIP.RemoveAt(i);
                if (mode == GameLogicMode.HostQuake)
                    respawnTime.RemoveAt(i);
            }
            if (mode == GameLogicMode.HostCS)
            {
                nPlayers[team]--;
            }
        }
        public void Update(GameTime gameTime)
        {
            switch (mode)
            {
                case GameLogicMode.Single:
                    {
                        ItemSpawn(gameTime);
                        for (int i = respawnClientIP.Count - 1; i >= 0; --i)
                        {
                            if (respawnTime[i] > 0)
                                respawnTime[i] -= gameTime.ElapsedGameTime.Milliseconds;
                            else
                            {
                                var state = UserTank.DefaultUserTankState;
                                state.position = ict.RandomTankPosition();
                                Factory.CreateTank(state, ict.AllTanksInMap());

                                respawnClientIP.RemoveAt(i);
                                respawnTime.RemoveAt(i);
                            }
                        }
                    }
                    break;
                case GameLogicMode.HostQuake:
                    {
                        ItemSpawn(gameTime);

                        var clientIp = new List<IPAddress>();
                        var tankIndexes = new List<byte>();
                        var positions = new List<Point>();

                        for (int i = respawnClientIP.Count - 1; i >= 0; --i)
                        {
                            if (respawnTime[i] > 0)
                                respawnTime[i] -= gameTime.ElapsedGameTime.Milliseconds;
                            else
                            {
                                var cIp = respawnClientIP[i];
                                clientIp.Add(cIp);

                                var state = UserTank.DefaultUserTankState;
                                var cInfo = host.GetClientInfo(cIp);
                                if (cInfo.team != 0)
                                    state.playerIndex = 0xfe;
                                state.position = ict.RandomTankPosition();
                                state.team = cInfo.team;
                                tankIndexes.Add(Factory.CreateTank(state, ict.AllTanksInMap()));
                                positions.Add(new Point((int)state.position.X, (int)state.position.Y));

                                respawnClientIP.RemoveAt(i);
                                respawnTime.RemoveAt(i);
                            }
                        }

                        if (clientIp.Count > 0)
                            host.AnnounceTanksCreation(clientIp, tankIndexes, positions, false);
                    }
                    break;
                case GameLogicMode.HostCS:
                    {
                        ItemSpawn(gameTime);
                        if (newRoundTime == null)
                            return;
                        if (newRoundTime > 0)
                            newRoundTime -= gameTime.ElapsedGameTime.Milliseconds;
                        if (newRoundTime <= 0)
                        {
                            NewCSRound();
                            newRoundTime = null;
                        }
                    }
                    break;
            }
        }

        // Private methods.
        Vector2 RandomItemPosition()
        {
            Vector2 ret = new Vector2();
            Point p = map.ReachablePoints()[TankAGame.Random.Next(map.ReachablePoints().Count)];
            ret.X = p.X * Map.CellSize;
            ret.Y = p.Y * Map.CellSize;
            return ret;
        }
        void NewNetworkGame()
        {
            // Create tanks for all players.
            var state = UserTank.DefaultUserTankState;

            var clientIp = new List<IPAddress>();
            var tankIndexes = new List<byte>();
            var positions = new List<Point>();

            var clientsInfo = host.AllClientsInfo();

            foreach (var ip in clientsInfo.Keys)
            {
                if (clientsInfo[ip].clientIndex == 0)
                    state.playerIndex = 0;
                else
                    state.playerIndex = 0xfe;

                state.position = ict.RandomTankPosition();
                state.team = clientsInfo[ip].team;

                clientIp.Add(ip);
                tankIndexes.Add(Factory.CreateTank(state, ict.AllTanksInMap()));
                positions.Add(new Point((int)state.position.X, (int)state.position.Y));
            }

            host.AnnounceTanksCreation(clientIp, tankIndexes, positions, false);
        }
        void NewCSRound()
        {
            if (nPlayers.Count(x => x.Value == 0) != 0)
                return;

            foreach (var team in nPlayers.Keys)
                nPlayersAlive[team] = nPlayers[team];
            // Clear all items.
            nextItemTime = 5000;

            map.NewCSRound();

            var cInfos = host.AllClientsInfo();
            var clientIp = new List<IPAddress>();
            var tankIndexes = new List<byte>();
            var positions = new List<Point>();

            var state = UserTank.DefaultUserTankState;
            foreach (var ip in cInfos.Keys)
            {
                if (respawnClientIP.Contains(ip))
                {
                    // Need respawn.
                    clientIp.Add(ip);
                    if (cInfos[ip].clientIndex == 0)
                        state.playerIndex = 0;
                    else
                        state.playerIndex = 0xfe;
                    state.position = ict.RandomTankPosition();
                    state.team = cInfos[ip].team;
                    tankIndexes.Add(Factory.CreateTank(state, ict.AllTanksInMap()));
                    positions.Add(new Point((int)state.position.X, (int)state.position.Y));
                }
                else
                {
                    // Need relocation.
                    ict.ChangeTankInMapState(cInfos[ip].tankIndex, ict.RandomTankPosition(), Direction.None);
                    ict.AckTankHealthChanged(cInfos[ip].tankIndex, 100);
                }
            }
            respawnClientIP.Clear();

            host.AnnounceTanksCreation(clientIp, tankIndexes, positions, true);
        }
        void ItemSpawn(GameTime gameTime)
        {
            for (int i = itemCategoryTime.Count - 1; i >= 0; --i)
            {
                if (itemCategoryTime[i] > 0)
                    itemCategoryTime[i] -= gameTime.ElapsedGameTime.Milliseconds;
                else
                {
                    availableItemCategories.Add(activeItemCategories[i]);

                    activeItemCategories.RemoveAt(i);
                    itemCategoryTime.RemoveAt(i);
                }
            }
            if (nextItemTime > 0)
                nextItemTime -= gameTime.ElapsedGameTime.Milliseconds;
            else
            {
                nextItemTime = 5000;
                ItemCategory cat = availableItemCategories[TankAGame.Random.Next(availableItemCategories.Count)];
                ItemType type = itemDic[cat][TankAGame.Random.Next(itemDic[cat].Count)];
                availableItemCategories.Remove(cat);
                activeItemCategories.Add(cat);

                itemCategoryTime.Add(21000);
                ItemState itemState = Item.DefaultItemState(type);
                itemState.position = RandomItemPosition();
                byte index = Factory.CreateItem(itemState, map.AllItemsInMap()).ItemIndex;
                if (mode == GameLogicMode.HostQuake || mode == GameLogicMode.HostCS)
                    host.AnnounceItemCreation(type, index, new Point((int)itemState.position.X, (int)itemState.position.Y));
            }
        }
        // Private members.
        const int rTime = 3000;
        bool hasBegun;
        List<IPAddress> respawnClientIP = new List<IPAddress>();
        List<int> respawnTime = new List<int>();

        Dictionary<byte, byte> nPlayersAlive = new Dictionary<byte, byte>();
        Dictionary<byte, byte> nPlayers = new Dictionary<byte, byte>();
        int? newRoundTime = null;

        int? nextItemTime = null;
        List<ItemCategory> availableItemCategories = new List<ItemCategory>();
        Dictionary<ItemCategory, List<ItemType>> itemDic = new Dictionary<ItemCategory, List<ItemType>>();
        List<ItemCategory> activeItemCategories = new List<ItemCategory>();
        List<int> itemCategoryTime = new List<int>();

        GameLogicMode mode;

        ITankAHost host;
        IControlTank ict;
        IMap map;
        // Properties
        public int? NextItemTime
        {
            get { return nextItemTime; }
            set { nextItemTime = value; }
        }
    }
}
