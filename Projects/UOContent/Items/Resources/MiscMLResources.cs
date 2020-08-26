namespace Server.Items
{
  public class Blight : Item
  {
    [Constructible]
    public Blight(int amount = 1)
      : base(0x3183)
    {
      Stackable = true;
      Amount = amount;
    }

    public Blight(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class LuminescentFungi : Item
  {
    [Constructible]
    public LuminescentFungi(int amount = 1)
      : base(0x3191)
    {
      Stackable = true;
      Amount = amount;
    }

    public LuminescentFungi(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class CapturedEssence : Item
  {
    [Constructible]
    public CapturedEssence(int amount = 1)
      : base(0x318E)
    {
      Stackable = true;
      Amount = amount;
    }

    public CapturedEssence(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class EyeOfTheTravesty : Item
  {
    [Constructible]
    public EyeOfTheTravesty(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public EyeOfTheTravesty(int amount = 1)
      : base(0x318D)
    {
      Stackable = true;
      Amount = amount;
    }

    public EyeOfTheTravesty(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class Corruption : Item
  {
    [Constructible]
    public Corruption(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public Corruption(int amount = 1)
      : base(0x3184)
    {
      Stackable = true;
      Amount = amount;
    }

    public Corruption(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class DreadHornMane : Item
  {
    [Constructible]
    public DreadHornMane(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public DreadHornMane(int amount = 1)
      : base(0x318A)
    {
      Stackable = true;
      Amount = amount;
    }

    public DreadHornMane(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class ParasiticPlant : Item
  {
    [Constructible]
    public ParasiticPlant(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public ParasiticPlant(int amount = 1)
      : base(0x3190)
    {
      Stackable = true;
      Amount = amount;
    }

    public ParasiticPlant(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class Muculent : Item
  {
    [Constructible]
    public Muculent(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public Muculent(int amount = 1)
      : base(0x3188)
    {
      Stackable = true;
      Amount = amount;
    }

    public Muculent(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class DiseasedBark : Item
  {
    [Constructible]
    public DiseasedBark(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public DiseasedBark(int amount = 1)
      : base(0x318B)
    {
      Stackable = true;
      Amount = amount;
    }

    public DiseasedBark(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class BarkFragment : Item
  {
    [Constructible]
    public BarkFragment(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public BarkFragment(int amount = 1)
      : base(0x318F)
    {
      Stackable = true;
      Amount = amount;
    }

    public BarkFragment(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class GrizzledBones : Item
  {
    [Constructible]
    public GrizzledBones(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public GrizzledBones(int amount = 1)
      : base(0x318C)
    {
      Stackable = true;
      Amount = amount;
    }

    public GrizzledBones(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      if (version <= 0 && ItemID == 0x318F)
        ItemID = 0x318C;
    }
  }

  public class LardOfParoxysmus : Item
  {
    [Constructible]
    public LardOfParoxysmus(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public LardOfParoxysmus(int amount = 1)
      : base(0x3189)
    {
      Stackable = true;
      Amount = amount;
    }

    public LardOfParoxysmus(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class PerfectEmerald : Item
  {
    [Constructible]
    public PerfectEmerald(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public PerfectEmerald(int amount = 1)
      : base(0x3194)
    {
      Stackable = true;
      Amount = amount;
    }

    public PerfectEmerald(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class DarkSapphire : Item
  {
    [Constructible]
    public DarkSapphire(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public DarkSapphire(int amount = 1)
      : base(0x3192)
    {
      Stackable = true;
      Amount = amount;
    }

    public DarkSapphire(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class Turquoise : Item
  {
    [Constructible]
    public Turquoise(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public Turquoise(int amount = 1)
      : base(0x3193)
    {
      Stackable = true;
      Amount = amount;
    }

    public Turquoise(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class EcruCitrine : Item
  {
    [Constructible]
    public EcruCitrine(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public EcruCitrine(int amount = 1)
      : base(0x3195)
    {
      Stackable = true;
      Amount = amount;
    }

    public EcruCitrine(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class WhitePearl : Item
  {
    [Constructible]
    public WhitePearl(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public WhitePearl(int amount = 1)
      : base(0x3196)
    {
      Stackable = true;
      Amount = amount;
    }

    public WhitePearl(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class FireRuby : Item
  {
    [Constructible]
    public FireRuby(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public FireRuby(int amount = 1)
      : base(0x3197)
    {
      Stackable = true;
      Amount = amount;
    }

    public FireRuby(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class BlueDiamond : Item
  {
    [Constructible]
    public BlueDiamond(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public BlueDiamond(int amount = 1)
      : base(0x3198)
    {
      Stackable = true;
      Amount = amount;
    }

    public BlueDiamond(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class BrilliantAmber : Item
  {
    [Constructible]
    public BrilliantAmber(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public BrilliantAmber(int amount = 1)
      : base(0x3199)
    {
      Stackable = true;
      Amount = amount;
    }

    public BrilliantAmber(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class Scourge : Item
  {
    [Constructible]
    public Scourge(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public Scourge(int amount = 1)
      : base(0x3185)
    {
      Stackable = true;
      Amount = amount;
      Hue = 150;
    }

    public Scourge(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class Putrefication : Item
  {
    [Constructible]
    public Putrefication(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public Putrefication(int amount = 1)
      : base(0x3186)
    {
      Stackable = true;
      Amount = amount;
      Hue = 883;
    }

    public Putrefication(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class Taint : Item
  {
    [Constructible]
    public Taint(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public Taint(int amount = 1)
      : base(0x3187)
    {
      Stackable = true;
      Amount = amount;
      Hue = 731;
    }

    public Taint(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  [Flippable(0x315A, 0x315B)]
  public class PristineDreadHorn : Item
  {
    [Constructible]
    public PristineDreadHorn()
      : base(0x315A)
    {
    }

    public PristineDreadHorn(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class SwitchItem : Item
  {
    [Constructible]
    public SwitchItem(int amountFrom, int amountTo)
      : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public SwitchItem(int amount = 1)
      : base(0x2F5F)
    {
      Stackable = true;
      Amount = amount;
    }

    public SwitchItem(Serial serial)
      : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
