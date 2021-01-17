using System.Collections.Generic;

namespace Server.Engines.Chat
{
    public class ChatUser
    {
        public const char NormalColorCharacter = '0';
        public const char ModeratorColorCharacter = '1';
        public const char VoicedColorCharacter = '2';

        private static readonly List<ChatUser> m_Users = new();
        private static readonly Dictionary<Mobile, ChatUser> m_Table = new();

        public ChatUser(Mobile m, string username)
        {
            Mobile = m;
            Ignored = new List<ChatUser>();
            Ignoring = new List<ChatUser>();
            Username = username;
        }

        public Mobile Mobile { get; }

        public List<ChatUser> Ignored { get; }

        public List<ChatUser> Ignoring { get; }

        public string Username { get; }

        public Channel CurrentChannel { get; set; }

        public bool IsOnline => Mobile.NetState != null;

        public bool Anonymous { get; set; }

        public bool IgnorePrivateMessage { get; set; }

        public bool IsModerator => CurrentChannel?.IsModerator(this) == true;

        public char GetColorCharacter() =>
            IsModerator ? ModeratorColorCharacter :
            CurrentChannel?.IsVoiced(this) == true ? VoicedColorCharacter : NormalColorCharacter;

        public bool CheckOnline()
        {
            if (IsOnline)
            {
                return true;
            }

            RemoveChatUser(this);
            return false;
        }

        public void SendMessage(int number, string param1 = null, string param2 = null) =>
            SendMessage(number, Mobile, param1, param2);

        public void SendMessage(int number, Mobile from, string param1 = null, string param2 = null) =>
            Mobile.NetState.SendChatMessage(from.Language, number, param1, param2);

        public bool IsIgnored(ChatUser check) => Ignored.Contains(check);

        public void AddIgnored(ChatUser user)
        {
            if (IsIgnored(user))
            {
                SendMessage(22, user.Username); // You are already ignoring %1.
            }
            else
            {
                Ignored.Add(user);
                user.Ignoring.Add(this);

                SendMessage(23, user.Username); // You are now ignoring %1.
            }
        }

        public void RemoveIgnored(ChatUser user)
        {
            if (IsIgnored(user))
            {
                Ignored.Remove(user);
                user.Ignoring.Remove(this);

                SendMessage(24, user.Username); // You are no longer ignoring %1.

                if (Ignored.Count == 0)
                {
                    SendMessage(26); // You are no longer ignoring anyone.
                }
            }
            else
            {
                SendMessage(25, user.Username); // You are not ignoring %1.
            }
        }

        public static ChatUser AddChatUser(Mobile from, string username)
        {
            var user = GetChatUser(from);

            if (user != null)
            {
                return user;
            }

            user = new ChatUser(from, username);

            m_Users.Add(user);
            m_Table[from] = user;

            Channel.SendChannelsTo(user);

            var list = Channel.Channels;

            for (var i = 0; i < list.Count; ++i)
            {
                var c = list[i];

                if (c.AddUser(user))
                {
                    break;
                }
            }

            // ChatSystem.SendCommandTo( user.m_Mobile, ChatCommand.AddUserToChannel, user.GetColorCharacter() + user.Username );

            return user;
        }

        public static void RemoveChatUser(ChatUser user)
        {
            if (user == null)
            {
                return;
            }

            for (var i = 0; i < user.Ignoring.Count; ++i)
            {
                user.Ignoring[i].RemoveIgnored(user);
            }

            if (m_Users.Remove(user))
            {
                ChatSystem.SendCommandTo(user.Mobile, ChatCommand.CloseChatWindow);

                user.CurrentChannel?.RemoveUser(user);

                m_Table.Remove(user.Mobile);
            }
        }

        public static ChatUser GetChatUser(Mobile from)
        {
            m_Table.TryGetValue(from, out var c);
            return c;
        }

        public static ChatUser GetChatUser(string username)
        {
            for (var i = 0; i < m_Users.Count; ++i)
            {
                var user = m_Users[i];

                if (user.Username == username)
                {
                    return user;
                }
            }

            return null;
        }

        public static void GlobalSendCommand(ChatCommand command, string param1, string param2 = null)
        {
            GlobalSendCommand(command, null, param1, param2);
        }

        public static void GlobalSendCommand(
            ChatCommand command, ChatUser initiator = null, string param1 = null, string param2 = null
        )
        {
            for (var i = 0; i < m_Users.Count; ++i)
            {
                var user = m_Users[i];

                if (user == initiator)
                {
                    continue;
                }

                if (user.CheckOnline())
                {
                    ChatSystem.SendCommandTo(user.Mobile, command, param1, param2);
                }
            }
        }
    }
}
