namespace Server.Multis
{
  public class LargeDragonBoat : BaseBoat
  {
    [Constructible]
    public LargeDragonBoat()
    {
    }

    public LargeDragonBoat(Serial serial) : base(serial)
    {
    }

    public override int NorthID => 0x14;
    public override int EastID => 0x15;
    public override int SouthID => 0x16;
    public override int WestID => 0x17;

    public override int HoldDistance => 5;
    public override int TillerManDistance => -5;

    public override Point2D StarboardOffset => new Point2D(2, -1);
    public override Point2D PortOffset => new Point2D(-2, -1);

    public override Point3D MarkOffset => new Point3D(0, 0, 3);

    public override BaseDockedBoat DockedBoat => new LargeDockedDragonBoat(this);

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }
  }

  public class LargeDragonBoatDeed : BaseBoatDeed
  {
    [Constructible]
    public LargeDragonBoatDeed() : base(0x14, new Point3D(0, -1, 0))
    {
    }

    public LargeDragonBoatDeed(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1041210; // large dragon ship deed
    public override BaseBoat Boat => new LargeDragonBoat();

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }
  }

  public class LargeDockedDragonBoat : BaseDockedBoat
  {
    public LargeDockedDragonBoat(BaseBoat boat) : base(0x14, new Point3D(0, -1, 0), boat)
    {
    }

    public LargeDockedDragonBoat(Serial serial) : base(serial)
    {
    }

    public override BaseBoat Boat => new LargeDragonBoat();

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }
  }
}