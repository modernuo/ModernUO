/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MapUO.cs                                                        *
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
using System.IO;
using Server.Engines.PartySystem;
using Server.Guilds;

namespace Server.Network;

public static class MapUO
{
    public static unsafe void Configure()
    {
        AssistantProtocol.Register(0x00, true, &QueryPartyMemberLocations);
        AssistantProtocol.Register(0x01, true, &QueryGuildMemberLocations);
    }

    public static void QueryGuildMemberLocations(NetState state, CircularBufferReader reader, int packetLength)
    {
        Mobile from = state.Mobile;

        state.SendGuildMemberLocations(from, from.Guild as Guild, reader.ReadBoolean());
    }

    public static void QueryPartyMemberLocations(NetState state, CircularBufferReader reader, int packetLength)
    {
        Mobile from = state.Mobile;
        var party = Party.Get(from);

        if (party != null)
        {
            state.SendPartyMemberLocations(from, party);
        }
    }

    public static void SendGuildMemberLocations(this NetState ns, Mobile from, Guild guild, bool sendLocations)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var count = guild?.Members.Count ?? 0;
        var maxLength = 9 + (count > 1 ? (count - 1) * (sendLocations ? 10 : 4) : 0);
        var writer = new SpanWriter(stackalloc byte[maxLength]);
        writer.Write((byte)0xF0); // Packet ID
        writer.Seek(2, SeekOrigin.Current);
        writer.Write((byte)0x02); // Command
        writer.Write(count > 0 && sendLocations);

        bool sendPacket = false;
        for (var i = 0; i < count; i++)
        {
            var m = guild!.Members[i];

            if (m?.NetState == null || m == from)
            {
                continue;
            }

            if (sendLocations && Utility.InUpdateRange(from.Location, m.Location) && from.CanSee(m))
            {
                continue;
            }

            sendPacket = true;
            writer.Write(m.Serial);

            if (sendLocations)
            {
                writer.Write((short)m.X);
                writer.Write((short)m.Y);
                writer.Write((byte)(m.Map?.MapID ?? 0));

                if (m.Alive)
                {
                    writer.Write((byte)(m.Hits * 100 / Math.Max(m.HitsMax, 1)));
                }
                else
                {
                    writer.Write((byte)0);
                }
            }
        }

        if (!sendPacket)
        {
            return;
        }

        writer.Write(0);
        writer.WritePacketLength();
        ns.Send(writer.Span);
    }

    public static void SendPartyMemberLocations(this NetState ns, Mobile from, Party party)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var count = party?.Members.Count ?? 0;
        var maxLength = 9 + (count > 1 ? (count - 1) * 9 : 0);
        var writer = new SpanWriter(stackalloc byte[maxLength]);
        writer.Write((byte)0xF0); // Packet ID
        writer.Seek(2, SeekOrigin.Current);
        writer.Write((byte)0x01); // Command

        bool sendPacket = false;
        for (var i = 0; i < count; i++)
        {
            var pmi = party!.Members[i];
            Mobile mob = pmi?.Mobile;

            if (mob?.NetState == null || mob == from)
            {
                continue;
            }

            if (Utility.InUpdateRange(from.Location, mob.Location) && from.CanSee(mob))
            {
                continue;
            }

            sendPacket = true;
            writer.Write(mob.Serial);
            writer.Write((short)mob.X);
            writer.Write((short)mob.Y);
            writer.Write((byte)(mob.Map?.MapID ?? 0));
        }

        if (!sendPacket)
        {
            return;
        }

        writer.Write(0);
        writer.WritePacketLength();
        ns.Send(writer.Span);
    }
}
