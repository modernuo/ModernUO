using ModernUO.CodeGeneratedEvents;

namespace Server.Network;

public static partial class GameServer
{
    public class GameLoginEventArgs
    {
        public GameLoginEventArgs(NetState state, string un, string pw, bool preAuthenticated)
        {
            State = state;
            Username = un;
            Password = pw;
            PreAuthenticated = preAuthenticated;
        }

        /// <summary>
        /// The auth id presented on this game login was issued to this account, from this address,
        /// after the account login packet verified the password. Read-only so a subscriber cannot
        /// grant itself the skip.
        /// </summary>
        public bool PreAuthenticated { get; }

        public NetState State { get; }

        public string Username { get; }

        public string Password { get; }

        public bool Accepted { get; set; }

        public CityInfo[] CityInfo { get; set; }
    }

    [GeneratedEvent(nameof(GameServerLoginEvent))]
    public static partial void GameServerLoginEvent(GameLoginEventArgs e);
}
