using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using TankA.GamePlay;
using TankA.Network;

namespace TankA
{
    class HostMenuScreen : MenuScreen
    {
        MenuEntry hostModeChooser;
        GameLogicMode currentMode = GameLogicMode.HostQuake;

        public HostMenuScreen()
            : base("Host new game")
        {
            hostModeChooser = new MenuEntry("Host mode: Quake");
            hostModeChooser.Selected += HostModeChooserSelected;
            MenuEntries.Add(hostModeChooser);

            string[] mapFiles = Directory.GetFiles(@"./", "*.map");
            foreach (var name in mapFiles)
            {
                // Create menu entries.
                var menuEntry = new MenuEntry("Map: " + name.Substring(2));
                // Event handlers.
                menuEntry.Selected += MapSelected;
                // Add entries to menu.
                MenuEntries.Add(menuEntry);
            }

            MenuEntry backMenuEntry = new MenuEntry("Back");
            backMenuEntry.Selected += OnCancel;
            MenuEntries.Add(backMenuEntry);
        }

        void MapSelected(object sender, PlayerIndexEventArgs e)
        {
            MenuEntry m = sender as MenuEntry;
            if (m == null)
                return;

            LoadingScreen.Load(ScreenManager, true, e.PlayerIndex,
                new GameplayScreen(currentMode, m.Text.Substring(5)));
        }
        void HostModeChooserSelected(object sender, PlayerIndexEventArgs e)
        {
            if (currentMode == GameLogicMode.HostQuake)
            {
                currentMode = GameLogicMode.HostCS;
                hostModeChooser.Text = "Host mode: Counter-strike";
            }
            else
            {
                currentMode = GameLogicMode.HostQuake;
                hostModeChooser.Text = "Host mode: Quake";
            }
        }
    }
}
