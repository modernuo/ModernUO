namespace Server.Engines.Chat
{
    public static class ChatSystem
    {
        public static bool Enabled { get; set; }

        public static void Configure()
        {
            Enabled = ServerConfiguration.GetOrUpdateSetting("chat.enabled", false);
        }

        public static void SendCommandTo(Mobile to, ChatCommand type, string param1 = null, string param2 = null)
        {
            to?.NetState.SendChatMessage(null, (int)type + 20, param1, param2);
        }

        public static ChatUser SearchForUser(ChatUser from, string name)
        {
            var user = ChatUser.GetChatUser(name);

            if (user == null)
            {
                from.SendMessage(32, name); // There is no player named '%1'.
            }

            return user;
        }
    }
}
