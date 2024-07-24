using Server.Network;

namespace Server.Misc
{
    public static class LoginStats
    {
        public static void OnLogin(Mobile m)
        {
            var userCount = NetState.Instances.Count;
            var itemCount = World.Items.Count;
            var mobileCount = World.Mobiles.Count;

            m.SendMessage(
                $"Welcome, {m.Name}! There {(userCount == 1 ? "is" : "are")} currently {userCount} user{(userCount == 1 ? "" : "s")} " +
                $"online, with {itemCount} item{(itemCount == 1 ? "" : "s")} and {mobileCount} mobile{(mobileCount == 1 ? "" : "s")} in the world."
            );
        }
    }
}
