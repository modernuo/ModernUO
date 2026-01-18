using System;
using System.Reflection;
using Server.Items;
using Server.Misc;
using Server.Tests.Maps;
using Xunit;

namespace Server.Tests;

[CollectionDefinition("Sequential UOContent Tests", DisableParallelization = true)]
public class UOContentFixture : ICollectionFixture<UOContentFixture>, IDisposable
{
    public UOContentFixture()
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

        // Configure networking (initializes RingSocketManager for tests)
        Server.Network.NetState.Configure();

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

        DecayScheduler.Configure();
    }

    private static int _counter;

    public void Dispose()
    {
        _counter++;

        if (_counter > 1)
        {
            throw new Exception("NO!");
        }
        Timer.Init(0);
    }
}
