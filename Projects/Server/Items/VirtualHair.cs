using System;
using System.Buffers;
using ModernUO.Serialization;
using Server.Network;

namespace Server;

public static class OutgoingVirtualHairPackets
{
    public const int EquipUpdatePacketLength = 15;
    public const int RemovePacketLength = 5;

    public static void SendHairEquipUpdatePacket(this NetState ns, Mobile m, uint hairSerial,  int itemId, int hue, Layer layer)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> buffer = stackalloc byte[EquipUpdatePacketLength].InitializePacket();
        CreateHairEquipUpdatePacket(buffer, m, hairSerial, itemId, hue, layer);
        ns.Send(buffer);
    }

    public static void CreateHairEquipUpdatePacket(Span<byte> buffer, Mobile m, uint hairSerial, int itemId, int hue, Layer layer)
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0x2E); // Packet ID

        writer.Write(hairSerial);
        writer.Write((short)itemId);
        writer.Write((byte)0);
        writer.Write((byte)layer);
        writer.Write(m.Serial);
        writer.Write((short)(m.SolidHueOverride >= 0 ? m.SolidHueOverride : hue));
    }

    public static void SendRemoveHairPacket(this NetState ns, uint hairSerial)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> buffer = stackalloc byte[RemovePacketLength].InitializePacket();
        CreateRemoveHairPacket(buffer, hairSerial);
        ns.Send(buffer);
    }

    public static void CreateRemoveHairPacket(Span<byte> buffer, uint hairSerial)
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0x1D); // Packet ID
        writer.Write(hairSerial);
    }
}

[SerializationGenerator(0, false)]
public partial class VirtualHairInfo
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _itemId;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _hue;

    public VirtualHairInfo() : this(0)
    {
    }

    public VirtualHairInfo(int itemid, int hue = 0)
    {
        _itemId = itemid;
        _hue = hue;
        VirtualSerial = World.NewVirtual;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        VirtualSerial = World.NewVirtual;
    }

    public Serial VirtualSerial { get; private set; }
}
