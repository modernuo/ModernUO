using Server.Targeting;

namespace Server.Engines.Plants
{
  public class Seed : Item
  {
    private PlantHue m_PlantHue;
    private PlantType m_PlantType;
    private bool m_ShowType;

    [Constructible]
    public Seed() : this(PlantTypeInfo.RandomFirstGeneration(), PlantHueInfo.RandomFirstGeneration())
    {
    }

    [Constructible]
    public Seed(PlantType plantType, PlantHue plantHue, bool showType = false) : base(0xDCF)
    {
      Weight = 1.0;
      Stackable = Core.SA;

      m_PlantType = plantType;
      m_PlantHue = plantHue;
      m_ShowType = showType;

      Hue = PlantHueInfo.GetInfo(plantHue).Hue;
    }

    public Seed(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public PlantType PlantType
    {
      get => m_PlantType;
      set
      {
        m_PlantType = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public PlantHue PlantHue
    {
      get => m_PlantHue;
      set
      {
        m_PlantHue = value;
        Hue = PlantHueInfo.GetInfo(value).Hue;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool ShowType
    {
      get => m_ShowType;
      set
      {
        m_ShowType = value;
        InvalidateProperties();
      }
    }

    public override int LabelNumber => 1060810; // seed

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public static Seed RandomBonsaiSeed()
    {
      return RandomBonsaiSeed(0.5);
    }

    public static Seed RandomBonsaiSeed(double increaseRatio)
    {
      return new Seed(PlantTypeInfo.RandomBonsai(increaseRatio), PlantHue.Plain);
    }

    public static Seed RandomPeculiarSeed(int group)
    {
      switch (group)
      {
        case 1: return new Seed(PlantTypeInfo.RandomPeculiarGroupOne(), PlantHue.Plain);
        case 2: return new Seed(PlantTypeInfo.RandomPeculiarGroupTwo(), PlantHue.Plain);
        case 3: return new Seed(PlantTypeInfo.RandomPeculiarGroupThree(), PlantHue.Plain);
        default: return new Seed(PlantTypeInfo.RandomPeculiarGroupFour(), PlantHue.Plain);
      }
    }

    private int GetLabel(out string args)
    {
      PlantTypeInfo typeInfo = PlantTypeInfo.GetInfo(m_PlantType);
      PlantHueInfo hueInfo = PlantHueInfo.GetInfo(m_PlantHue);

      int title;

      if (m_ShowType || typeInfo.PlantCategory == PlantCategory.Default)
        title = hueInfo.Name;
      else
        title = (int)typeInfo.PlantCategory;

      if (Amount == 1)
      {
        if (m_ShowType)
        {
          args = $"#{title}\t#{typeInfo.Name}";
          return typeInfo.GetSeedLabel(hueInfo);
        }

        args = $"#{title}";
        return hueInfo.IsBright() ? 1060839 : 1060838; // [bright] ~1_val~ seed
      }

      if (m_ShowType)
      {
        args = $"{Amount}\t#{title}\t#{typeInfo.Name}";
        return typeInfo.GetSeedLabelPlural(hueInfo);
      }

      args = $"{Amount}\t#{title}";
      return hueInfo.IsBright() ? 1113491 : 1113490; // ~1_amount~ [bright] ~2_val~ seeds
    }

    public override void AddNameProperty(ObjectPropertyList list)
    {
      string args;
      list.Add(GetLabel(out args), args);
    }

    public override void OnSingleClick(Mobile from)
    {
      string args;
      LabelTo(from, GetLabel(out args), args);
    }

    public override void OnDoubleClick(Mobile from)
    {
      if (!IsChildOf(from.Backpack))
      {
        from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
        return;
      }

      from.Target = new InternalTarget(this);
      LabelTo(from, 1061916); // Choose a bowl of dirt to plant this seed in.
    }

    public override bool StackWith(Mobile from, Item dropped, bool playSound)
    {
      return dropped is Seed other && other.PlantType == m_PlantType && other.PlantHue == m_PlantHue &&
             other.ShowType == m_ShowType && base.StackWith(from, other, playSound);
    }

    public override void OnAfterDuped(Item newItem)
    {
      if (!(newItem is Seed newSeed))
        return;

      newSeed.PlantType = m_PlantType;
      newSeed.PlantHue = m_PlantHue;
      newSeed.ShowType = m_ShowType;
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(2); // version

      writer.Write((int)m_PlantType);
      writer.Write((int)m_PlantHue);
      writer.Write(m_ShowType);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      m_PlantType = (PlantType)reader.ReadInt();
      m_PlantHue = (PlantHue)reader.ReadInt();
      m_ShowType = reader.ReadBool();

      if (Weight != 1.0)
        Weight = 1.0;

      if (version < 1)
        Stackable = Core.SA;

      if (version < 2 && PlantHueInfo.IsCrossable(m_PlantHue))
        m_PlantHue |= PlantHue.Reproduces;
    }

    private class InternalTarget : Target
    {
      private Seed m_Seed;

      public InternalTarget(Seed seed) : base(-1, false, TargetFlags.None)
      {
        m_Seed = seed;
        CheckLOS = false;
      }

      protected override void OnTarget(Mobile from, object targeted)
      {
        if (m_Seed.Deleted)
          return;

        if (!m_Seed.IsChildOf(from.Backpack))
        {
          from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
          return;
        }

        if (targeted is PlantItem plant)
          plant.PlantSeed(from, m_Seed);
        else if (targeted is Item item)
          item.LabelTo(from, 1061919); // You must use a seed on a bowl of dirt!
        else
          from.SendLocalizedMessage(1061919); // You must use a seed on a bowl of dirt!
      }
    }
  }
}
