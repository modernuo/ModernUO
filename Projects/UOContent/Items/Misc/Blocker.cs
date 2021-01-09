using System;
using System.Buffers.Binary;
using Server.Network;

namespace Server.Items
{
    public class Blocker : Item
    {
        private const ushort GMItemId = 0x1183;

        [Constructible]
        public Blocker() : base(0x21A4) => Movable = false;

        public Blocker(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 503057; // Impassable!

        public override void SendWorldPacketTo(NetState ns, ReadOnlySpan<byte> world = default)
        {
            var mob = ns.Mobile;
            if (AccessLevel.GameMaster >= mob?.AccessLevel)
            {
                base.SendWorldPacketTo(ns, world);
                return;
            }

            SendGMItem(ns);
        }

        private void SendGMItem(NetState ns)
        {
            // GM Packet
            Span<byte> buffer = stackalloc byte[OutgoingItemPackets.MaxWorldItemPacketLength];

            int length;

            if (ns.StygianAbyss)
            {
                length = OutgoingItemPackets.CreateWorldItemNew(buffer, this, ns.HighSeas);
                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(8, 2), GMItemId);
            }
            else
            {
                length = OutgoingItemPackets.CreateWorldItem(buffer, this);
                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(7, 2), GMItemId);
            }

            ns.Send(buffer.Slice(0, length));
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
