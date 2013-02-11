using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Lidgren.Network;
using TankA.GamePlay;

namespace TankA.Network
{
    interface ITankAClient
    {
        void ChangeCSTeam();
        byte GetCSTeam();
        void RequestInitialMapInfo();
        void NotReady();

        void RequestMove(byte tankIndex, Direction direction);
        void RequestFire(byte tankIndex);
    }

    class TankAClient : ITankAClient, IDisposable
    {
        public TankAClient()
        {
            NetConfiguration netConfig = new NetConfiguration("TankA");
            client = new NetClient(netConfig);
            readBuffer = client.CreateBuffer();

            timeBetweenFireReq = Tank.MinTBF - 50;

            clientIndex = 1;
        }
        public void Update(GameTime gameTime)
        {
            if (fireReqCoolDown > 0)
                fireReqCoolDown -= gameTime.ElapsedGameTime.Milliseconds;
            if (initialInfoReqTime > 0)
                initialInfoReqTime -= gameTime.ElapsedGameTime.Milliseconds;
            if (initialInfoReqTime <= 0)
            {
                RequestInitialClientInfo();
                initialInfoReqTime = null;
            }
            // Read update state from host.
            NetMessageType type;
            while (client.ReadMessage(readBuffer, out type))
            {
                switch (type)
                {
                    case NetMessageType.StatusChanged:
                        string newStatus = readBuffer.ReadString();
                        if (newStatus == "Connected")
                        {
                            initialInfoReqTime = 500;
                        }
                        break;
                    case NetMessageType.ServerDiscovered:
                        IPEndPoint hostEndPoint = readBuffer.ReadIPEndPoint();
                        var clientMenu = (IClientMenu)TankAGame.ThisGame.Services.GetService(typeof(IClientMenu));
                        if (clientMenu != null)
                            clientMenu.AddServer(hostEndPoint.Address);
                        break;
                    case NetMessageType.Data:
                        ProcessMessage();
                        break;
                }
            }
        }

        public NetConnectionStatus ConnectionStatus()
        {
            return client.Status;
        }
        public void Discover()
        {
            client.DiscoverLocalServers(NetworkManager.Port);
        }
        public void Connect(IPAddress ip)
        {
            if (client.Status == NetConnectionStatus.Disconnected)
            {
                client.Connect(new IPEndPoint(ip, NetworkManager.Port));
            }
        }
        public void Stop()
        {
            client.Shutdown("Quit");
        }
        public void Dispose()
        {
            client.Dispose();
        }

        public void ChangeCSTeam()
        {
            if (CSTeam == 0)
            {
                CSTeam = 1;
                return;
            }
            CSTeam = 0;
        }
        public byte GetCSTeam()
        {
            return CSTeam;
        }
        public void RequestInitialMapInfo()
        {
            // Notify host we're done loading map.
            if (client.Status != NetConnectionStatus.Connected)
                return;

            var netStat = (INetStat)TankAGame.ThisGame.Services.GetService(typeof(INetStat));
            netStat.SetMode(hostMode);

            NetBuffer sendBuffer = client.CreateBuffer();
            sendBuffer.Write((byte)MessageType.MapInfoReq);
            sendBuffer.Write(clientIndex);

            client.SendMessage(sendBuffer, NetChannel.ReliableUnordered);
        }
        public void NotReady()
        {
            ready = false;
        }
        public void RequestMove(byte tankIndex, Direction direction)
        {
            if (client.Status != NetConnectionStatus.Connected)
                return;

            NetBuffer sendBuffer = client.CreateBuffer();
            sendBuffer.Write((byte)MessageType.MoveReq);
            sendBuffer.Write(tankIndex);
            sendBuffer.Write((byte)direction);

            client.SendMessage(sendBuffer, NetChannel.ReliableInOrder1);
        }
        public void RequestFire(byte tankIndex)
        {
            if (client.Status != NetConnectionStatus.Connected || fireReqCoolDown > 0)
                return;
            fireReqCoolDown = timeBetweenFireReq;

            NetBuffer sendBuffer = client.CreateBuffer();
            sendBuffer.Write((byte)MessageType.FireReq);
            sendBuffer.Write(tankIndex);

            client.SendMessage(sendBuffer, NetChannel.ReliableInOrder1);
        }
        // Private methods
        void ProcessMessage()
        {
            var ict = (IControlTank)TankAGame.ThisGame.Services.GetService(typeof(IControlTank));
            var map = (IMap)TankAGame.ThisGame.Services.GetService(typeof(IMap));
            var netStat = (INetStat)TankAGame.ThisGame.Services.GetService(typeof(INetStat));
            MessageType type = (MessageType)readBuffer.ReadByte();

            byte tankIndex;
            Point pos = new Point();
            Direction direction;
            byte num;
            byte team;
            byte cIndex;
            byte itemIndex;
            int health;
            int freezeTime;
            List<byte> survivalIndexes = new List<byte>();

            switch (type)
            {
                case MessageType.FireAck:
                    if (!ready)
                        break;
                    tankIndex = readBuffer.ReadByte();
                    pos.X = readBuffer.ReadInt16();
                    pos.Y = readBuffer.ReadInt16();
                    ict.Fire(tankIndex, pos);
                    break;
                case MessageType.StateUpdate:
                    if (!ready)
                        break;
                    num = readBuffer.ReadByte();
                    for (int i = 0; i < num; ++i)
                    {
                        tankIndex = readBuffer.ReadByte();
                        pos.X = readBuffer.ReadInt16();
                        pos.Y = readBuffer.ReadInt16();
                        direction = (Direction)readBuffer.ReadByte();
                        ict.ChangeTankInMapState(tankIndex, new Vector2(pos.X, pos.Y), direction);

                        survivalIndexes.Add(tankIndex);
                    }
                    ict.FilterSurvival(survivalIndexes);
                    break;
                case MessageType.CreateTank:
                    if (!ready)
                        break;
                    bool newCSRound = readBuffer.ReadBoolean();
                    if (newCSRound)
                    {
                        map.NewCSRound();
                        Point teamScore;
                        teamScore.X = readBuffer.ReadInt16();
                        teamScore.Y = readBuffer.ReadInt16();
                        netStat.ChangeTeamStat(teamScore);
                    }
                    num = readBuffer.ReadByte();
                    for (int i = 0; i < num; ++i)
                    {
                        TankState state = UserTank.DefaultUserTankState;
                        cIndex = readBuffer.ReadByte();
                        tankIndex = readBuffer.ReadByte();
                        pos.X = readBuffer.ReadInt16();
                        pos.Y = readBuffer.ReadInt16();
                        team = readBuffer.ReadByte();

                        state.tankIndex = tankIndex;
                        state.position = new Vector2(pos.X, pos.Y);
                        state.team = team;
                        if (cIndex != clientIndex)
                            state.playerIndex = 0xfe;
                        Factory.CreateTank(state, ict.AllTanksInMap());
                    }
                    break;
                case MessageType.TankFreezeAnn:
                    if (!ready)
                        break;
                    tankIndex = readBuffer.ReadByte();
                    freezeTime = readBuffer.ReadInt16();
                    ict.AckTankFreeze(tankIndex, freezeTime);
                    break;
                case MessageType.CreateItem:
                    if (!ready)
                        break;
                    {
                        ItemType itemType = (ItemType)readBuffer.ReadByte();
                        ItemState state = Item.DefaultItemState(itemType);
                        state.itemIndex = readBuffer.ReadByte();
                        state.position.X = readBuffer.ReadInt16();
                        state.position.Y = readBuffer.ReadInt16();
                        // Add waiting time time to client's item to avoid
                        // network latency.
                        state.waitingTime += 200;

                        Factory.CreateItem(state, map.AllItemsInMap());
                    }
                    break;
                case MessageType.ActiveItem:
                    if (!ready)
                        break;
                    itemIndex = readBuffer.ReadByte();
                    tankIndex = readBuffer.ReadByte();
                    map.AckItemActivation(itemIndex, tankIndex);
                    break;
                case MessageType.HealthAnn:
                    if (!ready)
                        break;
                    // Tank's health.
                    num = readBuffer.ReadByte();
                    for (int i = 0; i < num; ++i)
                    {
                        tankIndex = readBuffer.ReadByte();
                        health = readBuffer.ReadByte();
                        ict.AckTankHealthChanged(tankIndex, health);
                    }
                    // BlockSprite's health.
                    num = readBuffer.ReadByte();
                    for (int i = 0; i < num; ++i)
                    {
                        pos.X = readBuffer.ReadByte();
                        pos.Y = readBuffer.ReadByte();
                        health = readBuffer.ReadInt16();
                        map.AckBlockSpriteHealthChanged(pos, health);
                    }
                    break;
                case MessageType.StatAnn:
                    if (!ready)
                        break;
                    AckStat(netStat, true);
                    AckStat(netStat, false);
                    break;
                case MessageType.ClientInfoAnn:
                    clientIndex = readBuffer.ReadByte();
                    hostMode = (GameLogicMode)readBuffer.ReadByte();
                    string mapName = readBuffer.ReadString();
                    LoadingScreen.Load(TankAGame.ScreenManager, true, PlayerIndex.One, new GameplayScreen(GameLogicMode.Client, mapName));
                    TankAGame.ThisGame.Services.RemoveService(typeof(IClientMenu));
                    break;
                case MessageType.MapInfoAnn:
                    // Tank info.
                    num = readBuffer.ReadByte();
                    for (int i = 0; i < num; ++i)
                    {
                        tankIndex = readBuffer.ReadByte();
                        pos.X = readBuffer.ReadInt16();
                        pos.Y = readBuffer.ReadInt16();
                        team = readBuffer.ReadByte();
                        direction = (Direction)readBuffer.ReadByte();
                        Direction gunDirection = (Direction)readBuffer.ReadByte();
                        byte speed = readBuffer.ReadByte();
                        byte ammoSpeed = readBuffer.ReadByte();
                        byte armor = readBuffer.ReadByte();
                        int tbf = readBuffer.ReadInt16();
                        health = readBuffer.ReadByte();
                        int invulTime = readBuffer.ReadInt16();
                        AmmoType ammoType = (AmmoType)readBuffer.ReadByte();

                        TankState state = UserTank.DefaultUserTankState;
                        state.tankIndex = tankIndex;
                        state.position = new Vector2(pos.X, pos.Y);
                        state.tankInfo.gunDirection = gunDirection;
                        state.team = team;
                        state.playerIndex = 0xfe;
                        state.tankInfo.speed = speed;

                        state.tankInfo.armor = armor;
                        state.tankInfo.timeBetweenFires = tbf;
                        state.tankInfo.health = health;
                        state.tankInfo.invulnerableTime = invulTime;
                        state.tankInfo.ammoInfo = Ammo.DefaultAmmoInfo(ammoType);
                        state.tankInfo.ammoInfo.speed = ammoSpeed;


                        Factory.CreateTank(state, ict.AllTanksInMap());
                        ict.ChangeTankInMapState(tankIndex, state.position, direction);
                    }
                    // Item info.
                    num = readBuffer.ReadByte();
                    for (int i = 0; i < num; ++i)
                    {
                        ItemState state = new ItemState();
                        state.itemIndex = readBuffer.ReadByte();
                        state.type = (ItemType)readBuffer.ReadByte();
                        state.position.X = readBuffer.ReadInt16();
                        state.position.Y = readBuffer.ReadInt16();
                        state.waitingTime = readBuffer.ReadInt16();
                        state.activeTime = readBuffer.ReadInt16();
                        state.affectedTankIndex = readBuffer.ReadByte();
                        state.oldValue = readBuffer.ReadInt32();

                        Factory.CreateItem(state, map.AllItemsInMap());
                    }
                    // Stat info.
                    num = readBuffer.ReadByte();
                    for (int i = 0; i < num; ++i)
                    {
                        byte[] b = readBuffer.ReadBytes(4);
                        IPAddress ip = new IPAddress(b);
                        string name = readBuffer.ReadString();
                        team = readBuffer.ReadByte();
                        short kill = readBuffer.ReadInt16();
                        short dealth = readBuffer.ReadInt16();
                        netStat.NewClient(ip, name, team);
                        netStat.ChangeStat(ip, kill, true);
                        netStat.ChangeStat(ip, dealth, false);
                    }
                    RequestBlockSpriteInfo();
                    break;
                case MessageType.BlockSpriteInfoAnn:
                    num = readBuffer.ReadByte();
                    var remainingBlockSprite = new List<Point>();
                    for (int i = 0; i < num; ++i)
                    {
                        health = readBuffer.ReadInt16();
                        int j = readBuffer.ReadInt16();
                        for (int k = 0; k < j; ++k)
                        {
                            pos.X = readBuffer.ReadByte();
                            pos.Y = readBuffer.ReadByte();
                            remainingBlockSprite.Add(pos);
                            map.AckBlockSpriteHealthChanged(pos, health);
                        }
                    }
                    map.FilterRemainingBlockSprite(remainingBlockSprite);
                    ready = true;
                    break;
            }
        }

        void RequestInitialClientInfo()
        {
            NetBuffer sendBuffer = client.CreateBuffer();
            sendBuffer.Write((byte)MessageType.ClientInfoReq);

            sendBuffer.Write(CSTeam);
            var config = TankAConfig.Instance;
            sendBuffer.Write(config.playerName);
            client.SendMessage(sendBuffer, NetChannel.ReliableUnordered);
        }
        void RequestBlockSpriteInfo()
        {
            NetBuffer sendBuffer = client.CreateBuffer();
            sendBuffer.Write((byte)MessageType.BlockSpriteInfoReq);

            client.SendMessage(sendBuffer, NetChannel.ReliableUnordered);
        }

        void AckStat(INetStat netStat, bool isKill)
        {
            byte num;
            num = readBuffer.ReadByte();
            for (int i = 0; i < num; ++i)
            {
                byte[] ip = readBuffer.ReadBytes(4);
                IPAddress ipaddr = new IPAddress(ip);
                short kill = readBuffer.ReadInt16();
                netStat.ChangeStat(ipaddr, kill, isKill);
            }
        }
        // Private members
        NetClient client;
        NetBuffer readBuffer;
        int fireReqCoolDown;
        byte clientIndex;
        int? initialInfoReqTime;
        readonly int timeBetweenFireReq;
        byte CSTeam;
        bool ready;

        GameLogicMode hostMode;
        // Properties
    }
}
