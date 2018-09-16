namespace Server.Items
{
  public abstract class Beard : Item
  {
    /*public static Beard CreateByID( int id, int hue )
    {
      switch ( id )
      {
        case 0x203E: return new LongBeard( hue );
        case 0x203F: return new ShortBeard( hue );
        case 0x2040: return new Goatee( hue );
        case 0x2041: return new Mustache( hue );
        case 0x204B: return new MediumShortBeard( hue );
        case 0x204C: return new MediumLongBeard( hue );
        case 0x204D: return new Vandyke( hue );
        default: return new GenericBeard( id, hue );
      }
    }*/

    protected Beard(int itemID, int hue = 0) : base(itemID)
    {
      LootType = LootType.Blessed;
      Layer = Layer.FacialHair;
      Hue = hue;
    }

    public Beard(Serial serial) : base(serial)
    {
    }

    public override bool DisplayLootType => false;

    public override bool VerifyMove(Mobile from)
    {
      return from.AccessLevel >= AccessLevel.GameMaster;
    }

    public override DeathMoveResult OnParentDeath(Mobile parent)
    {
      //Dupe( Amount );

      parent.FacialHairItemID = ItemID;
      parent.FacialHairHue = Hue;

      return DeathMoveResult.MoveToCorpse;
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);
      LootType = LootType.Blessed;

      int version = reader.ReadInt();
    }
  }

  public class GenericBeard : Beard
  {
    private GenericBeard(int itemID, int hue = 0) : base(itemID, hue)
    {
    }

    public GenericBeard(Serial serial) : base(serial)
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

  public class LongBeard : Beard
  {
    private LongBeard(int hue = 0)
      : base(0x203E, hue)
    {
    }

    public LongBeard(Serial serial) : base(serial)
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

  public class ShortBeard : Beard
  {
    private ShortBeard(int hue = 0)
      : base(0x203f, hue)
    {
    }

    public ShortBeard(Serial serial) : base(serial)
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

  public class Goatee : Beard
  {
    private Goatee(int hue = 0)
      : base(0x2040, hue)
    {
    }

    public Goatee(Serial serial) : base(serial)
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

  public class Mustache : Beard
  {
    private Mustache(int hue = 0)
      : base(0x2041, hue)
    {
    }

    public Mustache(Serial serial) : base(serial)
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

  public class MediumShortBeard : Beard
  {
    private MediumShortBeard(int hue = 0)
      : base(0x204B, hue)
    {
    }

    public MediumShortBeard(Serial serial) : base(serial)
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

  public class MediumLongBeard : Beard
  {
    private MediumLongBeard(int hue = 0)
      : base(0x204C, hue)
    {
    }

    public MediumLongBeard(Serial serial) : base(serial)
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

  public class Vandyke : Beard
  {
    private Vandyke(int hue = 0)
      : base(0x204D, hue)
    {
    }

    public Vandyke(Serial serial) : base(serial)
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