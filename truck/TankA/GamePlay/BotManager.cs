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


namespace TankA.GamePlay
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    /// 

    public class BotManager
    {
        public BotManager()
        {
            rnd = new Random();
            ict = (IControlTank)TankAGame.ThisGame.Services.GetService(typeof(IControlTank));
            ResetSpawnTime();

            foreach (Tank t in ict.AllTanksInMap().Values)
            {
                indexOfTank.Add(t.TankIndex);
                timeChange.Add(ResetChangeDirectionTime());
                directionOfBot.Add(ChangeDirection());
                typeOfBot.Add(RandomTypeOfBot());
            }


            // TODO: Construct any child components here
        }

        Direction d = new Direction();
        int botSpawnMinTime = 5000;
        int botSpawnMaxTime = 10000;
        int botChangeDirectionMinTime = 1000;
        int botChangeDirectionMaxTime = 2000;
        int nextSpawnTime = 0;
        int nextChangeDirection = 0;
        int maxBotInMap = 4;
        int botInMap = 0;
        byte type = 0;
        int i;
        public Random rnd { get; private set; }
        IControlTank ict;
        List<int> indexOfTank = new List<int>();
        List<int> timeChange = new List<int>();
        List<Direction> directionOfBot = new List<Direction>();
        List<byte> typeOfBot = new List<byte>();
        List<Vector2> vt = new List<Vector2>();
        List<int> playerToChase = new List<int>();
        List<Tank> bot = new List<Tank>();
        int chasePlayer;



        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        /// 

        //protected override void LoadContent()
        //{
        //    spriteBatch = new SpriteBatch(Game.GraphicsDevice);
        //          
        //    base.LoadContent();
        //}

        private void ResetSpawnTime()
        {
            nextSpawnTime = this.rnd.Next(
                botSpawnMinTime, botSpawnMaxTime);
        }

        private int ResetChangeDirectionTime()
        {
            nextChangeDirection = this.rnd.Next(
                botChangeDirectionMinTime, botChangeDirectionMaxTime);
            return nextChangeDirection;
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        /// 

        private void Spawn()
        {
            //Vector2 speed = Vector2.Zero;
            //Vector2 position = Vector2.Zero;
            TankState ts = new TankState
            {
                tankInfo = new TankInfo()
                {
                    tankType = TankType.Bot1,
                    realSize = new Point(50, 50),
                    gunType = GunType.Gun1,
                    gunDirection = Direction.Down,
                    health = 100,
                    armor = 5,
                    speed = 5,
                    timeBetweenFires = 1000,
                    ammoInfo = new AmmoInfo()
                    {
                        type = AmmoType.Basic,
                        realSize = new Point(0, 0),
                        speed = 10,
                        damage = 30,
                        range = 300
                    }
                },
                playerIndex = 0xff,
                tankIndex = 0xff,
                team = 255,
            };

            ts.position = ict.RandomTankPosition();
            Factory.CreateTank(ts, ict.AllTanksInMap());
            if (RandomTypeOfBot() == 1)
                randomPlayerToChase(playerToChase.Count - 1);
            foreach (Tank t in ict.AllTanksInMap().Values)
            {
                indexOfTank.Add(t.TankIndex);
                timeChange.Add(ResetChangeDirectionTime());
                directionOfBot.Add(ChangeDirection());
                typeOfBot.Add(RandomTypeOfBot());
            }
        }

        private byte RandomTypeOfBot()
        {
            switch (rnd.Next(2))
            {
                case 0:
                    type = 0;
                    break;
                case 1:
                    type = 1;
                    break;
            }
            return type;
        }

        private void Move(Tank t, Direction direction, byte typeBot)
        {
            if (typeBot == 0)
            {
                t.DestinedDirection = direction;
            }
            if (typeBot == 1)
            {
                updateTankInMap();
                if (t.Position.X < vt[chasePlayer].X)
                    t.DestinedDirection = Direction.Right;
                if (t.Position.X > vt[chasePlayer].X)
                    t.DestinedDirection = Direction.Left;
                if (t.Position.Y < vt[chasePlayer].Y)
                    t.DestinedDirection = Direction.Down;
                if (t.Position.Y > vt[chasePlayer].Y)
                    t.DestinedDirection = Direction.Up;
            }
        }

        private void updateTankInMap()
        {
            botInMap = 0;
            foreach (Tank ta in ict.AllTanksInMap().Values)
            {
                UserTank usT = ta as UserTank;
                if (usT != null)
                {
                    vt.Add(ta.Position);
                    playerToChase.Add(usT.PlayerIndex);
                    continue;
                }
                else ++botInMap;
            }
        }

        private void randomPlayerToChase(int numOfPlayer)
        {
            chasePlayer = rnd.Next(numOfPlayer);
        }


        private Direction ChangeDirection()
        {
            switch (rnd.Next(4))
            {
                case 0:
                    d = Direction.Down;
                    break;
                case 1:
                    d = Direction.Left;
                    break;
                case 2:
                    d = Direction.Up;
                    break;
                case 3:
                    d = Direction.Right;
                    break;
            }
            return d;
        }

        public void Update(GameTime gameTime)
        {
            updateTankInMap();
            nextSpawnTime -= gameTime.ElapsedGameTime.Milliseconds;
            if (nextSpawnTime < 0 && botInMap < maxBotInMap)
            {
                Spawn();
                ResetSpawnTime();
            }
            foreach (Tank t in ict.AllTanksInMap().Values)
            {
                UserTank usT = t as UserTank;
                if (usT == null)
                {
                    for (i = indexOfTank.Count - 1; i >= 0; --i)
                    {
                        if (indexOfTank[i] == t.TankIndex)
                        {
                            if (timeChange[i] < 0)
                            {
                                timeChange[i] = ResetChangeDirectionTime();
                                directionOfBot[i] = ChangeDirection();
                            }
                            else
                                timeChange[i] -= gameTime.ElapsedGameTime.Milliseconds;
                            if (typeOfBot[i] == 0)
                                Move(t, directionOfBot[i], 0);
                            else Move(t, directionOfBot[i], 0);
                            break;
                        }
                    }
                    t.Fire();
                }
            }

        }
    }
}