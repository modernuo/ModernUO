using System;
using Server.Network;

namespace Server.Engines.Chat
{
    public static class ChatSystem
    {
        public static bool Enabled { get; set; }

        public static void Configure()
        {
            Enabled = ServerConfiguration.GetOrUpdateSetting("chat.enabled", false);
        }

        public static void Initialize()
        {
            IncomingPackets.Register(0xB5, 0x40, true, OpenChatWindowRequest);
            IncomingPackets.Register(0xB3, 0, true, ChatAction);
        }

        public static void SendCommandTo(Mobile to, ChatCommand type, string param1 = null, string param2 = null)
        {
            to?.Send(new ChatMessagePacket(null, (int)type + 20, param1, param2));
        }

        public static void OpenChatWindowRequest(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (!Enabled)
            {
                from.SendMessage("The chat system has been disabled.");
                return;
            }

            // Newer clients don't send chat username anymore so we are ignoring the rest of this packet.

            // TODO: How does OSI handle incognito/disguise kits?
            // TODO: Does OSI still allow duplicate names?
            // For now we assume they should use their raw name.
            var chatName = from.RawName ?? $"Unknown User {Utility.RandomMinMax(1000000, 9999999)}";

            SendCommandTo(from, ChatCommand.OpenChatWindow, chatName);
            ChatUser.AddChatUser(from, chatName);
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

        public static void ChatAction(NetState state, CircularBufferReader reader)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                var from = state.Mobile;
                var user = ChatUser.GetChatUser(from);

                if (user == null)
                {
                    return;
                }

                var lang = reader.ReadAsciiSafe(4);
                int actionID = reader.ReadInt16();
                var param = reader.ReadBigUniSafe();

                var handler = ChatActionHandlers.GetHandler(actionID);

                if (handler == null)
                {
                    state.WriteConsole("Unknown chat action 0x{0:X}: {1}", actionID, param);
                    return;
                }

                var channel = user.CurrentChannel;

                if (handler.RequireConference && channel == null)
                {
                    // You must be in a conference to do this.
                    // To join a conference, select one from the Conference menu.
                    user.SendMessage(31);
                    return;
                }

                if (handler.RequireModerator && !user.IsModerator)
                {
                    user.SendMessage(29); // You must have operator status to do this.
                    return;
                }

                handler.Callback(user, channel, param);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
