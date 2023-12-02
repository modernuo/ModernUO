using ModernUO.Serialization;

namespace Server.Multis;

[SerializationGenerator(0, false)]
public partial class LargeDragonBoat : BaseBoat
{
    [Constructible]
    public LargeDragonBoat()
    {
    }

    public override int NorthID => 0x14;
    public override int EastID => 0x15;
    public override int SouthID => 0x16;
    public override int WestID => 0x17;

    public override int HoldDistance => 5;
    public override int TillerManDistance => -5;

    public override Point2D StarboardOffset => new(2, -1);
    public override Point2D PortOffset => new(-2, -1);

    public override Point3D MarkOffset => new(0, 0, 3);

    public override BaseDockedBoat DockedBoat => new LargeDockedDragonBoat(this);
}

[SerializationGenerator(0, false)]
public partial class LargeDragonBoatDeed : BaseBoatDeed
{
    [Constructible]
    public LargeDragonBoatDeed() : base(0x14, new Point3D(0, -1, 0))
    {
    }

    public override int LabelNumber => 1041210; // large dragon ship deed
    public override BaseBoat Boat => new LargeDragonBoat();
}

[SerializationGenerator(0, false)]
public partial class LargeDockedDragonBoat : BaseDockedBoat
{
    public LargeDockedDragonBoat(BaseBoat boat) : base(0x14, new Point3D(0, -1, 0), boat)
    {
    }

    public override BaseBoat Boat => new LargeDragonBoat();
}
