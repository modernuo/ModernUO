using System;
using Xunit;

namespace Server.Tests;

[CollectionDefinition("Sequential Server Tests", DisableParallelization = true)]
public class ServerFixture : ICollectionFixture<ServerFixture>, IDisposable
{
    public ServerFixture()
    {
        TestServerInitializer.Initialize(loadTileData: false);
    }

    public void Dispose()
    {
        Timer.Init(0);
    }
}
