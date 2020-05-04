using System;
using System.Collections.Generic;
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
      // Load Configurations
      ServerConfiguration.Load(true);

      // Configure / Initialize
      MapDefinitions.Configure();

      World.Load();

      fromMobile = new Mobile(Serial.NewMobile);
      fromMobile.DefaultMobileInit();

      toMobile = new Mobile(Serial.NewMobile);
      toMobile.DefaultMobileInit();

      fromCont = new Container(Serial.NewItem);
      toCont = new Container(Serial.NewItem);
      itemInFromCont = new Item(Serial.NewItem) { Parent = fromCont };
    }

    public void Dispose()
    {
    }
  }
}
