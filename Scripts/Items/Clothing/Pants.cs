namespace Server.Items
{
  public abstract class BasePants : BaseClothing
  {
    public BasePants(int itemID) : this(itemID, 0)
    {
    }

    public BasePants(int itemID, int hue) : base(itemID, Layer.Pants, hue)
    {
    }

    public BasePants(Serial serial) : base(serial)
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

  [Flippable(0x152e, 0x152f)]
  public class ShortPants : BasePants
  {
    [Constructible]
    public ShortPants() : this(0)
    {
    }

    [Constructible]
    public ShortPants(int hue) : base(0x152E, hue)
    {
      Weight = 2.0;
    }

    public ShortPants(Serial serial) : base(serial)
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

  [Flippable(0x1539, 0x153a)]
  public class LongPants : BasePants
  {
    [Constructible]
    public LongPants() : this(0)
    {
    }

    [Constructible]
    public LongPants(int hue) : base(0x1539, hue)
    {
      Weight = 2.0;
    }

    public LongPants(Serial serial) : base(serial)
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

  [Flippable(0x279B, 0x27E6)]
  public class TattsukeHakama : BasePants
  {
    [Constructible]
    public TattsukeHakama() : this(0)
    {
    }

    [Constructible]
    public TattsukeHakama(int hue) : base(0x279B, hue)
    {
      Weight = 2.0;
    }

    public TattsukeHakama(Serial serial) : base(serial)
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

  [Flippable(0x2FC3, 0x3179)]
  public class ElvenPants : BasePants
  {
    [Constructible]
    public ElvenPants() : this(0)
    {
    }

    [Constructible]
    public ElvenPants(int hue) : base(0x2FC3, hue)
    {
      Weight = 2.0;
    }

    public ElvenPants(Serial serial) : base(serial)
    {
    }

    public override Race RequiredRace => Race.Elf;

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}