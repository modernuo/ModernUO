/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Packets.Party.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network
{
    public static partial class Packets
    {
        // TODO: Move out of the core
        public static void PartyMessage(this NetState state, CircularBufferReader reader)
        {
            if (state.Mobile == null)
            {
                return;
            }

            switch (reader.ReadByte())
            {
                case 0x01:
                    state.PartyMessage_AddMember(reader);
                    break;
                case 0x02:
                    state.PartyMessage_RemoveMember(reader);
                    break;
                case 0x03:
                    state.PartyMessage_PrivateMessage(reader);
                    break;
                case 0x04:
                    state.PartyMessage_PublicMessage(reader);
                    break;
                case 0x06:
                    state.PartyMessage_SetCanLoot(reader);
                    break;
                case 0x08:
                    state.PartyMessage_Accept(reader);
                    break;
                case 0x09:
                    state.PartyMessage_Decline(reader);
                    break;
                default:
                    reader.Trace(state);
                    break;
            }
        }

        public static void PartyMessage_AddMember(this NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnAdd(state.Mobile);
        }

        public static void PartyMessage_RemoveMember(this NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnRemove(state.Mobile, World.FindMobile(reader.ReadUInt32()));
        }

        public static void PartyMessage_PrivateMessage(this NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnPrivateMessage(
                state.Mobile,
                World.FindMobile(reader.ReadUInt32()),
                reader.ReadBigUniSafe()
            );
        }

        public static void PartyMessage_PublicMessage(this NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnPublicMessage(state.Mobile, reader.ReadBigUniSafe());
        }

        public static void PartyMessage_SetCanLoot(this NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnSetCanLoot(state.Mobile, reader.ReadBoolean());
        }

        public static void PartyMessage_Accept(this NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnAccept(state.Mobile, World.FindMobile(reader.ReadUInt32()));
        }

        public static void PartyMessage_Decline(this NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnDecline(state.Mobile, World.FindMobile(reader.ReadUInt32()));
        }
    }
}
