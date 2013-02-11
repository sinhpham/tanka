using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using System.Xml.Serialization;
using System.IO;

namespace TankA
{
    public struct PlayerControl
    {
        public Keys upKey, downKey, rightKey, leftKey, fireKey;
    }
    sealed public class TankAConfig
    {
        private static readonly TankAConfig instance = new TankAConfig();
        // Constructors
        static TankAConfig()
        {
        }

        private TankAConfig()
        {
            // Construct default configuration
            PlayerControl p1Keys = new PlayerControl
            {
                upKey = Keys.Up,
                downKey = Keys.Down,
                leftKey = Keys.Left,
                rightKey = Keys.Right,
                fireKey = Keys.Space
            };
            PlayerControl p2Keys = new PlayerControl
            {
                upKey = Keys.W,
                downKey = Keys.S,
                leftKey = Keys.A,
                rightKey = Keys.D,
                fireKey = Keys.B
            };
            controllers[0] = p1Keys;
            controllers[1] = p2Keys;
            
            playerName = "";
        }
        // Public property to use
        public static TankAConfig Instance
        {
            get { return instance; }
        }
        // Public methods
        public bool LoadFromConfigFile()
        {
            if (File.Exists(configFileName))
            {
                TextReader textReader = new StreamReader(configFileName);
                XmlSerializer serializer = new XmlSerializer(typeof(PlayerControl[]));
                controllers = (PlayerControl[])serializer.Deserialize(textReader);
                return true;
            }            
            return false;
        }
        public bool SaveToConfigFile()
        {
            TextWriter textWriter = new StreamWriter(configFileName);
            XmlSerializer serializer = new XmlSerializer(typeof(PlayerControl[]));
            serializer.Serialize(textWriter, controllers);
            return true;
        }
        // Public data members
        
        public PlayerControl[] controllers = new PlayerControl[2];
        public bool displayTankHealthBar;
        public bool displayPeerInfo;
        public string playerName;
        public byte userTeam;
        // Private data members
        string configFileName = "config.xml";
    }
}