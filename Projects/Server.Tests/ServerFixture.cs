using System;
using Server.Items;
using Server.Misc;

namespace Server.Tests
{
  public class ServerFixture : IDisposable
  {
    private static Mobile m_FromMobile;
    private static Mobile m_ToMobile;
    private static Container m_FromCont;
    private static Container m_ToCont;
    private static Item m_ItemInFromCont;

    // Global setup
    static ServerFixture()
    {
      // Load Configurations
      ServerConfiguration.Load(true);

      // Configure / Initialize
      MapDefinitions.Configure();

      // Load the world
      World.Load();

      m_FromMobile = new Mobile(Serial.NewMobile);
      m_FromMobile.DefaultMobileInit();

      m_ToMobile = new Mobile(Serial.NewMobile);
      m_ToMobile.DefaultMobileInit();

      m_FromCont = new Container(Serial.NewItem);
      m_ToCont = new Container(Serial.NewItem);
      m_ItemInFromCont = new Item(Serial.NewItem) { Parent = m_FromCont };
    }

    public Mobile fromMobile => m_FromMobile;
    public Mobile toMobile => m_ToMobile;
    public Container fromCont => m_FromCont;
    public Container toCont => m_ToCont;
    public Item itemInFromCont => m_ItemInFromCont;

    public void Dispose()
    {
    }
  }
}
