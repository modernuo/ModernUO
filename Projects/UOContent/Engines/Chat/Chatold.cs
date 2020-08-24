namespace Server.Chat
{
    public static class ChatSystem
    {
        public static void Initialize()
        {
            EventSink.ChatRequest += EventSink_ChatRequest;
        }

        private static void EventSink_ChatRequest(Mobile m)
        {
            m.SendMessage("Chat is not currently supported.");
        }
    }
}
