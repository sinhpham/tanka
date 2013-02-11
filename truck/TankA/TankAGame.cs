using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using TankA.Network;

namespace TankA
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class TankAGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        static ScreenManager screenManager;
        TankAConfig config;
        NetworkManager network;

        public TankAGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1000;
            graphics.PreferredBackBufferHeight = 750;
            graphics.ApplyChanges();

            this.IsFixedTimeStep = false;
            random = new Random();

            Content.RootDirectory = "Content";

            config = TankAConfig.Instance;
            config.LoadFromConfigFile();

            
            screenManager = new ScreenManager(this);
            Components.Add(screenManager);

            // Active first screen
            screenManager.AddScreen(new BackgroundScreen(), null);
            screenManager.AddScreen(new MainMenuScreen(), null);

            network = new NetworkManager(this);
            Components.Add(network);
            Services.AddService(typeof(INetworkManager), network);
            thisGame = this;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
        // Properties and private members.
        public static Game ThisGame
        {
            get { return thisGame; }
        }
        static Game thisGame;
        public static Random Random
        {
            get { return random; }
        }
        static Random random;
        public static ScreenManager ScreenManager
        {
            get { return screenManager; }
        }
    }
}
