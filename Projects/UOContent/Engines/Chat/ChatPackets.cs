/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: ChatPackets.cs                                                  *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Buffers;
using Server.Network;

namespace Server.Engines.Chat
{
    public static class ChatPackets
    {
        public static unsafe void Configure()
        {
            IncomingPackets.Register(0xB5, 0x40, true, &OpenChatWindowRequest);
            IncomingPackets.Register(0xB3, 0, true, &ChatAction);
        }

        public static void OpenChatWindowRequest(NetState state, CircularBufferReader reader, int packetLength)
        {
            var from = state.Mobile;

            if (!ChatSystem.Enabled)
            {
                from.SendMessage("The chat system has been disabled.");
                return;
            }

            // Newer clients don't send chat username anymore so we are ignoring the rest of this packet.

            // TODO: How does OSI handle incognito/disguise kits?
            // TODO: Does OSI still allow duplicate names?
            // For now we assume they should use their raw name.
            var chatName = from.RawName ?? $"Unknown User {Utility.RandomMinMax(1000000, 9999999)}";

            ChatSystem.SendCommandTo(from, ChatCommand.OpenChatWindow, chatName);
            ChatUser.AddChatUser(from, chatName);
        }

        public static void ChatAction(NetState state, CircularBufferReader reader, int packetLength)
        {
            if (!ChatSystem.Enabled)
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
                    state.LogInfo($"Unknown chat action 0x{actionID:X}: {param}");
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

        public static void SendChatMessage(this NetState ns, string lang, int number, string param1, string param2)
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            param1 ??= "";
            param2 ??= "";

            var length = 13 + (param1.Length + param2.Length) * 2;
            var writer = new SpanWriter(stackalloc byte[length]);
            writer.Write((byte)0xB2); // Packet ID
            writer.Write((ushort)length);
            writer.Write((ushort)(number - 20)); // Command

            writer.WriteAscii(lang ?? "", 4);
            writer.WriteBigUniNull(param1);
            writer.WriteBigUniNull(param2);

            ns.Send(writer.Span);
        }
    }
}
