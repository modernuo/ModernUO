namespace Server.Items
{
  public class ArcaneCircleScroll : SpellScroll
  {
    [Constructible]
    public ArcaneCircleScroll(int amount = 1)
      : base(600, 0x2D51, amount) =>
      Hue = 0x8FD;

    public ArcaneCircleScroll(Serial serial)
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

  public class GiftOfRenewalScroll : SpellScroll
  {
    [Constructible]
    public GiftOfRenewalScroll(int amount = 1)
      : base(601, 0x2D52, amount) =>
      Hue = 0x8FD;

    public GiftOfRenewalScroll(Serial serial)
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

  public class ImmolatingWeaponScroll : SpellScroll
  {
    [Constructible]
    public ImmolatingWeaponScroll(int amount = 1)
      : base(602, 0x2D53, amount) =>
      Hue = 0x8FD;

    public ImmolatingWeaponScroll(Serial serial)
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

  public class AttuneWeaponScroll : SpellScroll
  {
    [Constructible]
    public AttuneWeaponScroll(int amount = 1)
      : base(603, 0x2D54, amount) =>
      Hue = 0x8FD;

    public AttuneWeaponScroll(Serial serial)
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

  public class ThunderstormScroll : SpellScroll
  {
    [Constructible]
    public ThunderstormScroll(int amount = 1)
      : base(604, 0x2D55, amount) =>
      Hue = 0x8FD;

    public ThunderstormScroll(Serial serial)
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

  public class NatureFuryScroll : SpellScroll
  {
    [Constructible]
    public NatureFuryScroll(int amount = 1)
      : base(605, 0x2D56, amount) =>
      Hue = 0x8FD;

    public NatureFuryScroll(Serial serial)
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

  public class SummonFeyScroll : SpellScroll
  {
    [Constructible]
    public SummonFeyScroll(int amount = 1)
      : base(606, 0x2D57, amount) =>
      Hue = 0x8FD;

    public SummonFeyScroll(Serial serial)
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

  public class SummonFiendScroll : SpellScroll
  {
    [Constructible]
    public SummonFiendScroll(int amount = 1)
      : base(607, 0x2D58, amount) =>
      Hue = 0x8FD;

    public SummonFiendScroll(Serial serial)
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

  public class ReaperFormScroll : SpellScroll
  {
    [Constructible]
    public ReaperFormScroll(int amount = 1)
      : base(608, 0x2D59, amount) =>
      Hue = 0x8FD;

    public ReaperFormScroll(Serial serial)
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

  public class WildfireScroll : SpellScroll
  {
    [Constructible]
    public WildfireScroll(int amount = 1)
      : base(609, 0x2D5A, amount) =>
      Hue = 0x8FD;

    public WildfireScroll(Serial serial)
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

  public class EssenceOfWindScroll : SpellScroll
  {
    [Constructible]
    public EssenceOfWindScroll(int amount = 1)
      : base(610, 0x2D5B, amount) =>
      Hue = 0x8FD;

    public EssenceOfWindScroll(Serial serial)
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

  public class DryadAllureScroll : SpellScroll
  {
    [Constructible]
    public DryadAllureScroll(int amount = 1)
      : base(611, 0x2D5C, amount) =>
      Hue = 0x8FD;

    public DryadAllureScroll(Serial serial)
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

  public class EtherealVoyageScroll : SpellScroll
  {
    [Constructible]
    public EtherealVoyageScroll(int amount = 1)
      : base(612, 0x2D5D, amount) =>
      Hue = 0x8FD;

    public EtherealVoyageScroll(Serial serial)
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

  public class WordOfDeathScroll : SpellScroll
  {
    [Constructible]
    public WordOfDeathScroll(int amount = 1)
      : base(613, 0x2D5E, amount) =>
      Hue = 0x8FD;

    public WordOfDeathScroll(Serial serial)
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

  public class GiftOfLifeScroll : SpellScroll
  {
    [Constructible]
    public GiftOfLifeScroll(int amount = 1)
      : base(614, 0x2D5F, amount) =>
      Hue = 0x8FD;

    public GiftOfLifeScroll(Serial serial)
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

  public class ArcaneEmpowermentScroll : SpellScroll
  {
    [Constructible]
    public ArcaneEmpowermentScroll(int amount = 1)
      : base(615, 0x2D60, amount) =>
      Hue = 0x8FD;

    public ArcaneEmpowermentScroll(Serial serial)
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
