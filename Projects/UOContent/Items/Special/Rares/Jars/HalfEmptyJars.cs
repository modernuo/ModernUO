namespace Server.Items
{
  public class HalfEmptyJar : Item
  {
    [Constructible]
    public HalfEmptyJar()
      : base(0x1007)
    {
      Movable = true;
      Stackable = false;
    }

    public HalfEmptyJar(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class HalfEmptyJars : Item
  {
    [Constructible]
    public HalfEmptyJars()
      : base(0xe4c)
    {
      Movable = true;
      Stackable = false;
    }

    public HalfEmptyJars(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class Jars2 : Item
  {
    [Constructible]
    public Jars2()
      : base(0xE4d)
    {
      Movable = true;
      Stackable = false;
    }

    public Jars2(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class Jars3 : Item
  {
    [Constructible]
    public Jars3()
      : base(0xE4e)
    {
      Movable = true;
      Stackable = false;
    }

    public Jars3(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class Jars4 : Item
  {
    [Constructible]
    public Jars4()
      : base(0xE4f)
    {
      Movable = true;
      Stackable = false;
    }

    public Jars4(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}