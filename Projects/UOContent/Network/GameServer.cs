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

        /// <summary>
        /// When true, the login will be completed asynchronously (e.g., by a gateway handler).
        /// The packet handler skips both the accept and reject paths.
        /// The handler that sets this is responsible for calling SendCharacterList or Disconnect.
        /// </summary>
        public bool Deferred { get; set; }

        public CityInfo[] CityInfo { get; set; }
    }

    [GeneratedEvent(nameof(GameServerLoginEvent))]
    public static partial void GameServerLoginEvent(GameLoginEventArgs e);
}
