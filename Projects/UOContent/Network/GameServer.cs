using ModernUO.CodeGeneratedEvents;

namespace Server.Network;

public static partial class GameServer
{
    public class GameLoginEventArgs
    {
        public GameLoginEventArgs(NetState state, string un, string pw)
        {
            State = state;
            Username = un;
            Password = pw;
        }

        public NetState State { get; }

        public string Username { get; }

        public string Password { get; }

        public bool Accepted { get; set; }

        public CityInfo[] CityInfo { get; set; }
    }

    [GeneratedEvent(nameof(GameServerLoginEvent))]
    public static partial void GameServerLoginEvent(GameLoginEventArgs e);
}
