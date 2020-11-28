using System;
using System.Buffers.Binary;
using Server.Network;

namespace Server.Items
{
    public class LOSBlocker : Item
    {
        private const ushort GMItemId = 0x36FF;

        [Constructible]
        public LOSBlocker() : base(0x21A2) => Movable = false;

        public LOSBlocker(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "no line of sight";

        public static void Initialize()
        {
            TileData.ItemTable[0x21A2].Flags = TileFlag.Wall | TileFlag.NoShoot;
            TileData.ItemTable[0x21A2].Height = 20;
        }

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

            writer.Write(1);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1 && ItemID == 0x2199)
            {
                ItemID = 0x21A2;
            }
        }
    }
}
