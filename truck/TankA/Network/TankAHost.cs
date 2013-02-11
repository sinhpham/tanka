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
    interface ITankAHost
    {
        Dictionary<IPAddress, ClientInfo> AllClientsInfo();
        ClientInfo GetClientInfo(IPAddress clientIp);

        void AckFire(byte tankIndex, Point position);
        void AnnounceTanksCreation(List<IPAddress> clientIp, List<byte> tankIndexes, List<Point> positions, bool newCSRound);
        void AnnounceTankFreeze(byte tankIndex, int freezeTime);
        void AnnounceItemCreation(ItemType type, byte index, Point position);
        void AnnounceItemActivation(byte itemIndex, byte tankIndex);
        void AckHealthChanged(Sprite s, int health);
        void AckKill(byte killer, byte killed);
    }

    class ClientInfo
    {
        public ClientInfo(byte clientIndex)
        {
            this.clientIndex = clientIndex;
        }
        public byte clientIndex;
        public byte tankIndex;
        public byte team;
        public bool isPlaying;
    }

    class TankAHost : ITankAHost, IDisposable
    {
        public TankAHost(GameLogicMode hostMode)
        {
            NetConfiguration netConfig = new NetConfiguration("TankA");
            netConfig.MaxConnections = 10;
            netConfig.Port = NetworkManager.Port;

            host = new NetServer(netConfig);
            readBuffer = host.CreateBuffer();
            currentFrame = framePerUpdate;

            this.hostMode = hostMode;
        }

        public void Initialize()
        {
            // Clear
            tankHealth.Clear();
            blockSpriteHealth.Clear();
            clientInfo.Clear();
            killers.Clear();
            victims.Clear();


            clientInfo.Add(IPAddress.Loopback, new ClientInfo(0));

            ict = (IControlTank)TankAGame.ThisGame.Services.GetService(typeof(IControlTank));
            map = (IMap)TankAGame.ThisGame.Services.GetService(typeof(IMap));
            netStat = (INetStat)TankAGame.ThisGame.Services.GetService(typeof(INetStat));
            gameLogic = (IGameLogic)TankAGame.ThisGame.Services.GetService(typeof(IGameLogic));
        }
        public void Dispose()
        {
            host.Dispose();
        }

        public void Update(GameTime gameTime)
        {
            NetMessageType type;
            NetConnection conn;
            // Always read messages.
            while (host.ReadMessage(readBuffer, out type, out conn))
            {
                switch (type)
                {
                    case NetMessageType.ConnectionApproval:
                        if (clientInfo.ContainsKey(conn.RemoteEndpoint.Address))
                            conn.Disapprove("Already connected");
                        else if (clientInfo.Count == maxNumberOfClients)
                            conn.Disapprove("Maximum number of players reached");
                        else
                            conn.Approve();
                        break;
                    case NetMessageType.StatusChanged:
                        string newStatus = readBuffer.ReadString();
                        Debug.Write("New connection status: ");
                        Debug.WriteLine(newStatus);
                        if (newStatus == "Connected")
                        {
                            // New client, auto generate client index.
                            byte cIndex = 0;
                            bool duplicated;
                            do
                            {
                                cIndex = (byte)TankAGame.Random.Next(255);
                                duplicated = false;
                                // Check if newly generated team is exist.
                                foreach (var cInfo in clientInfo.Values)
                                    if (cInfo.clientIndex == cIndex)
                                    {
                                        duplicated = true;
                                        break;
                                    }
                            } while (duplicated);
                            clientInfo.Add(conn.RemoteEndpoint.Address, new ClientInfo(cIndex));

                            if (hostMode == GameLogicMode.HostCS)
                            {
                                // TODO: implement hostCS mode.
                            }
                            if (hostMode == GameLogicMode.HostQuake)
                            {
                                // Team is the same as client index, sice each player
                                // belongs to different team.
                                clientInfo[conn.RemoteEndpoint.Address].team = cIndex;
                            }
                        }
                        if (newStatus == "Quit" || newStatus == "Connection timed out")
                        {
                            if (clientInfo.ContainsKey(conn.RemoteEndpoint.Address))
                            {
                                byte tankIndex = clientInfo[conn.RemoteEndpoint.Address].tankIndex;
                                ict.AckTankHealthChanged(tankIndex, 0);
                                gameLogic.ClientLeftGame(conn.RemoteEndpoint.Address, clientInfo[conn.RemoteEndpoint.Address].team);
                                clientInfo.Remove(conn.RemoteEndpoint.Address);
                                netStat.ClientLeft(conn.RemoteEndpoint.Address);
                            }
                        }
                        break;
                    case NetMessageType.Data:
                        ProcessMessage(conn);
                        break;
                }
            }

            // Update state to all client every x frames.
            --currentFrame;
            if (currentFrame == 0)
            {
                var allTanksInMapState = ict.GetAllTanksInMapState();
                // Construct send packet.
                NetBuffer sendBuffer = host.CreateBuffer();
                sendBuffer.Write((byte)MessageType.StateUpdate);
                sendBuffer.Write((byte)allTanksInMapState.Count());
                foreach (var state in allTanksInMapState)
                {
                    sendBuffer.Write(state.tankIndex);
                    sendBuffer.Write((short)state.position.X);
                    sendBuffer.Write((short)state.position.Y);
                    sendBuffer.Write((byte)state.destinedDirection);
                }

                SendToAllPlayingClients(sendBuffer, NetChannel.UnreliableInOrder1);
                AnnounceHealthChanged();
                AnnounceStatChanged();
                currentFrame = framePerUpdate;
            }
        }

        public void Start()
        {
            host.Start();
            host.SetMessageTypeEnabled(NetMessageType.ConnectionApproval, true);

            var config = TankAConfig.Instance;
            netStat.SetMode(hostMode);
            netStat.NewClient(IPAddress.Loopback, config.playerName, 0);
        }
        public void Stop()
        {
            host.Shutdown("Host quit");
        }
        public Dictionary<IPAddress, ClientInfo> AllClientsInfo()
        {
            return clientInfo;
        }
        public ClientInfo GetClientInfo(IPAddress clientIp)
        {
            if (clientInfo.ContainsKey(clientIp))
                return clientInfo[clientIp];
            return null;
        }
        public void AckFire(byte tankIndex, Point position)
        {
            NetBuffer sendBuffer = host.CreateBuffer();
            sendBuffer.Write((byte)MessageType.FireAck);
            sendBuffer.Write(tankIndex);
            sendBuffer.Write((short)position.X);
            sendBuffer.Write((short)position.Y);

            SendToAllPlayingClients(sendBuffer, NetChannel.ReliableInOrder1);
        }
        public void AnnounceTanksCreation(List<IPAddress> clientIp, List<byte> tankIndexes, List<Point> positions, bool newCSRound)
        {
            NetBuffer sendBuffer = host.CreateBuffer();
            sendBuffer.Write((byte)MessageType.CreateTank);

            sendBuffer.Write(newCSRound);
            if (newCSRound)
            {
                Point teamScore = netStat.TeamScore();
                sendBuffer.Write((short)teamScore.X);
                sendBuffer.Write((short)teamScore.Y);
            }
            sendBuffer.Write((byte)clientIp.Count);
            for (int i = 0; i < clientIp.Count; ++i)
            {
                sendBuffer.Write(clientInfo[clientIp[i]].clientIndex);
                sendBuffer.Write(tankIndexes[i]);
                sendBuffer.Write((short)positions[i].X);
                sendBuffer.Write((short)positions[i].Y);
                sendBuffer.Write(clientInfo[clientIp[i]].team);

                clientInfo[clientIp[i]].tankIndex = tankIndexes[i];
            }

            SendToAllPlayingClients(sendBuffer, NetChannel.ReliableUnordered);
        }
        public void AnnounceTankFreeze(byte tankIndex, int freezeTime)
        {
            NetBuffer sendBuffer = host.CreateBuffer();
            sendBuffer.Write((byte)MessageType.TankFreezeAnn);

            sendBuffer.Write(tankIndex);
            sendBuffer.Write((short)freezeTime);

            SendToAllPlayingClients(sendBuffer, NetChannel.ReliableUnordered);
        }
        public void AnnounceItemCreation(ItemType type, byte index, Point position)
        {
            NetBuffer sendBuffer = host.CreateBuffer();
            sendBuffer.Write((byte)MessageType.CreateItem);

            sendBuffer.Write((byte)type);
            sendBuffer.Write(index);
            sendBuffer.Write((short)position.X);
            sendBuffer.Write((short)position.Y);

            SendToAllPlayingClients(sendBuffer, NetChannel.ReliableUnordered);
        }
        public void AnnounceItemActivation(byte itemIndex, byte tankIndex)
        {
            NetBuffer sendBuffer = host.CreateBuffer();
            sendBuffer.Write((byte)MessageType.ActiveItem);

            sendBuffer.Write(itemIndex);
            sendBuffer.Write(tankIndex);

            SendToAllPlayingClients(sendBuffer, NetChannel.ReliableUnordered);
        }
        public void AckHealthChanged(Sprite s, int health)
        {
            Tank t = s as Tank;
            if (t != null)
            {
                if (tankHealth.ContainsKey(t.TankIndex))
                {
                    tankHealth[t.TankIndex] = (byte)health;
                }
                else
                {
                    tankHealth.Add(t.TankIndex, (byte)health);
                }
                return;
            }
            BlockSprite b = s as BlockSprite;
            if (b != null)
            {
                if (blockSpriteHealth.ContainsKey(b.MapPoint))
                    blockSpriteHealth[b.MapPoint] = (short)health;
                else
                    blockSpriteHealth.Add(b.MapPoint, (short)health);
                return;
            }
            Ammo a = s as Ammo;
            if (a != null)
            {

                return;
            }
            throw new NotImplementedException();
        }
        public void AckKill(byte killer, byte killed)
        {
            IPAddress ipKiller = clientInfo.Single(x => x.Value.tankIndex == killer).Key;
            IPAddress ipKilled = clientInfo.Single(x => x.Value.tankIndex == killed).Key;

            if (!killers.Contains(ipKiller))
                killers.Add(ipKiller);
            if (!victims.Contains(ipKilled))
                victims.Add(ipKilled);

            netStat.AckKill(ipKiller, ipKilled);
        }
        // Private methods
        void ProcessMessage(NetConnection conn)
        {
            // Called whener host has received a message.
            byte tankIndex;
            Direction direction;
            Point pos;
            byte clientIndex;
            string playerName;

            MessageType type = (MessageType)readBuffer.ReadByte();
            switch (type)
            {
                case MessageType.MoveReq:
                    tankIndex = readBuffer.ReadByte();
                    direction = (Direction)readBuffer.ReadByte();

                    ict.ChangeTankInMapState(tankIndex, new Vector2(-1, -1), direction);
                    break;
                case MessageType.FireReq:
                    Debug.WriteLine("Fire request received");
                    tankIndex = readBuffer.ReadByte();
                    pos = ict.GetTankPosition(tankIndex);
                    AckFire(tankIndex, pos);

                    ict.Fire(tankIndex, new Point(-1, -1));
                    break;
                case MessageType.ClientInfoReq:
                    byte clientTeam = readBuffer.ReadByte();
                    playerName = readBuffer.ReadString();
                    if (hostMode == GameLogicMode.HostCS)
                        clientInfo[conn.RemoteEndpoint.Address].team = clientTeam;
                    netStat.NewClient(conn.RemoteEndpoint.Address, playerName, clientTeam);
                    AnnounceInitialClientInfo(conn, clientInfo[conn.RemoteEndpoint.Address].clientIndex);
                    break;
                case MessageType.MapInfoReq:
                    clientIndex = readBuffer.ReadByte();
                    AnnounceInitialMapInfo(conn);
                    break;
                case MessageType.BlockSpriteInfoReq:
                    AnnounceBlockSpriteInfo(conn);
                    break;
            }
        }
        void AnnounceHealthChanged()
        {
            if (tankHealth.Count == 0 && blockSpriteHealth.Count == 0)
                return;

            NetBuffer sendBuffer = host.CreateBuffer();
            sendBuffer.Write((byte)MessageType.HealthAnn);

            sendBuffer.Write((byte)tankHealth.Count);
            foreach (byte index in tankHealth.Keys)
            {
                sendBuffer.Write(index);
                sendBuffer.Write(tankHealth[index]);
            }

            sendBuffer.Write((byte)blockSpriteHealth.Count);
            foreach (Point p in blockSpriteHealth.Keys)
            {
                sendBuffer.Write((byte)p.X);
                sendBuffer.Write((byte)p.Y);
                sendBuffer.Write(blockSpriteHealth[p]);
            }

            SendToAllPlayingClients(sendBuffer, NetChannel.ReliableInOrder3);
            tankHealth.Clear();
            blockSpriteHealth.Clear();
        }
        void AnnounceStatChanged()
        {
            if (killers.Count == 0)
                return;

            NetBuffer sendBuffer = host.CreateBuffer();
            sendBuffer.Write((byte)MessageType.StatAnn);

            sendBuffer.Write((byte)killers.Count);
            foreach (var killer in killers)
            {
                sendBuffer.Write(killer.GetAddressBytes());
                sendBuffer.Write(netStat.GetKill(killer));
            }
            sendBuffer.Write((byte)victims.Count);
            foreach (var victim in victims)
            {
                sendBuffer.Write(victim.GetAddressBytes());
                sendBuffer.Write(netStat.GetDeath(victim));
            }

            SendToAllPlayingClients(sendBuffer, NetChannel.ReliableUnordered);
        }
        void AnnounceInitialClientInfo(NetConnection conn, byte index)
        {
            NetBuffer sendBuffer = host.CreateBuffer();
            sendBuffer.Write((byte)MessageType.ClientInfoAnn);
            // Client's index.
            sendBuffer.Write(index);
            // Game mode.
            sendBuffer.Write((byte)hostMode);
            // Map name.
            sendBuffer.Write(map.MapName());

            host.SendMessage(sendBuffer, conn, NetChannel.ReliableUnordered);
        }
        void AnnounceInitialMapInfo(NetConnection conn)
        {
            NetBuffer sendBuffer = host.CreateBuffer();
            sendBuffer.Write((byte)MessageType.MapInfoAnn);
            // Tank info.
            var allTanks = ict.AllTanksInMap();
            sendBuffer.Write((byte)allTanks.Count);
            foreach (var tank in allTanks.Values)
            {
                sendBuffer.Write(tank.TankIndex);
                sendBuffer.Write((short)tank.Position.X);
                sendBuffer.Write((short)tank.Position.Y);
                sendBuffer.Write(tank.Team);
                sendBuffer.Write((byte)tank.DestinedDirection);
                sendBuffer.Write((byte)tank.GunDirection);
                sendBuffer.Write(tank.Speed);
                sendBuffer.Write(tank.AmmoSpeed);
                sendBuffer.Write(tank.Armor);
                sendBuffer.Write((short)tank.TimeBetweenFires);
                sendBuffer.Write((byte)tank.Health);
                sendBuffer.Write((short)tank.InvulnerableTime);
                sendBuffer.Write((byte)tank.AmmoType);
            }
            // Item info.
            var allItemsInMap = map.AllItemsInMap();
            sendBuffer.Write((byte)allItemsInMap.Count);
            foreach (var item in allItemsInMap)
            {
                var state = item.State;
                sendBuffer.Write(state.itemIndex);
                sendBuffer.Write((byte)state.type);
                sendBuffer.Write((short)state.position.X);
                sendBuffer.Write((short)state.position.Y);
                sendBuffer.Write((short)state.waitingTime);
                sendBuffer.Write((short)state.activeTime);
                sendBuffer.Write(state.affectedTankIndex);
                sendBuffer.Write(state.oldValue);
            }
            // Stat info.
            var stats = netStat.Stat();
            sendBuffer.Write((byte)stats.Count);
            foreach (var stat in stats)
            {
                sendBuffer.Write(stat.Key.GetAddressBytes());
                sendBuffer.Write(stat.Value.name);
                sendBuffer.Write(stat.Value.team);
                sendBuffer.Write(stat.Value.kill);
                sendBuffer.Write(stat.Value.death);
            }

            host.SendMessage(sendBuffer, conn, NetChannel.ReliableUnordered);
        }

        void AnnounceBlockSpriteInfo(NetConnection conn)
        {
            NetBuffer sendBuffer = host.CreateBuffer();
            sendBuffer.Write((byte)MessageType.BlockSpriteInfoAnn);

            var blocks = from cell in map.BlockMap()
                         where cell.Value.Health != null
                         orderby cell.Value.Health
                         group cell.Key by cell.Value.Health into g
                         select new { Health = g.Key, Cells = g, Count = g.Count() };
            int counter = blocks.Count();
            sendBuffer.Write((byte)counter);
            foreach (var b in blocks)
            {
                sendBuffer.Write((short)b.Health);
                sendBuffer.Write((short)b.Count);
                foreach (var cell in b.Cells)
                {
                    sendBuffer.Write((byte)cell.X);
                    sendBuffer.Write((byte)cell.Y);
                }
            }

            host.SendMessage(sendBuffer, conn, NetChannel.ReliableUnordered);
            clientInfo[conn.RemoteEndpoint.Address].isPlaying = true;
            var gl = (IGameLogic)TankAGame.ThisGame.Services.GetService(typeof(IGameLogic));
            gl.NewClientJoinedGame(conn.RemoteEndpoint.Address, clientInfo[conn.RemoteEndpoint.Address].team);
        }
        void SendToAllPlayingClients(NetBuffer sendBuffer, NetChannel channel)
        {
            var connections = host.Connections;
            foreach (var conn in connections)
            {
                if (conn.Status == NetConnectionStatus.Connected &&
                    clientInfo.ContainsKey(conn.RemoteEndpoint.Address) &&
                    clientInfo[conn.RemoteEndpoint.Address].isPlaying)
                {
                    host.SendMessage(sendBuffer, conn, channel);
                }
            }
        }
        // Private members
        NetServer host;
        NetBuffer readBuffer;

        readonly byte framePerUpdate = 3;
        byte currentFrame;

        Dictionary<byte, byte> tankHealth = new Dictionary<byte, byte>();
        Dictionary<Point, short> blockSpriteHealth = new Dictionary<Point, short>();

        Dictionary<IPAddress, ClientInfo> clientInfo = new Dictionary<IPAddress, ClientInfo>();
        const int maxNumberOfClients = 10;

        List<IPAddress> killers = new List<IPAddress>();
        List<IPAddress> victims = new List<IPAddress>();

        IControlTank ict;
        IMap map;
        INetStat netStat;
        IGameLogic gameLogic;

        GameLogicMode hostMode;
        // Properties
    }
}
