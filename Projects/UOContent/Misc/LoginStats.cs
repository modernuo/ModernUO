using Server.Network;

namespace Server.Misc
{
  public class LoginStats
  {
    public static void Initialize()
    {
      // Register our event handler
      EventSink.Login += EventSink_Login;
    }

    private static void EventSink_Login(Mobile m)
    {
      int userCount = TcpServer.Instances.Count;
      int itemCount = World.Items.Count;
      int mobileCount = World.Mobiles.Count;

      m.SendMessage(
        "Welcome, {0}! There {1} currently {2} user{3} online, with {4} item{5} and {6} mobile{7} in the world.",
        m.Name,
        userCount == 1 ? "is" : "are",
        userCount, userCount == 1 ? "" : "s",
        itemCount, itemCount == 1 ? "" : "s",
        mobileCount, mobileCount == 1 ? "" : "s");
    }
  }
}
