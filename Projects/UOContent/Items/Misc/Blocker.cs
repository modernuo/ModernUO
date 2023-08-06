using System;
using System.Buffers.Binary;
using ModernUO.Serialization;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Blocker : Item
{
    private const ushort GMItemId = 0x1183;

    [Constructible]
    public Blocker() : base(0x21A4) => Movable = false;

    public override int LabelNumber => 503057; // Impassable!

    public override void SendWorldPacketTo(NetState ns, ReadOnlySpan<byte> world = default)
    {
        var mob = ns.Mobile;
        if (mob?.AccessLevel >= AccessLevel.GameMaster)
        {
            SendGMItem(ns);
        }
        else
        {
            base.SendWorldPacketTo(ns, world);
        }
    }

    private void SendGMItem(NetState ns)
    {
        // GM Packet
        Span<byte> buffer = stackalloc byte[OutgoingEntityPackets.MaxWorldEntityPacketLength].InitializePacket();

        int length;

        if (ns.StygianAbyss)
        {
            length = OutgoingEntityPackets.CreateWorldEntity(buffer, this, ns.HighSeas);
            BinaryPrimitives.WriteUInt16BigEndian(buffer[8..10], GMItemId);
        }
        else
        {
            length = OutgoingItemPackets.CreateWorldItem(buffer, this);
            BinaryPrimitives.WriteUInt16BigEndian(buffer[7..9], GMItemId);
        }

        ns.Send(buffer[..length]);
    }
}
