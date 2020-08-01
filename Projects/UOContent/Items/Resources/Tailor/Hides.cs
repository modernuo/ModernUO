namespace Server.Items
{
  public abstract class BaseHides : Item, ICommodity
  {
    private CraftResource m_Resource;

    public BaseHides(CraftResource resource, int amount = 1) : base(0x1079)
    {
      Stackable = true;
      Weight = 5.0;
      Amount = amount;
      Hue = CraftResources.GetHue(resource);

      m_Resource = resource;
    }

    public BaseHides(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public CraftResource Resource
    {
      get => m_Resource;
      set
      {
        m_Resource = value;
        InvalidateProperties();
      }
    }

    public override int LabelNumber
    {
      get
      {
        if (m_Resource >= CraftResource.SpinedLeather && m_Resource <= CraftResource.BarbedLeather)
          return 1049687 + (m_Resource - CraftResource.SpinedLeather);

        return 1047023;
      }
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1); // version

      writer.Write((int)m_Resource);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 1:
          {
            m_Resource = (CraftResource)reader.ReadInt();
            break;
          }
        case 0:
          {
            OreInfo info = new OreInfo(reader.ReadInt(), reader.ReadInt(), reader.ReadString());

            m_Resource = CraftResources.GetFromOreInfo(info);
            break;
          }
      }
    }

    public override void AddNameProperty(ObjectPropertyList list)
    {
      if (Amount > 1)
        list.Add(1050039, "{0}\t#{1}", Amount, 1024216); // ~1_NUMBER~ ~2_ITEMNAME~
      else
        list.Add(1024216); // pile of hides
    }

    public override void GetProperties(ObjectPropertyList list)
    {
      base.GetProperties(list);

      if (!CraftResources.IsStandard(m_Resource))
      {
        int num = CraftResources.GetLocalizationNumber(m_Resource);

        if (num > 0)
          list.Add(num);
        else
          list.Add(CraftResources.GetName(m_Resource));
      }
    }
  }

  [Flippable(0x1079, 0x1078)]
  public class Hides : BaseHides, IScissorable
  {
    [Constructible]
    public Hides(int amount = 1) : base(CraftResource.RegularLeather, amount)
    {
    }

    public Hides(Serial serial) : base(serial)
    {
    }

    public bool Scissor(Mobile from, Scissors scissors)
    {
      if (Deleted || !from.CanSee(this)) return false;

      if (Core.AOS && !IsChildOf(from.Backpack))
      {
        from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack
        return false;
      }

      ScissorHelper(from, new Leather(), 1);

      return true;
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

  [Flippable(0x1079, 0x1078)]
  public class SpinedHides : BaseHides, IScissorable
  {
    [Constructible]
    public SpinedHides(int amount = 1) : base(CraftResource.SpinedLeather, amount)
    {
    }

    public SpinedHides(Serial serial) : base(serial)
    {
    }

    public bool Scissor(Mobile from, Scissors scissors)
    {
      if (Deleted || !from.CanSee(this)) return false;

      if (Core.AOS && !IsChildOf(from.Backpack))
      {
        from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack
        return false;
      }

      ScissorHelper(from, new SpinedLeather(), 1);

      return true;
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

  [Flippable(0x1079, 0x1078)]
  public class HornedHides : BaseHides, IScissorable
  {
    [Constructible]
    public HornedHides(int amount = 1) : base(CraftResource.HornedLeather, amount)
    {
    }

    public HornedHides(Serial serial) : base(serial)
    {
    }

    public bool Scissor(Mobile from, Scissors scissors)
    {
      if (Deleted || !from.CanSee(this)) return false;

      if (Core.AOS && !IsChildOf(from.Backpack))
      {
        from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack
        return false;
      }

      ScissorHelper(from, new HornedLeather(), 1);

      return true;
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

  [Flippable(0x1079, 0x1078)]
  public class BarbedHides : BaseHides, IScissorable
  {
    [Constructible]
    public BarbedHides(int amount = 1) : base(CraftResource.BarbedLeather, amount)
    {
    }

    public BarbedHides(Serial serial) : base(serial)
    {
    }

    public bool Scissor(Mobile from, Scissors scissors)
    {
      if (Deleted || !from.CanSee(this)) return false;

      if (Core.AOS && !IsChildOf(from.Backpack))
      {
        from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack
        return false;
      }

      ScissorHelper(from, new BarbedLeather(), 1);

      return true;
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
