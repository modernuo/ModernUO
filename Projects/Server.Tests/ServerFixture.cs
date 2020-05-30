using System;
// using Server.Misc;

namespace Server.Tests
{
  public class ServerFixture : IDisposable
  {

    // Global setup
    static ServerFixture()
    {
      // Load Configurations
      ServerConfiguration.Load(true);

      // Configure / Initialize
      TestMapDefinitions.ConfigureTestMapDefinitions();

      // Load the world
      World.Load();
    }

    public void Dispose()
    {
    }
  }
}
