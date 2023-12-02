using ModernUO.Serialization;

namespace Server.Multis;

[SerializationGenerator(0, false)]
public partial class LargeBoat : BaseBoat
{
    [Constructible]
    public LargeBoat()
    {
    }

    public override int NorthID => 0x10;
    public override int EastID => 0x11;
    public override int SouthID => 0x12;
    public override int WestID => 0x13;

    public override int HoldDistance => 5;
    public override int TillerManDistance => -5;

    public override Point2D StarboardOffset => new(2, -1);
    public override Point2D PortOffset => new(-2, -1);

    public override Point3D MarkOffset => new(0, 0, 3);

    public override BaseDockedBoat DockedBoat => new LargeDockedBoat(this);
}

[SerializationGenerator(0, false)]
public partial class LargeBoatDeed : BaseBoatDeed
{
    [Constructible]
    public LargeBoatDeed() : base(0x10, new Point3D(0, -1, 0))
    {
    }

    public override int LabelNumber => 1041209; // large ship deed
    public override BaseBoat Boat => new LargeBoat();
}

[SerializationGenerator(0, false)]
public partial class LargeDockedBoat : BaseDockedBoat
{
    public LargeDockedBoat(BaseBoat boat) : base(0x10, new Point3D(0, -1, 0), boat)
    {
    }

    public override BaseBoat Boat => new LargeBoat();
}
