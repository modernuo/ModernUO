using System;
using Server.Misc;

namespace Server.Tests
{
    internal class ServerFixture : IDisposable
    {
        // Global setup
        static ServerFixture()
        {
            Core.LoopContext = new EventLoopContext();

            Core.Expansion = Expansion.EJ;

            // Load Configurations
            ServerConfiguration.Load(true);

            // Configure / Initialize
            TestMapDefinitions.ConfigureTestMapDefinitions();

            // Configure the world
            World.Configure();

            Timer.Init(0);

            // Configure Races
            RaceDefinitions.Configure();

            // Load the world
            World.Load();
        }

        public void Dispose()
        {
            Timer.Init(0);
        }
    }
}
