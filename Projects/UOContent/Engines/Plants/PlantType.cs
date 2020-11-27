namespace Server.Engines.Plants
{
    public enum PlantType
    {
        CampionFlowers,
        Poppies,
        Snowdrops,
        Bulrushes,
        Lilies,
        PampasGrass,
        Rushes,
        ElephantEarPlant,
        Fern,
        PonytailPalm,
        SmallPalm,
        CenturyPlant,
        WaterPlant,
        SnakePlant,
        PricklyPearCactus,
        BarrelCactus,
        TribarrelCactus,
        CommonGreenBonsai,
        CommonPinkBonsai,
        UncommonGreenBonsai,
        UncommonPinkBonsai,
        RareGreenBonsai,
        RarePinkBonsai,
        ExceptionalBonsai,
        ExoticBonsai,
        Cactus,
        FlaxFlowers,
        FoxgloveFlowers,
        HopsEast,
        OrfluerFlowers,
        CypressTwisted,
        HedgeShort,
        JuniperBush,
        SnowdropPatch,
        Cattails,
        PoppyPatch,
        SpiderTree,
        WaterLily,
        CypressStraight,
        HedgeTall,
        HopsSouth,
        SugarCanes,
        CocoaTree
    }

    public enum PlantCategory
    {
        Default,
        Common = 1063335,      //
        Uncommon = 1063336,    //
        Rare = 1063337,        // Bonsai
        Exceptional = 1063341, //
        Exotic = 1063342,      //
        Peculiar = 1080528,
        Fragrant = 1080529
    }

    public class PlantTypeInfo
    {
        private static readonly PlantTypeInfo[] m_Table =
        {
            new(0xC83, 0, 0, PlantType.CampionFlowers, false, true, true, true, PlantCategory.Default),
            new(0xC86, 0, 0, PlantType.Poppies, false, true, true, true, PlantCategory.Default),
            new(0xC88, 0, 10, PlantType.Snowdrops, false, true, true, true, PlantCategory.Default),
            new(0xC94, -15, 0, PlantType.Bulrushes, false, true, true, true, PlantCategory.Default),
            new(0xC8B, 0, 0, PlantType.Lilies, false, true, true, true, PlantCategory.Default),
            new(0xCA5, -8, 0, PlantType.PampasGrass, false, true, true, true, PlantCategory.Default),
            new(0xCA7, -10, 0, PlantType.Rushes, false, true, true, true, PlantCategory.Default),
            new(0xC97, -20, 0, PlantType.ElephantEarPlant, true, false, true, true, PlantCategory.Default),
            new(0xC9F, -20, 0, PlantType.Fern, false, false, true, true, PlantCategory.Default),
            new(0xCA6, -16, -5, PlantType.PonytailPalm, false, false, true, true, PlantCategory.Default),
            new(0xC9C, -5, -10, PlantType.SmallPalm, false, false, true, true, PlantCategory.Default),
            new(0xD31, 0, -27, PlantType.CenturyPlant, true, false, true, true, PlantCategory.Default),
            new(0xD04, 0, 10, PlantType.WaterPlant, true, false, true, true, PlantCategory.Default),
            new(0xCA9, 0, 0, PlantType.SnakePlant, true, false, true, true, PlantCategory.Default),
            new(0xD2C, 0, 10, PlantType.PricklyPearCactus, false, false, true, true, PlantCategory.Default),
            new(0xD26, 0, 10, PlantType.BarrelCactus, false, false, true, true, PlantCategory.Default),
            new(0xD27, 0, 10, PlantType.TribarrelCactus, false, false, true, true, PlantCategory.Default),
            new(0x28DC, -5, 5, PlantType.CommonGreenBonsai, true, false, false, false, PlantCategory.Common),
            new(0x28DF, -5, 5, PlantType.CommonPinkBonsai, true, false, false, false, PlantCategory.Common),
            new(
                0x28DD,
                -5,
                5,
                PlantType.UncommonGreenBonsai,
                true,
                false,
                false,
                false,
                PlantCategory.Uncommon
            ),
            new(
                0x28E0,
                -5,
                5,
                PlantType.UncommonPinkBonsai,
                true,
                false,
                false,
                false,
                PlantCategory.Uncommon
            ),
            new(0x28DE, -5, 5, PlantType.RareGreenBonsai, true, false, false, false, PlantCategory.Rare),
            new(0x28E1, -5, 5, PlantType.RarePinkBonsai, true, false, false, false, PlantCategory.Rare),
            new(
                0x28E2,
                -5,
                5,
                PlantType.ExceptionalBonsai,
                true,
                false,
                false,
                false,
                PlantCategory.Exceptional
            ),
            new(0x28E3, -5, 5, PlantType.ExoticBonsai, true, false, false, false, PlantCategory.Exotic),
            new(0x0D25, 0, 0, PlantType.Cactus, false, false, false, false, PlantCategory.Peculiar),
            new(0x1A9A, 5, 10, PlantType.FlaxFlowers, false, true, false, false, PlantCategory.Peculiar),
            new(0x0C84, 0, 0, PlantType.FoxgloveFlowers, false, true, false, false, PlantCategory.Peculiar),
            new(0x1A9F, 5, -25, PlantType.HopsEast, false, false, false, false, PlantCategory.Peculiar),
            new(0x0CC1, 0, 0, PlantType.OrfluerFlowers, false, true, false, false, PlantCategory.Peculiar),
            new(
                0x0CFE,
                -45,
                -30,
                PlantType.CypressTwisted,
                false,
                false,
                false,
                false,
                PlantCategory.Peculiar
            ),
            new(0x0C8F, 0, 0, PlantType.HedgeShort, false, false, false, false, PlantCategory.Peculiar),
            new(0x0CC8, 0, 0, PlantType.JuniperBush, true, false, false, false, PlantCategory.Peculiar),
            new(0x0C8E, -20, 0, PlantType.SnowdropPatch, false, true, false, false, PlantCategory.Peculiar),
            new(0x0CB7, 0, 0, PlantType.Cattails, false, false, false, false, PlantCategory.Peculiar),
            new(0x0CBE, -20, 0, PlantType.PoppyPatch, false, true, false, false, PlantCategory.Peculiar),
            new(0x0CC9, 0, 0, PlantType.SpiderTree, false, false, false, false, PlantCategory.Peculiar),
            new(0x0DC1, -5, 15, PlantType.WaterLily, false, true, false, false, PlantCategory.Peculiar),
            new(
                0x0CFB,
                -45,
                -30,
                PlantType.CypressStraight,
                false,
                false,
                false,
                false,
                PlantCategory.Peculiar
            ),
            new(0x0DB8, 0, -20, PlantType.HedgeTall, false, false, false, false, PlantCategory.Peculiar),
            new(0x1AA1, 10, -25, PlantType.HopsSouth, false, false, false, false, PlantCategory.Peculiar),
            new(
                0x246C,
                -25,
                -20,
                PlantType.SugarCanes,
                false,
                false,
                false,
                false,
                PlantCategory.Peculiar,
                1114898,
                1114898,
                1094702,
                1094703,
                1095221,
                1113715
            ),
            new(
                0xC9E,
                -40,
                -30,
                PlantType.CocoaTree,
                false,
                false,
                false,
                true,
                PlantCategory.Fragrant,
                1080536,
                1080536,
                1080534,
                1080531,
                1080533,
                1113716
            )
        };

        private readonly int m_PlantLabelDecorative;
        private readonly int m_PlantLabelFullGrown;
        private readonly int m_PlantLabelPlant;

        // Cliloc overrides
        private readonly int m_PlantLabelSeed;
        private readonly int m_SeedLabel;
        private readonly int m_SeedLabelPlural;

        private PlantTypeInfo(
            int itemID, int offsetX, int offsetY, PlantType plantType, bool containsPlant, bool flowery,
            bool crossable, bool reproduces, PlantCategory plantCategory, int plantLabelSeed = -1, int plantLabelPlant = -1,
            int plantLabelFullGrown = -1, int plantLabelDecorative = -1, int seedLabel = -1, int seedLabelPlural = -1
        )
        {
            ItemID = itemID;
            OffsetX = offsetX;
            OffsetY = offsetY;
            PlantType = plantType;
            ContainsPlant = containsPlant;
            Flowery = flowery;
            Crossable = crossable;
            Reproduces = reproduces;
            PlantCategory = plantCategory;
            m_PlantLabelSeed = plantLabelSeed;
            m_PlantLabelPlant = plantLabelPlant;
            m_PlantLabelFullGrown = plantLabelFullGrown;
            m_PlantLabelDecorative = plantLabelDecorative;
            m_SeedLabel = seedLabel;
            m_SeedLabelPlural = seedLabelPlural;
        }

        public int ItemID { get; }

        public int OffsetX { get; }

        public int OffsetY { get; }

        public PlantType PlantType { get; }

        public PlantCategory PlantCategory { get; }

        public int Name => ItemID < 0x4000 ? 1020000 + ItemID : 1078872 + ItemID;

        public bool ContainsPlant { get; }

        public bool Flowery { get; }

        public bool Crossable { get; }

        public bool Reproduces { get; }

        public static PlantTypeInfo GetInfo(PlantType plantType)
        {
            var index = (int)plantType;

            if (index >= 0 && index < m_Table.Length)
            {
                return m_Table[index];
            }

            return m_Table[0];
        }

        public static PlantType RandomFirstGeneration()
        {
            return Utility.Random(3) switch
            {
                0 => PlantType.CampionFlowers,
                1 => PlantType.Fern,
                _ => PlantType.TribarrelCactus
            };
        }

        public static PlantType RandomPeculiarGroupOne()
        {
            return Utility.Random(6) switch
            {
                0 => PlantType.Cactus,
                1 => PlantType.FlaxFlowers,
                2 => PlantType.FoxgloveFlowers,
                3 => PlantType.HopsEast,
                4 => PlantType.CocoaTree,
                _ => PlantType.OrfluerFlowers
            };
        }

        public static PlantType RandomPeculiarGroupTwo()
        {
            return Utility.Random(5) switch
            {
                0 => PlantType.CypressTwisted,
                1 => PlantType.HedgeShort,
                2 => PlantType.JuniperBush,
                3 => PlantType.CocoaTree,
                _ => PlantType.SnowdropPatch
            };
        }

        public static PlantType RandomPeculiarGroupThree()
        {
            return Utility.Random(5) switch
            {
                0 => PlantType.Cattails,
                1 => PlantType.PoppyPatch,
                2 => PlantType.SpiderTree,
                3 => PlantType.CocoaTree,
                _ => PlantType.WaterLily
            };
        }

        public static PlantType RandomPeculiarGroupFour()
        {
            return Utility.Random(5) switch
            {
                0 => PlantType.CypressStraight,
                1 => PlantType.HedgeTall,
                2 => PlantType.HopsSouth,
                3 => PlantType.CocoaTree,
                _ => PlantType.SugarCanes
            };
        }

        public static PlantType RandomBonsai(double increaseRatio)
        {
            /* Chances of each plant type are equal to the chances of the previous plant type * increaseRatio:
             * E.g.:
             *  chances_of_uncommon = chances_of_common * increaseRatio
             *  chances_of_rare = chances_of_uncommon * increaseRatio
             *  ...
             *
             * If increaseRatio < 1 -> rare plants are actually rarer than the others
             * If increaseRatio > 1 -> rare plants are actually more common than the others (it might be the case with certain monsters)
             *
             * If a plant type (common, uncommon, ...) has 2 different colors, they have the same chances:
             *  chances_of_green_common = chances_of_pink_common = chances_of_common / 2
             *  ...
             */

            var k1 = increaseRatio >= 0.0 ? increaseRatio : 0.0;
            var k2 = k1 * k1;
            var k3 = k2 * k1;
            var k4 = k3 * k1;

            var exp1 = k1 + 1.0;
            var exp2 = k2 + exp1;
            var exp3 = k3 + exp2;
            var exp4 = k4 + exp3;

            var rand = Utility.RandomDouble();

            if (rand < 0.5 / exp4)
            {
                return PlantType.CommonGreenBonsai;
            }

            if (rand < 1.0 / exp4)
            {
                return PlantType.CommonPinkBonsai;
            }

            if (rand < (k1 * 0.5 + 1.0) / exp4)
            {
                return PlantType.UncommonGreenBonsai;
            }

            if (rand < exp1 / exp4)
            {
                return PlantType.UncommonPinkBonsai;
            }

            if (rand < (k2 * 0.5 + exp1) / exp4)
            {
                return PlantType.RareGreenBonsai;
            }

            if (rand < exp2 / exp4)
            {
                return PlantType.RarePinkBonsai;
            }

            if (rand < exp3 / exp4)
            {
                return PlantType.ExceptionalBonsai;
            }

            return PlantType.ExoticBonsai;
        }

        public static bool IsCrossable(PlantType plantType) => GetInfo(plantType).Crossable;

        public static PlantType Cross(PlantType first, PlantType second)
        {
            if (!IsCrossable(first) || !IsCrossable(second))
            {
                return PlantType.CampionFlowers;
            }

            var firstIndex = (int)first;
            var secondIndex = (int)second;

            if (firstIndex + 1 == secondIndex || firstIndex == secondIndex + 1)
            {
                return Utility.RandomBool() ? first : second;
            }

            return (PlantType)((firstIndex + secondIndex) / 2);
        }

        public static bool CanReproduce(PlantType plantType) => GetInfo(plantType).Reproduces;

        public int GetPlantLabelSeed(PlantHueInfo hueInfo)
        {
            if (m_PlantLabelSeed != -1)
            {
                return m_PlantLabelSeed;
            }

            return
                hueInfo.IsBright()
                    ? 1061887
                    : 1061888; // a ~1_val~ of ~2_val~ dirt with a ~3_val~ [bright] ~4_val~ ~5_val~ ~6_val~
        }

        public int GetPlantLabelPlant(PlantHueInfo hueInfo)
        {
            if (m_PlantLabelPlant != -1)
            {
                return m_PlantLabelPlant;
            }

            if (ContainsPlant)
            {
                return
                    hueInfo.IsBright()
                        ? 1060832
                        : 1060831; // a ~1_val~ of ~2_val~ dirt with a ~3_val~ [bright] ~4_val~ ~5_val~
            }

            return
                hueInfo.IsBright()
                    ? 1061887
                    : 1061888; // a ~1_val~ of ~2_val~ dirt with a ~3_val~ [bright] ~4_val~ ~5_val~ ~6_val~
        }

        public int GetPlantLabelFullGrown(PlantHueInfo hueInfo)
        {
            if (m_PlantLabelFullGrown != -1)
            {
                return m_PlantLabelFullGrown;
            }

            if (ContainsPlant)
            {
                return hueInfo.IsBright() ? 1061891 : 1061889; // a ~1_HEALTH~ [bright] ~2_COLOR~ ~3_NAME~
            }

            return hueInfo.IsBright() ? 1061892 : 1061890;     // a ~1_HEALTH~ [bright] ~2_COLOR~ ~3_NAME~ plant
        }

        public int GetPlantLabelDecorative(PlantHueInfo hueInfo)
        {
            if (m_PlantLabelDecorative != -1)
            {
                return m_PlantLabelDecorative;
            }

            return hueInfo.IsBright() ? 1074267 : 1070973; // a decorative [bright] ~1_COLOR~ ~2_TYPE~
        }

        public int GetSeedLabel(PlantHueInfo hueInfo)
        {
            if (m_SeedLabel != -1)
            {
                return m_SeedLabel;
            }

            return hueInfo.IsBright() ? 1061918 : 1061917; // [bright] ~1_COLOR~ ~2_TYPE~ seed
        }

        public int GetSeedLabelPlural(PlantHueInfo hueInfo)
        {
            if (m_SeedLabelPlural != -1)
            {
                return m_SeedLabelPlural;
            }

            return hueInfo.IsBright() ? 1113493 : 1113492; // ~1_amount~ [bright] ~2_color~ ~3_type~ seeds
        }
    }
}
