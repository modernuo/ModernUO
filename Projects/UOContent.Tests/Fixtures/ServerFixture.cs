using System;
using System.Reflection;
using Server.Misc;

namespace Server.Tests;

internal class ServerFixture : IDisposable
{
    // Global setup
    static ServerFixture()
    {
        Core.ApplicationAssembly = Assembly.GetExecutingAssembly();
        Core.LoopContext = new EventLoopContext();
        Core.Expansion = Expansion.EJ;

        // Load Configurations
        ServerConfiguration.Load(true);

        // Load UOContent.dll into the type resolver
        ServerConfiguration.AssemblyDirectories.Add(Core.BaseDirectory);
        AssemblyHandler.LoadAssemblies(["Server.dll", "UOContent.dll"]);

        // Load Skills
        SkillsInfo.Configure();

        // Configure / Initialize
        TestMapDefinitions.ConfigureTestMapDefinitions();

        // Configure the world
        World.Configure();

        Timer.Init(0);

        // Configure Races
        RaceDefinitions.Configure();

        // Load the world
        World.Load();

        World.ExitSerializationThreads();
    }

    public void Dispose()
    {
        Timer.Init(0);
    }
}
