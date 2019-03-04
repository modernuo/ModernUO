namespace Server.Items
{
  [Flippable(0xc77, 0xc78)]
  public class Carrot : Food
  {
    [Constructible]
    public Carrot(int amount = 1) : base(amount, 0xc78)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Carrot(Serial serial) : base(serial)
    {
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  [Flippable(0xc7b, 0xc7c)]
  public class Cabbage : Food
  {
    [Constructible]
    public Cabbage(int amount = 1) : base(amount, 0xc7b)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Cabbage(Serial serial) : base(serial)
    {
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  [Flippable(0xc6d, 0xc6e)]
  public class Onion : Food
  {
    [Constructible]
    public Onion(int amount = 1) : base(amount, 0xc6d)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Onion(Serial serial) : base(serial)
    {
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  [Flippable(0xc70, 0xc71)]
  public class Lettuce : Food
  {
    [Constructible]
    public Lettuce(int amount = 1) : base(amount, 0xc70)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Lettuce(Serial serial) : base(serial)
    {
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  [Flippable(0xC6A, 0xC6B)]
  public class Pumpkin : Food
  {
    [Constructible]
    public Pumpkin(int amount = 1) : base(amount, 0xC6A)
    {
      Weight = 1.0;
      FillFactor = 8;
    }

    public Pumpkin(Serial serial) : base(serial)
    {
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      if (version < 1)
      {
        if (FillFactor == 4)
          FillFactor = 8;

        if (Weight == 5.0)
          Weight = 1.0;
      }
    }
  }

  public class SmallPumpkin : Food
  {
    [Constructible]
    public SmallPumpkin(int amount = 1) : base(amount, 0xC6C)
    {
      Weight = 1.0;
      FillFactor = 8;
    }

    public SmallPumpkin(Serial serial) : base(serial)
    {
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
