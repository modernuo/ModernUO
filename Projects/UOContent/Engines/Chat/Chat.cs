using System;
using System.IO;
using Server.Accounting;
using Server.Misc;
using Server.Network;

namespace Server.Engines.Chat
{
    public class ChatSystem
    {
        public static bool Enabled { get; set; } = true;

        public static void Initialize()
        {
            PacketHandlers.Register(0xB5, 0x40, true, OpenChatWindowRequest);
            PacketHandlers.Register(0xB3, 0, true, ChatAction);
        }

        public static void SendCommandTo(Mobile to, ChatCommand type, string param1 = null, string param2 = null)
        {
            to?.Send(new ChatMessagePacket(null, (int)type + 20, param1, param2));
        }

        public static void OpenChatWindowRequest(NetState state, PacketReader pvSrc)
        {
            var from = state.Mobile;

            if (!Enabled)
            {
                from.SendMessage("The chat system has been disabled.");
                return;
            }

            pvSrc.Seek(2, SeekOrigin.Begin);
            var chatName = pvSrc.ReadUnicodeStringSafe((0x40 - 2) >> 1).Trim();

            var acct = state.Account as Account;

            string accountChatName = null;

            if (acct != null)
            {
                accountChatName = acct.GetTag("ChatName");
            }

            accountChatName = accountChatName?.Trim();

            if (!string.IsNullOrEmpty(accountChatName))
            {
                if (chatName.Length > 0 && chatName != accountChatName)
                {
                    from.SendMessage("You cannot change chat nickname once it has been set.");
                }
            }
            else
            {
                if (chatName.Length == 0)
                {
                    SendCommandTo(from, ChatCommand.AskNewNickname);
                    return;
                }

                if (NameVerification.Validate(chatName, 2, 31, true, true, true, 0, NameVerification.SpaceDashPeriodQuote) &&
                    chatName.ToLower().IndexOf("system") == -1)
                {
                    // TODO: Optimize this search

                    foreach (Account checkAccount in Accounts.GetAccounts())
                    {
                        var existingName = checkAccount.GetTag("ChatName");

                        if (existingName != null)
                        {
                            existingName = existingName.Trim();

                            if (Insensitive.Equals(existingName, chatName))
                            {
                                from.SendMessage("Nickname already in use.");
                                SendCommandTo(from, ChatCommand.AskNewNickname);
                                return;
                            }
                        }
                    }

                    accountChatName = chatName;

                    acct?.AddTag("ChatName", chatName);
                }
                else
                {
                    from.SendLocalizedMessage(501173); // That name is disallowed.
                    SendCommandTo(from, ChatCommand.AskNewNickname);
                    return;
                }
            }

            SendCommandTo(from, ChatCommand.OpenChatWindow, accountChatName);
            ChatUser.AddChatUser(from);
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

        public static void ChatAction(NetState state, PacketReader pvSrc)
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

                var lang = pvSrc.ReadStringSafe(4);
                int actionID = pvSrc.ReadInt16();
                var param = pvSrc.ReadUnicodeString();

                var handler = ChatActionHandlers.GetHandler(actionID);

                if (handler != null)
                {
                    var channel = user.CurrentChannel;

                    if (handler.RequireConference && channel == null)
                        /* You must be in a conference to do this.
                         * To join a conference, select one from the Conference menu.
                         */
                    {
                        user.SendMessage(31);
                    }
                    else if (handler.RequireModerator && !user.IsModerator)
                    {
                        user.SendMessage(29); // You must have operator status to do this.
                    }
                    else
                    {
                        handler.Callback(user, channel, param);
                    }
                }
                else
                {
                    Console.WriteLine("Client: {0}: Unknown chat action 0x{1:X}: {2}", state, actionID, param);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
