using System;
using System.Buffers;
using System.IO;
using Server.Engines.PartySystem;
using Server.Guilds;

namespace Server.Network
{
    public static class ProtocolExtensions
    {
        private static readonly PacketHandler[] _handlers = new PacketHandler[0x100];

        public static void Configure()
        {
            IncomingPackets.Register( 0xF0, 0, false, DecodeBundledPacket);
            Register(0x00, true, QueryPartyMemberLocations);
            Register(0x01, true, QueryGuildMemberLocations);
        }

        public static void Register(int packetID, bool ingame, OnPacketReceive onReceive)
        {
            _handlers[packetID] = new PacketHandler(packetID, 0, ingame, onReceive);
        }

        public static PacketHandler GetHandler(int packetID) =>
            packetID >= 0 && packetID < _handlers.Length ? _handlers[packetID] : null;

        public static void DecodeBundledPacket(NetState state, CircularBufferReader reader, ref int packetLength)
        {
            int packetID = reader.ReadByte();

            PacketHandler ph = GetHandler(packetID);

            if (ph == null)
            {
                return;
            }

            if (ph.Ingame && state.Mobile == null)
            {
                state.WriteConsole("Sent in-game packet (0xBFx{0:X2}) before having been attached to a mobile", packetID);
                state.Disconnect();
            }
            else if (ph.Ingame && state.Mobile.Deleted)
            {
                state.Disconnect();
            }
            else
            {
                ph.OnReceive(state, reader, ref packetLength);
            }
        }

        public static void QueryGuildMemberLocations(NetState state, CircularBufferReader reader, ref int packetLength)
        {
            Mobile from = state.Mobile;

            state.SendGuildMemberLocations(from, from.Guild as Guild, reader.ReadBoolean());
        }

        public static void QueryPartyMemberLocations(NetState state, CircularBufferReader reader, ref int packetLength)
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
            if (ns == null)
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

                if (sendLocations && Utility.InUpdateRange(from, m) && from.CanSee(m))
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
            if (ns == null)
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

                if (Utility.InUpdateRange(from, mob) && from.CanSee(mob))
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
}
