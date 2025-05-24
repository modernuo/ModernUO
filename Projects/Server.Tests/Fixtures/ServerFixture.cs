using System;
using System.Reflection;
using Xunit;

namespace Server.Tests;

[CollectionDefinition("Sequential Server Tests", DisableParallelization = true)]
public class ServerFixture : ICollectionFixture<ServerFixture>, IDisposable
{
    public ServerFixture()
    {
        Core.ApplicationAssembly = Assembly.GetExecutingAssembly(); // Server.Tests.dll

        // Load Configurations
        ServerConfiguration.Load(true);

        // Load an empty assembly list into the resolver
        ServerConfiguration.AssemblyDirectories.Add(Core.BaseDirectory);
        AssemblyHandler.LoadAssemblies(["Server.dll"]);

        Core.LoopContext = new EventLoopContext();
        Core.Expansion = Expansion.EJ;

        // Configure / Initialize
        TestMapDefinitions.ConfigureTestMapDefinitions();

        // Configure the world
        World.Configure();

        Timer.Init(0);

        // Load the world
        World.Load();

        World.ExitSerializationThreads();
    }

    public void Dispose()
    {
        Timer.Init(0);
    }
}
