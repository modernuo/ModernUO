using ModernUO.Serialization;

namespace Server.Multis;

[SerializationGenerator(0, false)]
public partial class MediumBoat : BaseBoat
{
    [Constructible]
    public MediumBoat()
    {
    }

    public override int NorthID => 0x8;
    public override int EastID => 0x9;
    public override int SouthID => 0xA;
    public override int WestID => 0xB;

    public override int HoldDistance => 4;
    public override int TillerManDistance => -5;

    public override Point2D StarboardOffset => new(2, 0);
    public override Point2D PortOffset => new(-2, 0);

    public override Point3D MarkOffset => new(0, 1, 3);

    public override BaseDockedBoat DockedBoat => new MediumDockedBoat(this);
}

[SerializationGenerator(0, false)]
public partial class MediumBoatDeed : BaseBoatDeed
{
    [Constructible]
    public MediumBoatDeed() : base(0x8, Point3D.Zero)
    {
    }

    public override int LabelNumber => 1041207; // medium ship deed
    public override BaseBoat Boat => new MediumBoat();
}

[SerializationGenerator(0, false)]
public partial class MediumDockedBoat : BaseDockedBoat
{
    public MediumDockedBoat(BaseBoat boat) : base(0x8, Point3D.Zero, boat)
    {
    }

    public override BaseBoat Boat => new MediumBoat();
}
