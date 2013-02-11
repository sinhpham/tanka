#region File Description
//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input;
using TankA.GamePlay;
using TankA.Network;
#endregion

namespace TankA
{
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>

    class GameplayScreen : GameScreen
    {
        ContentManager content;
        Map map;

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>

        public GameplayScreen(GameLogicMode mode, string mapName)
        {
            this.mode = mode;
            this.mapName = mapName;

            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
            nPlayers = 2;
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Content");
            if (mode == GameLogicMode.Single)
            {
                map = new Map(content, true);
            }
            else
            {
                map = new Map(content, false);
            }

            if (!map.LoadMap(mapName, content))
            {
                // TODO: report error - map failed to load.
                throw new NotImplementedException();
            }
            if (mode != GameLogicMode.Client)
            {
                if (mode != GameLogicMode.Single)
                {
                    // Host mode.
                    var net = (INetworkManager)TankAGame.ThisGame.Services.GetService(typeof(INetworkManager));
                    net.StartHost(mode);
                }
                else
                {
                    // Single play mode.
                    var netStat = (INetStat)TankAGame.ThisGame.Services.GetService(typeof(INetStat));
                    netStat.SetMode(GameLogicMode.HostQuake);
                    netStat.NewClient(IPAddress.Loopback, "Me", 0);
                }
                var igl = (IGameLogic)TankAGame.ThisGame.Services.GetService(typeof(IGameLogic));
                igl.BeginGame(mode);
            }
            else
            {
                // Client mode.
                var client = (ITankAClient)TankAGame.ThisGame.Services.GetService(typeof(ITankAClient));
                client.RequestInitialMapInfo();
            }
            // once the load has finished, we use ResetElapsedTime to tell the game's
            // timing mechanism that we have just finished a very long frame, and that
            // it should not try to catch up.
            ScreenManager.Game.ResetElapsedTime();
        }


        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void UnloadContent()
        {
            content.Unload();
            map.UnloadContent();

            INetworkManager net = (INetworkManager)TankAGame.ThisGame.Services.GetService(typeof(INetworkManager));
            if (mode == GameLogicMode.HostCS || mode == GameLogicMode.HostQuake)
                net.StopHost();
            if (mode == GameLogicMode.Client)
            {
                var client = (ITankAClient)TankAGame.ThisGame.Services.GetService(typeof(ITankAClient));
                if (client != null)
                    client.NotReady();
                net.StopClient();
            }
        }


        #endregion

        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
            if (quickLoadTimer > 0)
                quickLoadTimer -= gameTime.ElapsedGameTime.Milliseconds;
            if (quickSaveTimer > 0)
                quickSaveTimer -= gameTime.ElapsedGameTime.Milliseconds;

            if (IsActive)
            {
                map.Update(gameTime);
            }

        }


        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // Look up inputs for the active player profile.
            int playerIndex = (int)ControllingPlayer.Value;

            KeyboardState keyboardState = input.CurrentKeyboardStates[playerIndex];
            GamePadState gamePadState = input.CurrentGamePadStates[playerIndex];

            // The game pauses either if the user presses the pause button, or if
            // they unplug the active gamepad. This requires us to keep track of
            // whether a gamepad was ever plugged in, because we don't want to pause
            // on PC if they are playing with a keyboard and have no gamepad at all!
            bool gamePadDisconnected = !gamePadState.IsConnected &&
                                       input.GamePadWasConnected[playerIndex];

            if (input.IsPauseGame(ControllingPlayer) || gamePadDisconnected)
            {
                ScreenManager.AddScreen(new PauseMenuScreen(), ControllingPlayer);
            }
            else
            {
                TankAConfig config = TankAConfig.Instance;
                if (keyboardState.IsKeyDown(Keys.F5) && quickSaveTimer <= 0)
                {
                    map.QuickSave();
                    quickSaveTimer = 500;
                    return;
                }
                if (keyboardState.IsKeyDown(Keys.F8) && quickLoadTimer <= 0)
                {
                    map.QuickLoad();
                    quickLoadTimer = 500;
                    return;
                }
                if (keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt))
                    config.displayTankHealthBar = true;
                else
                    config.displayTankHealthBar = false;
                if (keyboardState.IsKeyDown(Keys.End))
                    config.displayPeerInfo = true;
                else
                    config.displayPeerInfo = false;

                for (byte i = 0; i < nPlayers; ++i)
                    HandleGamePlayInput(keyboardState, config, i);
            }
        }

        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // This game has a blue background. Why? Because!
            ScreenManager.GraphicsDevice.Clear(ClearOptions.Target,
                                               Color.CornflowerBlue, 0, 0);

            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
            map.Draw(spriteBatch);
            spriteBatch.End();

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0)
                ScreenManager.FadeBackBufferToBlack(255 - TransitionAlpha);
        }
        // Private methods
        void HandleGamePlayInput(KeyboardState keyboardState, TankAConfig config, byte playerIndex)
        {
            // Fire
            if (keyboardState.IsKeyDown(config.controllers[playerIndex].fireKey))
                map.FireUserTank(playerIndex);
            // Movement
            if (keyboardState.IsKeyDown(config.controllers[playerIndex].upKey))
                map.MoveUserTank(playerIndex, Direction.Up);
            else if (keyboardState.IsKeyDown(config.controllers[playerIndex].downKey))
                map.MoveUserTank(playerIndex, Direction.Down);
            else if (keyboardState.IsKeyDown(config.controllers[playerIndex].leftKey))
                map.MoveUserTank(playerIndex, Direction.Left);
            else if (keyboardState.IsKeyDown(config.controllers[playerIndex].rightKey))
                map.MoveUserTank(playerIndex, Direction.Right);
            else
                map.MoveUserTank(playerIndex, Direction.None);
        }
        // Private members
        byte nPlayers;
        GameLogicMode mode;
        string mapName;
        int quickSaveTimer;
        int quickLoadTimer;
    }
}
