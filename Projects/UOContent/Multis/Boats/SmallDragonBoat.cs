using ModernUO.Serialization;

namespace Server.Multis;

[SerializationGenerator(0, false)]
public partial class SmallDragonBoat : BaseBoat
{
    [Constructible]
    public SmallDragonBoat()
    {
    }

    public override int NorthID => 0x4;
    public override int EastID => 0x5;
    public override int SouthID => 0x6;
    public override int WestID => 0x7;

    public override int HoldDistance => 4;
    public override int TillerManDistance => -4;

    public override Point2D StarboardOffset => new(2, 0);
    public override Point2D PortOffset => new(-2, 0);

    public override Point3D MarkOffset => new(0, 1, 3);

    public override BaseDockedBoat DockedBoat => new SmallDockedDragonBoat(this);
}

[SerializationGenerator(0, false)]
public partial class SmallDragonBoatDeed : BaseBoatDeed
{
    [Constructible]
    public SmallDragonBoatDeed() : base(0x4, Point3D.Zero)
    {
    }

    public override int LabelNumber => 1041206; // small dragon ship deed
    public override BaseBoat Boat => new SmallDragonBoat();
}

[SerializationGenerator(0, false)]
public partial class SmallDockedDragonBoat : BaseDockedBoat
{
    public SmallDockedDragonBoat(BaseBoat boat) : base(0x4, Point3D.Zero, boat)
    {
    }

    public override BaseBoat Boat => new SmallDragonBoat();
}
