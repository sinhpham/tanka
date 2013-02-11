using System;

namespace TankA
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (TankAGame game = new TankAGame())
            {
                game.Run();
            }
        }
    }
}

