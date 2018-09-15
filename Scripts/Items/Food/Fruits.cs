namespace Server.Items
{
  public class FruitBasket : Food
  {
    [Constructible]
    public FruitBasket() : base(1, 0x993)
    {
      Weight = 2.0;
      FillFactor = 5;
      Stackable = false;
    }

    public FruitBasket(Serial serial) : base(serial)
    {
    }

    public override bool Eat(Mobile from)
    {
      if (!base.Eat(from))
        return false;

      from.AddToBackpack(new Basket());
      return true;
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

  [Flippable(0x171f, 0x1720)]
  public class Banana : Food
  {
    [Constructible]
    public Banana() : this(1)
    {
    }

    [Constructible]
    public Banana(int amount) : base(amount, 0x171f)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Banana(Serial serial) : base(serial)
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

  [Flippable(0x1721, 0x1722)]
  public class Bananas : Food
  {
    [Constructible]
    public Bananas() : this(1)
    {
    }

    [Constructible]
    public Bananas(int amount) : base(amount, 0x1721)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Bananas(Serial serial) : base(serial)
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

  public class SplitCoconut : Food
  {
    [Constructible]
    public SplitCoconut() : this(1)
    {
    }

    [Constructible]
    public SplitCoconut(int amount) : base(amount, 0x1725)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public SplitCoconut(Serial serial) : base(serial)
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

  public class Lemon : Food
  {
    [Constructible]
    public Lemon() : this(1)
    {
    }

    [Constructible]
    public Lemon(int amount) : base(amount, 0x1728)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Lemon(Serial serial) : base(serial)
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

  public class Lemons : Food
  {
    [Constructible]
    public Lemons() : this(1)
    {
    }

    [Constructible]
    public Lemons(int amount) : base(amount, 0x1729)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Lemons(Serial serial) : base(serial)
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

  public class Lime : Food
  {
    [Constructible]
    public Lime() : this(1)
    {
    }

    [Constructible]
    public Lime(int amount) : base(amount, 0x172a)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Lime(Serial serial) : base(serial)
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

  public class Limes : Food
  {
    [Constructible]
    public Limes() : this(1)
    {
    }

    [Constructible]
    public Limes(int amount) : base(amount, 0x172B)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Limes(Serial serial) : base(serial)
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

  public class Coconut : Food
  {
    [Constructible]
    public Coconut() : this(1)
    {
    }

    [Constructible]
    public Coconut(int amount) : base(amount, 0x1726)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Coconut(Serial serial) : base(serial)
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

  public class OpenCoconut : Food
  {
    [Constructible]
    public OpenCoconut() : this(1)
    {
    }

    [Constructible]
    public OpenCoconut(int amount) : base(amount, 0x1723)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public OpenCoconut(Serial serial) : base(serial)
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

  public class Dates : Food
  {
    [Constructible]
    public Dates() : this(1)
    {
    }

    [Constructible]
    public Dates(int amount) : base(amount, 0x1727)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Dates(Serial serial) : base(serial)
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

  public class Grapes : Food
  {
    [Constructible]
    public Grapes() : this(1)
    {
    }

    [Constructible]
    public Grapes(int amount) : base(amount, 0x9D1)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Grapes(Serial serial) : base(serial)
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

  public class Peach : Food
  {
    [Constructible]
    public Peach() : this(1)
    {
    }

    [Constructible]
    public Peach(int amount) : base(amount, 0x9D2)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Peach(Serial serial) : base(serial)
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

  public class Pear : Food
  {
    [Constructible]
    public Pear() : this(1)
    {
    }

    [Constructible]
    public Pear(int amount) : base(amount, 0x994)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Pear(Serial serial) : base(serial)
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

  public class Apple : Food
  {
    [Constructible]
    public Apple() : this(1)
    {
    }

    [Constructible]
    public Apple(int amount) : base(amount, 0x9D0)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Apple(Serial serial) : base(serial)
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

  public class Watermelon : Food
  {
    [Constructible]
    public Watermelon() : this(1)
    {
    }

    [Constructible]
    public Watermelon(int amount) : base(amount, 0xC5C)
    {
      Weight = 5.0;
      FillFactor = 5;
    }

    public Watermelon(Serial serial) : base(serial)
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
        if (FillFactor == 2)
          FillFactor = 5;

        if (Weight == 2.0)
          Weight = 5.0;
      }
    }
  }

  public class SmallWatermelon : Food
  {
    [Constructible]
    public SmallWatermelon() : this(1)
    {
    }

    [Constructible]
    public SmallWatermelon(int amount) : base(amount, 0xC5D)
    {
      Weight = 5.0;
      FillFactor = 5;
    }

    public SmallWatermelon(Serial serial) : base(serial)
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

  [Flippable(0xc72, 0xc73)]
  public class Squash : Food
  {
    [Constructible]
    public Squash() : this(1)
    {
    }

    [Constructible]
    public Squash(int amount) : base(amount, 0xc72)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Squash(Serial serial) : base(serial)
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

  [Flippable(0xc79, 0xc7a)]
  public class Cantaloupe : Food
  {
    [Constructible]
    public Cantaloupe() : this(1)
    {
    }

    [Constructible]
    public Cantaloupe(int amount) : base(amount, 0xc79)
    {
      Weight = 1.0;
      FillFactor = 1;
    }

    public Cantaloupe(Serial serial) : base(serial)
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