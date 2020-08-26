using System;
using System.Collections.Generic;

namespace Server.Items
{
  public enum CraftResource
  {
    None = 0,
    Iron = 1,
    DullCopper,
    ShadowIron,
    Copper,
    Bronze,
    Gold,
    Agapite,
    Verite,
    Valorite,

    RegularLeather = 101,
    SpinedLeather,
    HornedLeather,
    BarbedLeather,

    RedScales = 201,
    YellowScales,
    BlackScales,
    GreenScales,
    WhiteScales,
    BlueScales,

    RegularWood = 301,
    OakWood,
    AshWood,
    YewWood,
    Heartwood,
    Bloodwood,
    Frostwood
  }

  public enum CraftResourceType
  {
    None,
    Metal,
    Leather,
    Scales,
    Wood
  }

  public class CraftAttributeInfo
  {
    public int WeaponFireDamage { get; set; }

    public int WeaponColdDamage { get; set; }

    public int WeaponPoisonDamage { get; set; }

    public int WeaponEnergyDamage { get; set; }

    public int WeaponChaosDamage { get; set; }

    public int WeaponDirectDamage { get; set; }

    public int WeaponDurability { get; set; }

    public int WeaponLuck { get; set; }

    public int WeaponGoldIncrease { get; set; }

    public int WeaponLowerRequirements { get; set; }

    public int ArmorPhysicalResist { get; set; }

    public int ArmorFireResist { get; set; }

    public int ArmorColdResist { get; set; }

    public int ArmorPoisonResist { get; set; }

    public int ArmorEnergyResist { get; set; }

    public int ArmorDurability { get; set; }

    public int ArmorLuck { get; set; }

    public int ArmorGoldIncrease { get; set; }

    public int ArmorLowerRequirements { get; set; }

    public int RunicMinAttributes { get; set; }

    public int RunicMaxAttributes { get; set; }

    public int RunicMinIntensity { get; set; }

    public int RunicMaxIntensity { get; set; }

    public static readonly CraftAttributeInfo Blank;
    public static readonly CraftAttributeInfo DullCopper, ShadowIron, Copper, Bronze, Golden, Agapite, Verite, Valorite;
    public static readonly CraftAttributeInfo Spined, Horned, Barbed;
    public static readonly CraftAttributeInfo RedScales, YellowScales, BlackScales, GreenScales, WhiteScales, BlueScales;
    public static readonly CraftAttributeInfo OakWood, AshWood, YewWood, Heartwood, Bloodwood, Frostwood;

    static CraftAttributeInfo()
    {
      Blank = new CraftAttributeInfo();

      CraftAttributeInfo dullCopper = DullCopper = new CraftAttributeInfo();

      dullCopper.ArmorPhysicalResist = 6;
      dullCopper.ArmorDurability = 50;
      dullCopper.ArmorLowerRequirements = 20;
      dullCopper.WeaponDurability = 100;
      dullCopper.WeaponLowerRequirements = 50;
      dullCopper.RunicMinAttributes = 1;
      dullCopper.RunicMaxAttributes = 2;
      if (Core.ML)
      {
        dullCopper.RunicMinIntensity = 40;
        dullCopper.RunicMaxIntensity = 100;
      }
      else
      {
        dullCopper.RunicMinIntensity = 10;
        dullCopper.RunicMaxIntensity = 35;
      }

      CraftAttributeInfo shadowIron = ShadowIron = new CraftAttributeInfo();

      shadowIron.ArmorPhysicalResist = 2;
      shadowIron.ArmorFireResist = 1;
      shadowIron.ArmorEnergyResist = 5;
      shadowIron.ArmorDurability = 100;
      shadowIron.WeaponColdDamage = 20;
      shadowIron.WeaponDurability = 50;
      shadowIron.RunicMinAttributes = 2;
      shadowIron.RunicMaxAttributes = 2;
      if (Core.ML)
      {
        shadowIron.RunicMinIntensity = 45;
        shadowIron.RunicMaxIntensity = 100;
      }
      else
      {
        shadowIron.RunicMinIntensity = 20;
        shadowIron.RunicMaxIntensity = 45;
      }

      CraftAttributeInfo copper = Copper = new CraftAttributeInfo();

      copper.ArmorPhysicalResist = 1;
      copper.ArmorFireResist = 1;
      copper.ArmorPoisonResist = 5;
      copper.ArmorEnergyResist = 2;
      copper.WeaponPoisonDamage = 10;
      copper.WeaponEnergyDamage = 20;
      copper.RunicMinAttributes = 2;
      copper.RunicMaxAttributes = 3;
      if (Core.ML)
      {
        copper.RunicMinIntensity = 50;
        copper.RunicMaxIntensity = 100;
      }
      else
      {
        copper.RunicMinIntensity = 25;
        copper.RunicMaxIntensity = 50;
      }

      CraftAttributeInfo bronze = Bronze = new CraftAttributeInfo();

      bronze.ArmorPhysicalResist = 3;
      bronze.ArmorColdResist = 5;
      bronze.ArmorPoisonResist = 1;
      bronze.ArmorEnergyResist = 1;
      bronze.WeaponFireDamage = 40;
      bronze.RunicMinAttributes = 3;
      bronze.RunicMaxAttributes = 3;
      if (Core.ML)
      {
        bronze.RunicMinIntensity = 55;
        bronze.RunicMaxIntensity = 100;
      }
      else
      {
        bronze.RunicMinIntensity = 30;
        bronze.RunicMaxIntensity = 65;
      }

      CraftAttributeInfo golden = Golden = new CraftAttributeInfo();

      golden.ArmorPhysicalResist = 1;
      golden.ArmorFireResist = 1;
      golden.ArmorColdResist = 2;
      golden.ArmorEnergyResist = 2;
      golden.ArmorLuck = 40;
      golden.ArmorLowerRequirements = 30;
      golden.WeaponLuck = 40;
      golden.WeaponLowerRequirements = 50;
      golden.RunicMinAttributes = 3;
      golden.RunicMaxAttributes = 4;
      if (Core.ML)
      {
        golden.RunicMinIntensity = 60;
        golden.RunicMaxIntensity = 100;
      }
      else
      {
        golden.RunicMinIntensity = 35;
        golden.RunicMaxIntensity = 75;
      }

      CraftAttributeInfo agapite = Agapite = new CraftAttributeInfo();

      agapite.ArmorPhysicalResist = 2;
      agapite.ArmorFireResist = 3;
      agapite.ArmorColdResist = 2;
      agapite.ArmorPoisonResist = 2;
      agapite.ArmorEnergyResist = 2;
      agapite.WeaponColdDamage = 30;
      agapite.WeaponEnergyDamage = 20;
      agapite.RunicMinAttributes = 4;
      agapite.RunicMaxAttributes = 4;
      if (Core.ML)
      {
        agapite.RunicMinIntensity = 65;
        agapite.RunicMaxIntensity = 100;
      }
      else
      {
        agapite.RunicMinIntensity = 40;
        agapite.RunicMaxIntensity = 80;
      }

      CraftAttributeInfo verite = Verite = new CraftAttributeInfo();

      verite.ArmorPhysicalResist = 3;
      verite.ArmorFireResist = 3;
      verite.ArmorColdResist = 2;
      verite.ArmorPoisonResist = 3;
      verite.ArmorEnergyResist = 1;
      verite.WeaponPoisonDamage = 40;
      verite.WeaponEnergyDamage = 20;
      verite.RunicMinAttributes = 4;
      verite.RunicMaxAttributes = 5;
      if (Core.ML)
      {
        verite.RunicMinIntensity = 70;
        verite.RunicMaxIntensity = 100;
      }
      else
      {
        verite.RunicMinIntensity = 45;
        verite.RunicMaxIntensity = 90;
      }

      CraftAttributeInfo valorite = Valorite = new CraftAttributeInfo();

      valorite.ArmorPhysicalResist = 4;
      valorite.ArmorColdResist = 3;
      valorite.ArmorPoisonResist = 3;
      valorite.ArmorEnergyResist = 3;
      valorite.ArmorDurability = 50;
      valorite.WeaponFireDamage = 10;
      valorite.WeaponColdDamage = 20;
      valorite.WeaponPoisonDamage = 10;
      valorite.WeaponEnergyDamage = 20;
      valorite.RunicMinAttributes = 5;
      valorite.RunicMaxAttributes = 5;
      if (Core.ML)
      {
        valorite.RunicMinIntensity = 85;
        valorite.RunicMaxIntensity = 100;
      }
      else
      {
        valorite.RunicMinIntensity = 50;
        valorite.RunicMaxIntensity = 100;
      }

      CraftAttributeInfo spined = Spined = new CraftAttributeInfo();

      spined.ArmorPhysicalResist = 5;
      spined.ArmorLuck = 40;
      spined.RunicMinAttributes = 1;
      spined.RunicMaxAttributes = 3;
      if (Core.ML)
      {
        spined.RunicMinIntensity = 40;
        spined.RunicMaxIntensity = 100;
      }
      else
      {
        spined.RunicMinIntensity = 20;
        spined.RunicMaxIntensity = 40;
      }

      CraftAttributeInfo horned = Horned = new CraftAttributeInfo();

      horned.ArmorPhysicalResist = 2;
      horned.ArmorFireResist = 3;
      horned.ArmorColdResist = 2;
      horned.ArmorPoisonResist = 2;
      horned.ArmorEnergyResist = 2;
      horned.RunicMinAttributes = 3;
      horned.RunicMaxAttributes = 4;
      if (Core.ML)
      {
        horned.RunicMinIntensity = 45;
        horned.RunicMaxIntensity = 100;
      }
      else
      {
        horned.RunicMinIntensity = 30;
        horned.RunicMaxIntensity = 70;
      }

      CraftAttributeInfo barbed = Barbed = new CraftAttributeInfo();

      barbed.ArmorPhysicalResist = 2;
      barbed.ArmorFireResist = 1;
      barbed.ArmorColdResist = 2;
      barbed.ArmorPoisonResist = 3;
      barbed.ArmorEnergyResist = 4;
      barbed.RunicMinAttributes = 4;
      barbed.RunicMaxAttributes = 5;
      if (Core.ML)
      {
        barbed.RunicMinIntensity = 50;
        barbed.RunicMaxIntensity = 100;
      }
      else
      {
        barbed.RunicMinIntensity = 40;
        barbed.RunicMaxIntensity = 100;
      }

      CraftAttributeInfo red = RedScales = new CraftAttributeInfo();

      red.ArmorFireResist = 10;
      red.ArmorColdResist = -3;

      CraftAttributeInfo yellow = YellowScales = new CraftAttributeInfo();

      yellow.ArmorPhysicalResist = -3;
      yellow.ArmorLuck = 20;

      CraftAttributeInfo black = BlackScales = new CraftAttributeInfo();

      black.ArmorPhysicalResist = 10;
      black.ArmorEnergyResist = -3;

      CraftAttributeInfo green = GreenScales = new CraftAttributeInfo();

      green.ArmorFireResist = -3;
      green.ArmorPoisonResist = 10;

      CraftAttributeInfo white = WhiteScales = new CraftAttributeInfo();

      white.ArmorPhysicalResist = -3;
      white.ArmorColdResist = 10;

      CraftAttributeInfo blue = BlueScales = new CraftAttributeInfo();

      blue.ArmorPoisonResist = -3;
      blue.ArmorEnergyResist = 10;

      // public static readonly CraftAttributeInfo OakWood, AshWood, YewWood, Heartwood, Bloodwood, Frostwood;

      CraftAttributeInfo oak = OakWood = new CraftAttributeInfo();

      CraftAttributeInfo ash = AshWood = new CraftAttributeInfo();

      CraftAttributeInfo yew = YewWood = new CraftAttributeInfo();

      CraftAttributeInfo heart = Heartwood = new CraftAttributeInfo();

      CraftAttributeInfo blood = Bloodwood = new CraftAttributeInfo();

      CraftAttributeInfo frost = Frostwood = new CraftAttributeInfo();
    }
  }

  public class CraftResourceInfo
  {
    public int Hue { get; }

    public int Number { get; }

    public string Name { get; }

    public CraftAttributeInfo AttributeInfo { get; }

    public CraftResource Resource { get; }

    public Type[] ResourceTypes { get; }

    public CraftResourceInfo(int hue, int number, string name, CraftAttributeInfo attributeInfo, CraftResource resource, params Type[] resourceTypes)
    {
      Hue = hue;
      Number = number;
      Name = name;
      AttributeInfo = attributeInfo;
      Resource = resource;
      ResourceTypes = resourceTypes;

      for (int i = 0; i < resourceTypes.Length; ++i)
        CraftResources.RegisterType(resourceTypes[i], resource);
    }
  }

  public static class CraftResources
  {
    private static readonly CraftResourceInfo[] m_MetalInfo = {
      new CraftResourceInfo(0x000, 1053109, "Iron",      CraftAttributeInfo.Blank,   CraftResource.Iron,       typeof(IronIngot),    typeof(IronOre),      typeof(Granite)),
      new CraftResourceInfo(0x973, 1053108, "Dull Copper", CraftAttributeInfo.DullCopper,  CraftResource.DullCopper,   typeof(DullCopperIngot),  typeof(DullCopperOre),  typeof(DullCopperGranite)),
      new CraftResourceInfo(0x966, 1053107, "Shadow Iron", CraftAttributeInfo.ShadowIron,  CraftResource.ShadowIron,   typeof(ShadowIronIngot),  typeof(ShadowIronOre),  typeof(ShadowIronGranite)),
      new CraftResourceInfo(0x96D, 1053106, "Copper",    CraftAttributeInfo.Copper,    CraftResource.Copper,     typeof(CopperIngot),    typeof(CopperOre),    typeof(CopperGranite)),
      new CraftResourceInfo(0x972, 1053105, "Bronze",    CraftAttributeInfo.Bronze,    CraftResource.Bronze,     typeof(BronzeIngot),    typeof(BronzeOre),    typeof(BronzeGranite)),
      new CraftResourceInfo(0x8A5, 1053104, "Gold",      CraftAttributeInfo.Golden,    CraftResource.Gold,       typeof(GoldIngot),    typeof(GoldOre),      typeof(GoldGranite)),
      new CraftResourceInfo(0x979, 1053103, "Agapite",   CraftAttributeInfo.Agapite,   CraftResource.Agapite,      typeof(AgapiteIngot),   typeof(AgapiteOre),   typeof(AgapiteGranite)),
      new CraftResourceInfo(0x89F, 1053102, "Verite",    CraftAttributeInfo.Verite,    CraftResource.Verite,     typeof(VeriteIngot),    typeof(VeriteOre),    typeof(VeriteGranite)),
      new CraftResourceInfo(0x8AB, 1053101, "Valorite",    CraftAttributeInfo.Valorite,  CraftResource.Valorite,     typeof(ValoriteIngot),  typeof(ValoriteOre),    typeof(ValoriteGranite))
    };

    private static readonly CraftResourceInfo[] m_ScaleInfo = {
      new CraftResourceInfo(0x66D, 1053129, "Red Scales",  CraftAttributeInfo.RedScales,   CraftResource.RedScales,    typeof(RedScales)),
      new CraftResourceInfo(0x8A8, 1053130, "Yellow Scales", CraftAttributeInfo.YellowScales,  CraftResource.YellowScales,   typeof(YellowScales)),
      new CraftResourceInfo(0x455, 1053131, "Black Scales",  CraftAttributeInfo.BlackScales,   CraftResource.BlackScales,    typeof(BlackScales)),
      new CraftResourceInfo(0x851, 1053132, "Green Scales",  CraftAttributeInfo.GreenScales,   CraftResource.GreenScales,    typeof(GreenScales)),
      new CraftResourceInfo(0x8FD, 1053133, "White Scales",  CraftAttributeInfo.WhiteScales,   CraftResource.WhiteScales,    typeof(WhiteScales)),
      new CraftResourceInfo(0x8B0, 1053134, "Blue Scales", CraftAttributeInfo.BlueScales,    CraftResource.BlueScales,   typeof(BlueScales))
    };

    private static readonly CraftResourceInfo[] m_LeatherInfo = {
      new CraftResourceInfo(0x000, 1049353, "Normal",    CraftAttributeInfo.Blank,   CraftResource.RegularLeather, typeof(Leather),      typeof(Hides)),
      new CraftResourceInfo(0x283, 1049354, "Spined",    CraftAttributeInfo.Spined,    CraftResource.SpinedLeather,  typeof(SpinedLeather),  typeof(SpinedHides)),
      new CraftResourceInfo(0x227, 1049355, "Horned",    CraftAttributeInfo.Horned,    CraftResource.HornedLeather,  typeof(HornedLeather),  typeof(HornedHides)),
      new CraftResourceInfo(0x1C1, 1049356, "Barbed",    CraftAttributeInfo.Barbed,    CraftResource.BarbedLeather,  typeof(BarbedLeather),  typeof(BarbedHides))
    };

    private static readonly CraftResourceInfo[] m_AOSLeatherInfo = {
      new CraftResourceInfo(0x000, 1049353, "Normal",    CraftAttributeInfo.Blank,   CraftResource.RegularLeather, typeof(Leather),      typeof(Hides)),
      new CraftResourceInfo(0x8AC, 1049354, "Spined",    CraftAttributeInfo.Spined,    CraftResource.SpinedLeather,  typeof(SpinedLeather),  typeof(SpinedHides)),
      new CraftResourceInfo(0x845, 1049355, "Horned",    CraftAttributeInfo.Horned,    CraftResource.HornedLeather,  typeof(HornedLeather),  typeof(HornedHides)),
      new CraftResourceInfo(0x851, 1049356, "Barbed",    CraftAttributeInfo.Barbed,    CraftResource.BarbedLeather,  typeof(BarbedLeather),  typeof(BarbedHides))
    };

    private static readonly CraftResourceInfo[] m_WoodInfo = {
      new CraftResourceInfo(0x000, 1011542, "Normal",    CraftAttributeInfo.Blank,   CraftResource.RegularWood,  typeof(Log),      typeof(Board)),
      new CraftResourceInfo(0x7DA, 1072533, "Oak",     CraftAttributeInfo.OakWood,   CraftResource.OakWood,    typeof(OakLog),   typeof(OakBoard)),
      new CraftResourceInfo(0x4A7, 1072534, "Ash",     CraftAttributeInfo.AshWood,   CraftResource.AshWood,    typeof(AshLog),   typeof(AshBoard)),
      new CraftResourceInfo(0x4A8, 1072535, "Yew",     CraftAttributeInfo.YewWood,   CraftResource.YewWood,    typeof(YewLog),   typeof(YewBoard)),
      new CraftResourceInfo(0x4A9, 1072536, "Heartwood",   CraftAttributeInfo.Heartwood, CraftResource.Heartwood,  typeof(HeartwoodLog), typeof(HeartwoodBoard)),
      new CraftResourceInfo(0x4AA, 1072538, "Bloodwood",   CraftAttributeInfo.Bloodwood, CraftResource.Bloodwood,  typeof(BloodwoodLog), typeof(BloodwoodBoard)),
      new CraftResourceInfo(0x47F, 1072539, "Frostwood",   CraftAttributeInfo.Frostwood, CraftResource.Frostwood,  typeof(FrostwoodLog), typeof(FrostwoodBoard))
    };

    /// <summary>
    /// Returns true if '<paramref name="resource"/>' is None, Iron, RegularLeather or RegularWood. False if otherwise.
    /// </summary>
    public static bool IsStandard(CraftResource resource) => resource == CraftResource.None || resource == CraftResource.Iron || resource == CraftResource.RegularLeather || resource == CraftResource.RegularWood;

    private static Dictionary<Type, CraftResource> m_TypeTable;

    /// <summary>
    /// Registers that '<paramref name="resourceType"/>' uses '<paramref name="resource"/>' so that it can later be queried by <see cref="CraftResources.GetFromType"/>
    /// </summary>
    public static void RegisterType(Type resourceType, CraftResource resource)
    {
      if (m_TypeTable == null)
        m_TypeTable = new Dictionary<Type, CraftResource>();

      m_TypeTable[resourceType] = resource;
    }

    /// <summary>
    /// Returns the <see cref="CraftResource"/> value for which '<paramref name="resourceType"/>' uses -or- CraftResource.None if an unregistered type was specified.
    /// </summary>
    public static CraftResource GetFromType(Type resourceType)
    {
      if (m_TypeTable == null)
        return CraftResource.None;

      return m_TypeTable.TryGetValue(resourceType, out CraftResource res) ? res : CraftResource.None;
    }

    /// <summary>
    /// Returns a <see cref="CraftResourceInfo"/> instance describing '<paramref name="resource"/>' -or- null if an invalid resource was specified.
    /// </summary>
    public static CraftResourceInfo GetInfo(CraftResource resource)
    {
      var list = GetType(resource) switch
      {
        CraftResourceType.Metal => m_MetalInfo,
        CraftResourceType.Leather => Core.AOS ? m_AOSLeatherInfo : m_LeatherInfo,
        CraftResourceType.Scales => m_ScaleInfo,
        CraftResourceType.Wood => m_WoodInfo,
        _ => null
      };

      if (list != null)
      {
        int index = GetIndex(resource);

        if (index >= 0 && index < list.Length)
          return list[index];
      }

      return null;
    }

    /// <summary>
    /// Returns a <see cref="CraftResourceType"/> value indiciating the type of '<paramref name="resource"/>'.
    /// </summary>
    public static CraftResourceType GetType(CraftResource resource)
    {
      if (resource >= CraftResource.Iron && resource <= CraftResource.Valorite)
        return CraftResourceType.Metal;

      if (resource >= CraftResource.RegularLeather && resource <= CraftResource.BarbedLeather)
        return CraftResourceType.Leather;

      if (resource >= CraftResource.RedScales && resource <= CraftResource.BlueScales)
        return CraftResourceType.Scales;

      if (resource >= CraftResource.RegularWood && resource <= CraftResource.Frostwood)
        return CraftResourceType.Wood;

      return CraftResourceType.None;
    }

    /// <summary>
    /// Returns the first <see cref="CraftResource"/> in the series of resources for which '<paramref name="resource"/>' belongs.
    /// </summary>
    public static CraftResource GetStart(CraftResource resource)
    {
      return GetType(resource) switch
      {
        CraftResourceType.Metal => CraftResource.Iron,
        CraftResourceType.Leather => CraftResource.RegularLeather,
        CraftResourceType.Scales => CraftResource.RedScales,
        CraftResourceType.Wood => CraftResource.RegularWood,
        _ => CraftResource.None
      };
    }

    /// <summary>
    /// Returns the index of '<paramref name="resource"/>' in the seriest of resources for which it belongs.
    /// </summary>
    public static int GetIndex(CraftResource resource)
    {
      CraftResource start = GetStart(resource);

      if (start == CraftResource.None)
        return 0;

      return resource - start;
    }

    /// <summary>
    /// Returns the <see cref="CraftResourceInfo.Number"/> property of '<paramref name="resource"/>' -or- 0 if an invalid resource was specified.
    /// </summary>
    public static int GetLocalizationNumber(CraftResource resource)
    {
      CraftResourceInfo info = GetInfo(resource);

      return info?.Number ?? 0;
    }

    /// <summary>
    /// Returns the <see cref="CraftResourceInfo.Hue"/> property of '<paramref name="resource"/>' -or- 0 if an invalid resource was specified.
    /// </summary>
    public static int GetHue(CraftResource resource)
    {
      CraftResourceInfo info = GetInfo(resource);

      return info?.Hue ?? 0;
    }

    /// <summary>
    /// Returns the <see cref="CraftResourceInfo.Name"/> property of '<paramref name="resource"/>' -or- an empty string if the resource specified was invalid.
    /// </summary>
    public static string GetName(CraftResource resource)
    {
      CraftResourceInfo info = GetInfo(resource);

      return info == null ? string.Empty : info.Name;
    }

    /// <summary>
    /// Returns the <see cref="CraftResource"/> value which represents '<paramref name="info"/>' -or- CraftResource.None if unable to convert.
    /// </summary>
    public static CraftResource GetFromOreInfo(OreInfo info)
    {
      if (info.Name.IndexOf("Spined") >= 0)
        return CraftResource.SpinedLeather;
      if (info.Name.IndexOf("Horned") >= 0)
        return CraftResource.HornedLeather;
      if (info.Name.IndexOf("Barbed") >= 0)
        return CraftResource.BarbedLeather;
      if (info.Name.IndexOf("Leather") >= 0)
        return CraftResource.RegularLeather;

      if (info.Level == 0)
        return CraftResource.Iron;
      if (info.Level == 1)
        return CraftResource.DullCopper;
      if (info.Level == 2)
        return CraftResource.ShadowIron;
      if (info.Level == 3)
        return CraftResource.Copper;
      if (info.Level == 4)
        return CraftResource.Bronze;
      if (info.Level == 5)
        return CraftResource.Gold;
      if (info.Level == 6)
        return CraftResource.Agapite;
      if (info.Level == 7)
        return CraftResource.Verite;
      if (info.Level == 8)
        return CraftResource.Valorite;

      return CraftResource.None;
    }

    /// <summary>
    /// Returns the <see cref="CraftResource"/> value which represents '<paramref name="info"/>', using '<paramref name="material"/>' to help resolve leather OreInfo instances.
    /// </summary>
    public static CraftResource GetFromOreInfo(OreInfo info, ArmorMaterialType material)
    {
      if (material == ArmorMaterialType.Studded || material == ArmorMaterialType.Leather || material == ArmorMaterialType.Spined ||
           material == ArmorMaterialType.Horned || material == ArmorMaterialType.Barbed)
      {
        if (info.Level == 0)
          return CraftResource.RegularLeather;
        if (info.Level == 1)
          return CraftResource.SpinedLeather;
        if (info.Level == 2)
          return CraftResource.HornedLeather;
        if (info.Level == 3)
          return CraftResource.BarbedLeather;

        return CraftResource.None;
      }

      return GetFromOreInfo(info);
    }
  }

  // NOTE: This class is only for compatability with very old RunUO versions.
  // No changes to it should be required for custom resources.
  public class OreInfo
  {
    public static readonly OreInfo Iron = new OreInfo(0, 0x000, "Iron");
    public static readonly OreInfo DullCopper = new OreInfo(1, 0x973, "Dull Copper");
    public static readonly OreInfo ShadowIron = new OreInfo(2, 0x966, "Shadow Iron");
    public static readonly OreInfo Copper = new OreInfo(3, 0x96D, "Copper");
    public static readonly OreInfo Bronze = new OreInfo(4, 0x972, "Bronze");
    public static readonly OreInfo Gold = new OreInfo(5, 0x8A5, "Gold");
    public static readonly OreInfo Agapite = new OreInfo(6, 0x979, "Agapite");
    public static readonly OreInfo Verite = new OreInfo(7, 0x89F, "Verite");
    public static readonly OreInfo Valorite = new OreInfo(8, 0x8AB, "Valorite");

    public OreInfo(int level, int hue, string name)
    {
      Level = level;
      Hue = hue;
      Name = name;
    }

    public int Level { get; }

    public int Hue { get; }

    public string Name { get; }
  }
}
