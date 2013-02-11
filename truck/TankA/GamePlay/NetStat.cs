using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TankA.GamePlay
{
    interface INetStat
    {
        void SetMode(GameLogicMode mode);

        void ChangeStat(IPAddress ip, short value, bool isKill);
        void ChangeTeamStat(Point teamStat);
        void AckKill(IPAddress killer, IPAddress killed);
        void AckLose(byte loseTeam);

        Dictionary<IPAddress, PeerStat> Stat();
        Point TeamScore();
        short GetKill(IPAddress client);
        short GetDeath(IPAddress client);

        void ChangeClientName(IPAddress ip, string name);
        void NewClient(IPAddress ip, string name, byte team);
        void ClientLeft(IPAddress ip);
    }

    class PeerStat
    {
        public PeerStat()
        {
            name = "No name";
        }
        public PeerStat(string name, byte team)
        {
            this.name = name;
            this.team = team;
        }
        public string name;
        public byte team;
        public short kill;
        public short death;
    }

    class NetStat : INetStat
    {
        // Constructor.
        public NetStat()
        {
            mode = GameLogicMode.Undefined;
        }
        // Draw.
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Factory.StatHeader(), new Vector2(230, 82), null, Color.White,
                0, Vector2.Zero, 1, SpriteEffects.None, Map.Layer2);
            int y = 200;
            if (mode == GameLogicMode.HostQuake)
            {
                var rows = from s in stats.Values
                           orderby s.kill descending
                           select new { Name = s.name, Kill = s.kill, Death = s.death };

                foreach (var r in rows)
                {
                    string rowText = r.Name.PadRight(15)
                        + r.Kill.ToString().PadLeft(5)
                        + r.Death.ToString().PadLeft(5);

                    spriteBatch.Draw(Factory.StatBlue(), new Vector2(230, y), null, Color.White,
                        0, Vector2.Zero, 1, SpriteEffects.None, Map.Layer2);
                    spriteBatch.DrawString(Factory.HealthFont(), rowText,
                        new Vector2(300, y), Color.White);

                    y += rowSize;
                }
            }
            if (mode == GameLogicMode.HostCS)
            {
                var rows = from s in stats.Values
                           orderby s.kill descending
                           group s by s.team into g
                           select new { Team = g.Key, Rows = g };

                foreach (var team in rows)
                {
                    string teamString = ("       " + team.Team.ToString()).PadRight(20);

                    if (team.Team == 0)
                        teamString += teamScore.X.ToString().PadLeft(5);
                    if (team.Team == 1)
                        teamString += teamScore.Y.ToString().PadLeft(5);

                    spriteBatch.Draw(Factory.StatTeam(), new Vector2(230, y), null, Color.White,
                        0, Vector2.Zero, 1, SpriteEffects.None, Map.Layer2 - 0.001f);
                    spriteBatch.DrawString(Factory.HealthFont(), teamString,
                        new Vector2(300, y), Color.White);
                    y += rowSize;
                    foreach (var r in team.Rows)
                    {
                        string rowText = r.name.PadRight(15)
                            + r.kill.ToString().PadLeft(5)
                            + r.death.ToString().PadLeft(5);
                        spriteBatch.Draw(Factory.StatBlue(), new Vector2(230, y), null, Color.White,
                        0, Vector2.Zero, 1, SpriteEffects.None, Map.Layer2);
                        spriteBatch.DrawString(Factory.HealthFont(), rowText,
                            new Vector2(300, y), Color.White);
                        y += rowSize;
                    }
                }
            }
            spriteBatch.Draw(Factory.StatFooter(), new Vector2(230, y), null, Color.White,
                0, Vector2.Zero, 1, SpriteEffects.None, Map.Layer2);
        }

        public void SetMode(GameLogicMode mode)
        {
            this.mode = mode;
        }

        public void ChangeStat(IPAddress ip, short value, bool isKill)
        {
            if (!stats.ContainsKey(ip))
            {
                stats.Add(ip, new PeerStat());
                stats[ip].name = ip.ToString();
            }
            if (isKill)
                stats[ip].kill = value;
            else
                stats[ip].death = value;
        }
        public void ChangeTeamStat(Point teamStat)
        {
            teamScore = teamStat;
        }
        public void AckKill(IPAddress killer, IPAddress killed)
        {
            if (stats.ContainsKey(killer))
                stats[killer].kill++;

            if (stats.ContainsKey(killed))
                stats[killed].death++;
        }
        public void AckLose(byte loseTeam)
        {
            switch (loseTeam)
            {
                case 0:
                    teamScore.Y++;
                    break;
                case 1:
                    teamScore.X++;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public Dictionary<IPAddress, PeerStat> Stat()
        {
            return stats;
        }
        public Point TeamScore()
        {
            return teamScore;
        }
        public short GetKill(IPAddress client)
        {
            if (stats.ContainsKey(client))
                return stats[client].kill;
            return -1;
        }
        public short GetDeath(IPAddress client)
        {
            if (stats.ContainsKey(client))
                return stats[client].death;
            return -1;
        }
        public void ChangeClientName(IPAddress ip, string name)
        {
            if (stats.ContainsKey(ip))
                stats[ip].name = name;
        }
        public void NewClient(IPAddress ip, string name, byte team)
        {
            if (name == "")
                name = ip.ToString();
            stats.Add(ip, new PeerStat(name, team));
        }
        public void ClientLeft(IPAddress ip)
        {
            stats.Remove(ip);
        }
        // Private members.
        Dictionary<IPAddress, PeerStat> stats = new Dictionary<IPAddress, PeerStat>();
        Point teamScore;

        GameLogicMode mode;

        const int rowSize = 50;
    }
}
