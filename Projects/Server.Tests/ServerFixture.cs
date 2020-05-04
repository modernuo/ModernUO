using System;
using Server.Items;
using Server.Misc;

namespace Server.Tests
{
  public class ServerFixture : IDisposable
  {
    public Mobile fromMobile { get; }
    public Mobile toMobile { get; }
    public Container fromCont { get; }
    public Container toCont { get; }
    public Item itemInFromCont { get; }

    public ServerFixture()
    {
      // Configure / Initialize
      MapDefinitions.Configure();

      World.Load();

      fromMobile = new Mobile(Serial.NewMobile);
      toMobile = new Mobile(Serial.NewMobile);
      fromCont = new Container(Serial.NewItem);
      toCont = new Container(Serial.NewItem);
      itemInFromCont = new Item(Serial.NewItem) { Parent = fromCont };
    }

    public void Dispose()
    {
    }
  }
}
