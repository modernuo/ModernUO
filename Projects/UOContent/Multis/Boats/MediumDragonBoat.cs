using ModernUO.Serialization;

namespace Server.Multis;

[SerializationGenerator(0, false)]
public partial class MediumDragonBoat : BaseBoat
{
    [Constructible]
    public MediumDragonBoat()
    {
    }

    public override int NorthID => 0xC;
    public override int EastID => 0xD;
    public override int SouthID => 0xE;
    public override int WestID => 0xF;

    public override int HoldDistance => 4;
    public override int TillerManDistance => -5;

    public override Point2D StarboardOffset => new(2, 0);
    public override Point2D PortOffset => new(-2, 0);

    public override Point3D MarkOffset => new(0, 1, 3);

    public override BaseDockedBoat DockedBoat => new MediumDockedDragonBoat(this);
}

[SerializationGenerator(0, false)]
public partial class MediumDragonBoatDeed : BaseBoatDeed
{
    [Constructible]
    public MediumDragonBoatDeed() : base(0xC, Point3D.Zero)
    {
    }

    public override int LabelNumber => 1041208; // medium dragon ship deed
    public override BaseBoat Boat => new MediumDragonBoat();
}

[SerializationGenerator(0, false)]
public partial class MediumDockedDragonBoat : BaseDockedBoat
{
    public MediumDockedDragonBoat(BaseBoat boat) : base(0xC, Point3D.Zero, boat)
    {
    }

    public override BaseBoat Boat => new MediumDragonBoat();
}
