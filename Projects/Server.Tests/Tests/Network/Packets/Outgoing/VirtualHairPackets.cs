namespace Server.Network;

public sealed class HairEquipUpdate : Packet
{
    public HairEquipUpdate(Mobile parent)
        : base(0x2E, 15)
    {
        var hue = parent.SolidHueOverride >= 0 ? parent.SolidHueOverride : parent.HairHue;

        Stream.Write(parent.Hair.VirtualSerial);
        Stream.Write((short)parent.HairItemID);
        Stream.Write((byte)0);
        Stream.Write((byte)Layer.Hair);
        Stream.Write(parent.Serial);
        Stream.Write((short)hue);
    }
}

public sealed class FacialHairEquipUpdate : Packet
{
    public FacialHairEquipUpdate(Mobile parent)
        : base(0x2E, 15)
    {
        var hue = parent.SolidHueOverride >= 0 ? parent.SolidHueOverride : parent.FacialHairHue;

        Stream.Write(parent.FacialHair.VirtualSerial);
        Stream.Write((short)parent.FacialHairItemID);
        Stream.Write((byte)0);
        Stream.Write((byte)Layer.FacialHair);
        Stream.Write(parent.Serial);
        Stream.Write((short)hue);
    }
}

public sealed class RemoveHair : Packet
{
    public RemoveHair(Mobile parent)
        : base(0x1D, 5)
    {
        Stream.Write(parent.Hair.VirtualSerial);
    }
}

public sealed class RemoveFacialHair : Packet
{
    public RemoveFacialHair(Mobile parent)
        : base(0x1D, 5)
    {
        Stream.Write(parent.FacialHair.VirtualSerial);
    }
}
