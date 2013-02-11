using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using TankA.GamePlay;
using TankA.Network;

namespace TankA
{
    interface IClientMenu
    {
        void AddServer(IPAddress ip);
    }

    class ClientMenuScreen : MenuScreen, IClientMenu
    {
        public ClientMenuScreen()
            : base("Client")
        {
            MenuEntry refresh = new MenuEntry("Refresh");
            refresh.Selected += RefreshMenuEntrySelected;
            MenuEntries.Add(refresh);

            teamMenuEntry = new MenuEntry("Team: 0");
            teamMenuEntry.Selected += TeamMenuEntrySelected;
            MenuEntries.Add(teamMenuEntry);

            MenuEntry back = new MenuEntry("Back");
            back.Selected += OnCancel;
            MenuEntries.Add(back);

            MenuEntry text = new MenuEntry("Available host(s): ");
            MenuEntries.Add(text);

            var net = (INetworkManager)TankAGame.ThisGame.Services.GetService(typeof(INetworkManager));
            net.StartClient();

            var client = (ITankAClient)TankAGame.ThisGame.Services.GetService(typeof(ITankAClient));
            byte t = client.GetCSTeam();
            if (t == 1)
                teamMenuEntry.Text = "Team: 1";

            TankAGame.ThisGame.Services.AddService(typeof(IClientMenu), this);
        }

        public void AddServer(IPAddress ip)
        {
            foreach (var m in MenuEntries)
                if (m.Text == ip.ToString())
                    return;

            MenuEntry server = new MenuEntry(ip.ToString());
            server.Selected += ServerMenuEntrySelected;
            MenuEntries.Add(server);
        }

        void RefreshMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            var net = (INetworkManager)TankAGame.ThisGame.Services.GetService(typeof(INetworkManager));
            net.Discover();
            for (int i = MenuEntries.Count - 1; i >= 4; --i)
                MenuEntries.RemoveAt(i);
                
        }
        void TeamMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            var client = (ITankAClient)TankAGame.ThisGame.Services.GetService(typeof(ITankAClient));
            client.ChangeCSTeam();
            if (teamMenuEntry.Text == "Team: 0")
            {
                teamMenuEntry.Text = "Team: 1";
            }
            else
            {
                teamMenuEntry.Text = "Team: 0";
            }
        }
        void ServerMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            MenuEntry m = sender as MenuEntry;
            if (m == null)
                return;

            var net = (INetworkManager)TankAGame.ThisGame.Services.GetService(typeof(INetworkManager));
            net.Connect(IPAddress.Parse(m.Text));
        }

        MenuEntry teamMenuEntry;
    }
}
