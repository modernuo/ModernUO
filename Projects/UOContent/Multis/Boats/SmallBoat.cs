using ModernUO.Serialization;

namespace Server.Multis;

[SerializationGenerator(0, false)]
public partial class SmallBoat : BaseBoat
{
    [Constructible]
    public SmallBoat()
    {
    }

    public override int NorthID => 0x0;
    public override int EastID => 0x1;
    public override int SouthID => 0x2;
    public override int WestID => 0x3;

    public override int HoldDistance => 4;
    public override int TillerManDistance => -4;

    public override Point2D StarboardOffset => new(2, 0);
    public override Point2D PortOffset => new(-2, 0);

    public override Point3D MarkOffset => new(0, 1, 3);

    public override BaseDockedBoat DockedBoat => new SmallDockedBoat(this);
}

[SerializationGenerator(0, false)]
public partial class SmallBoatDeed : BaseBoatDeed
{
    [Constructible]
    public SmallBoatDeed() : base(0x0, Point3D.Zero)
    {
    }

    public override int LabelNumber => 1041205; // small ship deed
    public override BaseBoat Boat => new SmallBoat();
}

[SerializationGenerator(0, false)]
public partial class SmallDockedBoat : BaseDockedBoat
{
    public SmallDockedBoat(BaseBoat boat) : base(0x0, Point3D.Zero, boat)
    {
    }

    public override BaseBoat Boat => new SmallBoat();
}
