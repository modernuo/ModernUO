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

        protected override void SendWorldPacketTo(NetState ns)
        {
            var mob = ns.Mobile;
            if (AccessLevel.GameMaster >= mob?.AccessLevel)
            {
                base.SendWorldPacketTo(ns);
                return;
            }

            // GM Packet
            Span<byte> buffer = stackalloc byte[OutgoingItemPackets.NewWorldItemPacketLength];
            int length;

            if (ns.StygianAbyss)
            {
                OutgoingItemPackets.CreateWorldItemNew(ref buffer, this);
                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(8, 2), GMItemId);
                length = ns.HighSeas ? OutgoingItemPackets.NewWorldItemPacketLength : OutgoingItemPackets.SAWorldItemPacketLength;
            }
            else
            {
                length = OutgoingItemPackets.CreateWorldItem(ref buffer, this);
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
