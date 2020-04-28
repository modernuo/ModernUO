using System;
using Server.Misc;

namespace Server.Tests
{
  public class ServerFixture : IDisposable
  {
    public Mobile mobile { get; }

    public ServerFixture()
    {
      // Configure / Initialize
      MapDefinitions.Configure();

      World.Load();

      mobile = new Mobile(0x1);
    }

    public void Dispose()
    {
    }
  }
}
