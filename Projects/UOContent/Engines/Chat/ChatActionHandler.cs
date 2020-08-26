namespace Server.Engines.Chat
{
    public delegate void OnChatAction(ChatUser from, Channel channel, string param);

    public class ChatActionHandler
    {
        public ChatActionHandler(bool requireModerator, bool requireConference, OnChatAction callback)
        {
            RequireModerator = requireModerator;
            RequireConference = requireConference;
            Callback = callback;
        }

        public bool RequireModerator { get; }

        public bool RequireConference { get; }

        public OnChatAction Callback { get; }
    }
}
