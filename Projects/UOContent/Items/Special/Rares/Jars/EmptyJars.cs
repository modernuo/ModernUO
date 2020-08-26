namespace Server.Items
{
  public class EmptyJar : Item
  {
    [Constructible]
    public EmptyJar()
      : base(0x1005)
    {
      Movable = true;
      Stackable = false;
    }

    public EmptyJar(Serial serial)
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

  public class EmptyJars : Item
  {
    [Constructible]
    public EmptyJars()
      : base(0xe44)
    {
      Movable = true;
      Stackable = false;
    }

    public EmptyJars(Serial serial)
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

  public class EmptyJars2 : Item
  {
    [Constructible]
    public EmptyJars2()
      : base(0xe45)
    {
      Movable = true;
      Stackable = false;
    }

    public EmptyJars2(Serial serial)
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

  public class EmptyJars3 : Item
  {
    [Constructible]
    public EmptyJars3()
      : base(0xe46)
    {
      Movable = true;
      Stackable = false;
    }

    public EmptyJars3(Serial serial)
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

  public class EmptyJars4 : Item
  {
    [Constructible]
    public EmptyJars4()
      : base(0xe47)
    {
      Movable = true;
      Stackable = false;
    }

    public EmptyJars4(Serial serial)
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