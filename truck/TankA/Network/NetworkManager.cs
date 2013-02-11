using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using TankA.GamePlay;
using Lidgren.Network;

namespace TankA.Network
{
    interface INetworkManager
    {
        void StartHost(GameLogicMode hostMode);
        void StopHost();
        void StartClient();
        void StopClient();
        void Connect(IPAddress ip);
        void Discover();
    }

    public enum MessageType
    {
        MoveReq,
        FireReq,
        FireAck,
        StateUpdate,
        CreateTank,
        TankFreezeAnn,
        CreateItem,
        ActiveItem,
        HealthAnn,
        StatAnn,
        ClientInfoReq,
        ClientInfoAnn,
        MapInfoReq,
        MapInfoAnn,
        BlockSpriteInfoReq,
        BlockSpriteInfoAnn
    }

    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class NetworkManager : Microsoft.Xna.Framework.GameComponent, INetworkManager
    {
        public NetworkManager(Game game)
            : base(game)
        {
            // TODO: Construct any child components here
            role = GameLogicMode.Undefined;
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // TODO: Add your update code here
            if (role == GameLogicMode.Undefined)
                return;

            switch (role)
            {
                case GameLogicMode.HostCS:
                    tankAHost.Update(gameTime);
                    break;
                case GameLogicMode.HostQuake:
                    tankAHost.Update(gameTime);
                    break;
                case GameLogicMode.Client:
                    tankAClient.Update(gameTime);
                    break;
            }
            base.Update(gameTime);
        }
        // INetwork interface implementation.
        public void StartHost(GameLogicMode hostMode)
        {
            tankAHost = new TankAHost(hostMode);

            tankAHost.Initialize();
            tankAHost.Start();
            role = hostMode;
            TankAGame.ThisGame.Services.AddService(typeof(ITankAHost), tankAHost);
        }
        public void StopHost()
        {
            if (tankAHost != null)
            {
                tankAHost.Stop();
                // Wait for host to properly send shutdown message.
                Thread.Sleep(100);
                tankAHost.Dispose();
                tankAHost = null;
            }

            role = GameLogicMode.Undefined;
            TankAGame.ThisGame.Services.RemoveService(typeof(ITankAHost));
        }
        public void StartClient()
        {
            tankAClient = new TankAClient();

            role = GameLogicMode.Client;
            TankAGame.ThisGame.Services.AddService(typeof(ITankAClient), tankAClient);
        }
        public void StopClient()
        {
            if (tankAClient != null)
            {
                tankAClient.Stop();
                // Wait for client to properly send shutdown message.
                Thread.Sleep(100);
                tankAClient.Dispose();
                tankAClient = null;
            }

            role = GameLogicMode.Undefined;
            TankAGame.ThisGame.Services.RemoveService(typeof(ITankAClient));
        }
        public void Connect(IPAddress ip)
        {
            tankAClient.Connect(ip);
        }
        public void Discover()
        {
            tankAClient.Discover();
        }
        // Private members
        static GameLogicMode role;
        TankAHost tankAHost;
        TankAClient tankAClient;

        const int port = 27388;
        // Properties
        public static int Port
        {
            get { return port; }
        }
        public static GameLogicMode Role
        {
            get { return role; }
        }
    }
}