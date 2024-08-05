using System;
using System.Buffers;
using System.Runtime.CompilerServices;
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

public abstract class BaseVirtualHairInfo
{
    protected BaseVirtualHairInfo(int itemid, int hue = 0)
    {
        ItemID = itemid;
        Hue = hue;
        VirtualSerial = World.NewVirtual;
    }

    protected BaseVirtualHairInfo(IGenericReader reader)
    {
        var version = reader.ReadInt();

        switch (version)
        {
            case 0:
                {
                    ItemID = reader.ReadInt();
                    Hue = reader.ReadInt();
                    break;
                }
        }

        VirtualSerial = World.NewVirtual;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ItemID { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Hue { get; set; }

    public virtual void Serialize(IGenericWriter writer)
    {
        writer.Write(0); // version
        writer.Write(ItemID);
        writer.Write(Hue);
    }

    public Serial VirtualSerial { get; private set; } = Serial.Zero;
}

public class VirtualHairInfo : BaseVirtualHairInfo
{
    public VirtualHairInfo(int itemid)
        : base(itemid)
    {
    }

    public VirtualHairInfo(int itemid, int hue)
        : base(itemid, hue)
    {
    }

    public VirtualHairInfo(IGenericReader reader)
        : base(reader)
    {
    }
}

public class VirtualFacialHairInfo : BaseVirtualHairInfo
{
    public VirtualFacialHairInfo(int itemid)
        : base(itemid)
    {
    }

    public VirtualFacialHairInfo(int itemid, int hue)
        : base(itemid, hue)
    {
    }

    public VirtualFacialHairInfo(IGenericReader reader)
        : base(reader)
    {
    }
}
