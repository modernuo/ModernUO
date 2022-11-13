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
        public static readonly CraftAttributeInfo Blank;
        public static readonly CraftAttributeInfo DullCopper, ShadowIron, Copper, Bronze, Golden, Agapite, Verite, Valorite;
        public static readonly CraftAttributeInfo Spined, Horned, Barbed;
        public static readonly CraftAttributeInfo RedScales, YellowScales, BlackScales, GreenScales, WhiteScales, BlueScales;
        public static readonly CraftAttributeInfo OakWood, AshWood, YewWood, Heartwood, Bloodwood, Frostwood;

        static CraftAttributeInfo()
        {
            Blank = new CraftAttributeInfo();

            var dullCopper = DullCopper = new CraftAttributeInfo();

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

            var shadowIron = ShadowIron = new CraftAttributeInfo();

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

            var copper = Copper = new CraftAttributeInfo();

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

            var bronze = Bronze = new CraftAttributeInfo();

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

            var golden = Golden = new CraftAttributeInfo();

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

            var agapite = Agapite = new CraftAttributeInfo();

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

            var verite = Verite = new CraftAttributeInfo();

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

            var valorite = Valorite = new CraftAttributeInfo();

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

            var spined = Spined = new CraftAttributeInfo();

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

            var horned = Horned = new CraftAttributeInfo();

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

            var barbed = Barbed = new CraftAttributeInfo();

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

            var red = RedScales = new CraftAttributeInfo();

            red.ArmorFireResist = 10;
            red.ArmorColdResist = -3;

            var yellow = YellowScales = new CraftAttributeInfo();

            yellow.ArmorPhysicalResist = -3;
            yellow.ArmorLuck = 20;

            var black = BlackScales = new CraftAttributeInfo();

            black.ArmorPhysicalResist = 10;
            black.ArmorEnergyResist = -3;

            var green = GreenScales = new CraftAttributeInfo();

            green.ArmorFireResist = -3;
            green.ArmorPoisonResist = 10;

            var white = WhiteScales = new CraftAttributeInfo();

            white.ArmorPhysicalResist = -3;
            white.ArmorColdResist = 10;

            var blue = BlueScales = new CraftAttributeInfo();

            blue.ArmorPoisonResist = -3;
            blue.ArmorEnergyResist = 10;

            // public static readonly CraftAttributeInfo OakWood, AshWood, YewWood, Heartwood, Bloodwood, Frostwood;

            var oak = OakWood = new CraftAttributeInfo();

            var ash = AshWood = new CraftAttributeInfo();

            var yew = YewWood = new CraftAttributeInfo();

            var heart = Heartwood = new CraftAttributeInfo();

            var blood = Bloodwood = new CraftAttributeInfo();

            var frost = Frostwood = new CraftAttributeInfo();
        }

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
    }

    public class CraftResourceInfo
    {
        public CraftResourceInfo(
            int hue, int number, string name, CraftAttributeInfo attributeInfo, CraftResource resource,
            params Type[] resourceTypes
        )
        {
            Hue = hue;
            Number = number;
            Name = name;
            AttributeInfo = attributeInfo;
            Resource = resource;
            ResourceTypes = resourceTypes;

            for (var i = 0; i < resourceTypes.Length; ++i)
            {
                CraftResources.RegisterType(resourceTypes[i], resource);
            }
        }

        public int Hue { get; }

        public int Number { get; }

        public string Name { get; }

        public CraftAttributeInfo AttributeInfo { get; }

        public CraftResource Resource { get; }

        public Type[] ResourceTypes { get; }
    }

    public static class CraftResources
    {
        private static readonly CraftResourceInfo[] m_MetalInfo =
        {
            new(
                0x000,
                1053109,
                "Iron",
                CraftAttributeInfo.Blank,
                CraftResource.Iron,
                typeof(IronIngot),
                typeof(IronOre),
                typeof(Granite)
            ),
            new(
                0x973,
                1053108,
                "Dull Copper",
                CraftAttributeInfo.DullCopper,
                CraftResource.DullCopper,
                typeof(DullCopperIngot),
                typeof(DullCopperOre),
                typeof(DullCopperGranite)
            ),
            new(
                0x966,
                1053107,
                "Shadow Iron",
                CraftAttributeInfo.ShadowIron,
                CraftResource.ShadowIron,
                typeof(ShadowIronIngot),
                typeof(ShadowIronOre),
                typeof(ShadowIronGranite)
            ),
            new(
                0x96D,
                1053106,
                "Copper",
                CraftAttributeInfo.Copper,
                CraftResource.Copper,
                typeof(CopperIngot),
                typeof(CopperOre),
                typeof(CopperGranite)
            ),
            new(
                0x972,
                1053105,
                "Bronze",
                CraftAttributeInfo.Bronze,
                CraftResource.Bronze,
                typeof(BronzeIngot),
                typeof(BronzeOre),
                typeof(BronzeGranite)
            ),
            new(
                0x8A5,
                1053104,
                "Gold",
                CraftAttributeInfo.Golden,
                CraftResource.Gold,
                typeof(GoldIngot),
                typeof(GoldOre),
                typeof(GoldGranite)
            ),
            new(
                0x979,
                1053103,
                "Agapite",
                CraftAttributeInfo.Agapite,
                CraftResource.Agapite,
                typeof(AgapiteIngot),
                typeof(AgapiteOre),
                typeof(AgapiteGranite)
            ),
            new(
                0x89F,
                1053102,
                "Verite",
                CraftAttributeInfo.Verite,
                CraftResource.Verite,
                typeof(VeriteIngot),
                typeof(VeriteOre),
                typeof(VeriteGranite)
            ),
            new(
                0x8AB,
                1053101,
                "Valorite",
                CraftAttributeInfo.Valorite,
                CraftResource.Valorite,
                typeof(ValoriteIngot),
                typeof(ValoriteOre),
                typeof(ValoriteGranite)
            )
        };

        private static readonly CraftResourceInfo[] m_ScaleInfo =
        {
            new(
                0x66D,
                1053129,
                "Red Scales",
                CraftAttributeInfo.RedScales,
                CraftResource.RedScales,
                typeof(RedScales)
            ),
            new(
                0x8A8,
                1053130,
                "Yellow Scales",
                CraftAttributeInfo.YellowScales,
                CraftResource.YellowScales,
                typeof(YellowScales)
            ),
            new(
                0x455,
                1053131,
                "Black Scales",
                CraftAttributeInfo.BlackScales,
                CraftResource.BlackScales,
                typeof(BlackScales)
            ),
            new(
                0x851,
                1053132,
                "Green Scales",
                CraftAttributeInfo.GreenScales,
                CraftResource.GreenScales,
                typeof(GreenScales)
            ),
            new(
                0x8FD,
                1053133,
                "White Scales",
                CraftAttributeInfo.WhiteScales,
                CraftResource.WhiteScales,
                typeof(WhiteScales)
            ),
            new(
                0x8B0,
                1053134,
                "Blue Scales",
                CraftAttributeInfo.BlueScales,
                CraftResource.BlueScales,
                typeof(BlueScales)
            )
        };

        private static readonly CraftResourceInfo[] m_LeatherInfo =
        {
            new(
                0x000,
                1049353,
                "Normal",
                CraftAttributeInfo.Blank,
                CraftResource.RegularLeather,
                typeof(Leather),
                typeof(Hides)
            ),
            new(
                0x283,
                1049354,
                "Spined",
                CraftAttributeInfo.Spined,
                CraftResource.SpinedLeather,
                typeof(SpinedLeather),
                typeof(SpinedHides)
            ),
            new(
                0x227,
                1049355,
                "Horned",
                CraftAttributeInfo.Horned,
                CraftResource.HornedLeather,
                typeof(HornedLeather),
                typeof(HornedHides)
            ),
            new(
                0x1C1,
                1049356,
                "Barbed",
                CraftAttributeInfo.Barbed,
                CraftResource.BarbedLeather,
                typeof(BarbedLeather),
                typeof(BarbedHides)
            )
        };

        private static readonly CraftResourceInfo[] m_AOSLeatherInfo =
        {
            new(
                0x000,
                1049353,
                "Normal",
                CraftAttributeInfo.Blank,
                CraftResource.RegularLeather,
                typeof(Leather),
                typeof(Hides)
            ),
            new(
                0x8AC,
                1049354,
                "Spined",
                CraftAttributeInfo.Spined,
                CraftResource.SpinedLeather,
                typeof(SpinedLeather),
                typeof(SpinedHides)
            ),
            new(
                0x845,
                1049355,
                "Horned",
                CraftAttributeInfo.Horned,
                CraftResource.HornedLeather,
                typeof(HornedLeather),
                typeof(HornedHides)
            ),
            new(
                0x851,
                1049356,
                "Barbed",
                CraftAttributeInfo.Barbed,
                CraftResource.BarbedLeather,
                typeof(BarbedLeather),
                typeof(BarbedHides)
            )
        };

        private static readonly CraftResourceInfo[] m_WoodInfo =
        {
            new(
                0x000,
                1011542,
                "Normal",
                CraftAttributeInfo.Blank,
                CraftResource.RegularWood,
                typeof(Log),
                typeof(Board)
            ),
            new(
                0x7DA,
                1072533,
                "Oak",
                CraftAttributeInfo.OakWood,
                CraftResource.OakWood,
                typeof(OakLog),
                typeof(OakBoard)
            ),
            new(
                0x4A7,
                1072534,
                "Ash",
                CraftAttributeInfo.AshWood,
                CraftResource.AshWood,
                typeof(AshLog),
                typeof(AshBoard)
            ),
            new(
                0x4A8,
                1072535,
                "Yew",
                CraftAttributeInfo.YewWood,
                CraftResource.YewWood,
                typeof(YewLog),
                typeof(YewBoard)
            ),
            new(
                0x4A9,
                1072536,
                "Heartwood",
                CraftAttributeInfo.Heartwood,
                CraftResource.Heartwood,
                typeof(HeartwoodLog),
                typeof(HeartwoodBoard)
            ),
            new(
                0x4AA,
                1072538,
                "Bloodwood",
                CraftAttributeInfo.Bloodwood,
                CraftResource.Bloodwood,
                typeof(BloodwoodLog),
                typeof(BloodwoodBoard)
            ),
            new(
                0x47F,
                1072539,
                "Frostwood",
                CraftAttributeInfo.Frostwood,
                CraftResource.Frostwood,
                typeof(FrostwoodLog),
                typeof(FrostwoodBoard)
            )
        };

        private static Dictionary<Type, CraftResource> m_TypeTable;

        /// <summary>
        ///     Returns true if '<paramref name="resource" />' is None, Iron, RegularLeather or RegularWood. False if otherwise.
        /// </summary>
        public static bool IsStandard(CraftResource resource) =>
            resource is CraftResource.None or CraftResource.Iron or CraftResource.RegularLeather or CraftResource.RegularWood;

        /// <summary>
        ///     Registers that '<paramref name="resourceType" />' uses '<paramref name="resource" />' so that it can later be queried by
        ///     <see cref="CraftResources.GetFromType" />
        /// </summary>
        public static void RegisterType(Type resourceType, CraftResource resource)
        {
            if (m_TypeTable == null)
            {
                m_TypeTable = new Dictionary<Type, CraftResource>();
            }

            m_TypeTable[resourceType] = resource;
        }

        /// <summary>
        ///     Returns the <see cref="CraftResource" /> value for which '<paramref name="resourceType" />' uses -or- CraftResource.None
        ///     if an unregistered type was specified.
        /// </summary>
        public static CraftResource GetFromType(Type resourceType)
        {
            if (m_TypeTable == null)
            {
                return CraftResource.None;
            }

            return m_TypeTable.TryGetValue(resourceType, out var res) ? res : CraftResource.None;
        }

        /// <summary>
        ///     Returns a <see cref="CraftResourceInfo" /> instance describing '<paramref name="resource" />' -or- null if an invalid
        ///     resource was specified.
        /// </summary>
        public static CraftResourceInfo GetInfo(CraftResource resource)
        {
            var list = GetType(resource) switch
            {
                CraftResourceType.Metal   => m_MetalInfo,
                CraftResourceType.Leather => Core.AOS ? m_AOSLeatherInfo : m_LeatherInfo,
                CraftResourceType.Scales  => m_ScaleInfo,
                CraftResourceType.Wood    => m_WoodInfo,
                _                         => null
            };

            if (list != null)
            {
                var index = GetIndex(resource);

                if (index >= 0 && index < list.Length)
                {
                    return list[index];
                }
            }

            return null;
        }

        /// <summary>
        ///     Returns a <see cref="CraftResourceType" /> value indiciating the type of '<paramref name="resource" />'.
        /// </summary>
        public static CraftResourceType GetType(CraftResource resource)
        {
            if (resource >= CraftResource.Iron && resource <= CraftResource.Valorite)
            {
                return CraftResourceType.Metal;
            }

            if (resource >= CraftResource.RegularLeather && resource <= CraftResource.BarbedLeather)
            {
                return CraftResourceType.Leather;
            }

            if (resource >= CraftResource.RedScales && resource <= CraftResource.BlueScales)
            {
                return CraftResourceType.Scales;
            }

            if (resource >= CraftResource.RegularWood && resource <= CraftResource.Frostwood)
            {
                return CraftResourceType.Wood;
            }

            return CraftResourceType.None;
        }

        /// <summary>
        ///     Returns the first <see cref="CraftResource" /> in the series of resources for which '<paramref name="resource" />'
        ///     belongs.
        /// </summary>
        public static CraftResource GetStart(CraftResource resource) =>
            GetType(resource) switch
            {
                CraftResourceType.Metal   => CraftResource.Iron,
                CraftResourceType.Leather => CraftResource.RegularLeather,
                CraftResourceType.Scales  => CraftResource.RedScales,
                CraftResourceType.Wood    => CraftResource.RegularWood,
                _                         => CraftResource.None
            };

        /// <summary>
        ///     Returns the index of '<paramref name="resource" />' in the seriest of resources for which it belongs.
        /// </summary>
        public static int GetIndex(CraftResource resource)
        {
            var start = GetStart(resource);

            if (start == CraftResource.None)
            {
                return 0;
            }

            return resource - start;
        }

        /// <summary>
        ///     Returns the <see cref="CraftResourceInfo.Number" /> property of '<paramref name="resource" />' -or- 0 if an invalid
        ///     resource was specified.
        /// </summary>
        public static int GetLocalizationNumber(CraftResource resource)
        {
            var info = GetInfo(resource);

            return info?.Number ?? 0;
        }

        /// <summary>
        ///     Returns the <see cref="CraftResourceInfo.Hue" /> property of '<paramref name="resource" />' -or- 0 if an invalid
        ///     resource was specified.
        /// </summary>
        public static int GetHue(CraftResource resource)
        {
            var info = GetInfo(resource);

            return info?.Hue ?? 0;
        }

        /// <summary>
        ///     Returns the <see cref="CraftResourceInfo.Name" /> property of '<paramref name="resource" />' -or- an empty string if the
        ///     resource specified was invalid.
        /// </summary>
        public static string GetName(CraftResource resource)
        {
            var info = GetInfo(resource);

            return info == null ? string.Empty : info.Name;
        }
    }
}
