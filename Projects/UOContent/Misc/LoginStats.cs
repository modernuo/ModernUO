using Server.Network;

namespace Server.Misc
{
    public static class LoginStats
    {
        public static void Initialize()
        {
            // Register our event handler
            EventSink.Login += EventSink_Login;
        }

        private static void EventSink_Login(Mobile m)
        {
            var userCount = TcpServer.Instances.Count;
            var itemCount = World.Items.Count;
            var mobileCount = World.Mobiles.Count;

            m.SendMessage(
                $"Welcome, {m.Name}! There {(userCount == 1 ? "is" : "are")} currently {userCount} user{(userCount == 1 ? "" : "s")} " +
                $"online, with {itemCount} item{(itemCount == 1 ? "" : "s")} and {mobileCount} mobile{(mobileCount == 1 ? "" : "s")} in the world."
            );
        }
    }
}
