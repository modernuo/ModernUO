using System;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.Utilities;

namespace Server.Engines.Harvest
{
    public class Mining : HarvestSystem
    {
        private static Mining m_System;

        private static readonly int[] m_Offsets =
        {
            -1, -1,
            -1, 0,
            -1, 1,
            0, -1,
            0, 1,
            1, -1,
            1, 0,
            1, 1
        };

        private static readonly int[] m_MountainAndCaveTiles =
        {
            220, 221, 222, 223, 224, 225, 226, 227, 228, 229,
            230, 231, 236, 237, 238, 239, 240, 241, 242, 243,
            244, 245, 246, 247, 252, 253, 254, 255, 256, 257,
            258, 259, 260, 261, 262, 263, 268, 269, 270, 271,
            272, 273, 274, 275, 276, 277, 278, 279, 286, 287,
            288, 289, 290, 291, 292, 293, 294, 296, 296, 297,
            321, 322, 323, 324, 467, 468, 469, 470, 471, 472,
            473, 474, 476, 477, 478, 479, 480, 481, 482, 483,
            484, 485, 486, 487, 492, 493, 494, 495, 543, 544,
            545, 546, 547, 548, 549, 550, 551, 552, 553, 554,
            555, 556, 557, 558, 559, 560, 561, 562, 563, 564,
            565, 566, 567, 568, 569, 570, 571, 572, 573, 574,
            575, 576, 577, 578, 579, 581, 582, 583, 584, 585,
            586, 587, 588, 589, 590, 591, 592, 593, 594, 595,
            596, 597, 598, 599, 600, 601, 610, 611, 612, 613,

            1010, 1741, 1742, 1743, 1744, 1745, 1746, 1747, 1748, 1749,
            1750, 1751, 1752, 1753, 1754, 1755, 1756, 1757, 1771, 1772,
            1773, 1774, 1775, 1776, 1777, 1778, 1779, 1780, 1781, 1782,
            1783, 1784, 1785, 1786, 1787, 1788, 1789, 1790, 1801, 1802,
            1803, 1804, 1805, 1806, 1807, 1808, 1809, 1811, 1812, 1813,
            1814, 1815, 1816, 1817, 1818, 1819, 1820, 1821, 1822, 1823,
            1824, 1831, 1832, 1833, 1834, 1835, 1836, 1837, 1838, 1839,
            1840, 1841, 1842, 1843, 1844, 1845, 1846, 1847, 1848, 1849,
            1850, 1851, 1852, 1853, 1854, 1861, 1862, 1863, 1864, 1865,
            1866, 1867, 1868, 1869, 1870, 1871, 1872, 1873, 1874, 1875,
            1876, 1877, 1878, 1879, 1880, 1881, 1882, 1883, 1884, 1981,
            1982, 1983, 1984, 1985, 1986, 1987, 1988, 1989, 1990, 1991,
            1992, 1993, 1994, 1995, 1996, 1997, 1998, 1999, 2000, 2001,
            2002, 2003, 2004, 2028, 2029, 2030, 2031, 2032, 2033, 2100,
            2101, 2102, 2103, 2104, 2105,

            0x453B, 0x453C, 0x453D, 0x453E, 0x453F, 0x4540, 0x4541,
            0x4542, 0x4543, 0x4544, 0x4545, 0x4546, 0x4547, 0x4548,
            0x4549, 0x454A, 0x454B, 0x454C, 0x454D, 0x454E, 0x454F
        };

        private static readonly int[] m_SandTiles =
        {
            22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
            32, 33, 34, 35, 36, 37, 38, 39, 40, 41,
            42, 43, 44, 45, 46, 47, 48, 49, 50, 51,
            52, 53, 54, 55, 56, 57, 58, 59, 60, 61,
            62, 68, 69, 70, 71, 72, 73, 74, 75,

            286, 287, 288, 289, 290, 291, 292, 293, 294, 295,
            296, 297, 298, 299, 300, 301, 402, 424, 425, 426,
            427, 441, 442, 443, 444, 445, 446, 447, 448, 449,
            450, 451, 452, 453, 454, 455, 456, 457, 458, 459,
            460, 461, 462, 463, 464, 465, 642, 643, 644, 645,
            650, 651, 652, 653, 654, 655, 656, 657, 821, 822,
            823, 824, 825, 826, 827, 828, 833, 834, 835, 836,
            845, 846, 847, 848, 849, 850, 851, 852, 857, 858,
            859, 860, 951, 952, 953, 954, 955, 956, 957, 958,
            967, 968, 969, 970,

            1447, 1448, 1449, 1450, 1451, 1452, 1453, 1454, 1455,
            1456, 1457, 1458, 1611, 1612, 1613, 1614, 1615, 1616,
            1617, 1618, 1623, 1624, 1625, 1626, 1635, 1636, 1637,
            1638, 1639, 1640, 1641, 1642, 1647, 1648, 1649, 1650
        };

        private Mining()
        {
            OreAndStone = new HarvestDefinition
            {
                BankWidth = 8,
                BankHeight = 8,
                MinTotal = 10,
                MaxTotal = 34,
                MinRespawn = TimeSpan.FromMinutes(10.0),
                MaxRespawn = TimeSpan.FromMinutes(20.0),
                Skill = SkillName.Mining,
                Tiles = m_MountainAndCaveTiles,
                MaxRange = 2,
                ConsumedPerHarvest = 1,
                ConsumedPerFeluccaHarvest = 2,
                EffectActions = new[] { 11 },
                EffectSounds = new[] { 0x125, 0x126 },
                EffectCounts = new[] { 1 },
                EffectDelay = TimeSpan.FromSeconds(1.6),
                EffectSoundDelay = TimeSpan.FromSeconds(0.9),
                NoResourcesMessage = 503040,     // There is no metal here to mine.
                DoubleHarvestMessage = 503042,   // Someone has gotten to the metal before you.
                TimedOutOfRangeMessage = 503041, // You have moved too far away to continue mining.
                OutOfRangeMessage = 500446,      // That is too far away.
                FailMessage = 503043,            // You loosen some rocks but fail to find any useable ore.
                PackFullMessage = 1010481,       // Your backpack is full, so the ore you mined is lost.
                ToolBrokeMessage = 1044038       // You have worn out your tool!
            };

            HarvestResource[] res =
            {
                new(00.0, 00.0, 100.0, 1007072, typeof(IronOre), typeof(Granite)),
                new(
                    65.0,
                    25.0,
                    105.0,
                    1007073,
                    typeof(DullCopperOre),
                    typeof(DullCopperGranite),
                    typeof(DullCopperElemental)
                ),
                new(
                    70.0,
                    30.0,
                    110.0,
                    1007074,
                    typeof(ShadowIronOre),
                    typeof(ShadowIronGranite),
                    typeof(ShadowIronElemental)
                ),
                new(
                    75.0,
                    35.0,
                    115.0,
                    1007075,
                    typeof(CopperOre),
                    typeof(CopperGranite),
                    typeof(CopperElemental)
                ),
                new(
                    80.0,
                    40.0,
                    120.0,
                    1007076,
                    typeof(BronzeOre),
                    typeof(BronzeGranite),
                    typeof(BronzeElemental)
                ),
                new(
                    85.0,
                    45.0,
                    125.0,
                    1007077,
                    typeof(GoldOre),
                    typeof(GoldGranite),
                    typeof(GoldenElemental)
                ),
                new(
                    90.0,
                    50.0,
                    130.0,
                    1007078,
                    typeof(AgapiteOre),
                    typeof(AgapiteGranite),
                    typeof(AgapiteElemental)
                ),
                new(
                    95.0,
                    55.0,
                    135.0,
                    1007079,
                    typeof(VeriteOre),
                    typeof(VeriteGranite),
                    typeof(VeriteElemental)
                ),
                new(
                    99.0,
                    59.0,
                    139.0,
                    1007080,
                    typeof(ValoriteOre),
                    typeof(ValoriteGranite),
                    typeof(ValoriteElemental)
                )
            };

            HarvestVein[] veins =
            {
                new(496, 0.0, res[0], null),   // Iron
                new(112, 0.5, res[1], res[0]), // Dull Copper
                new(098, 0.5, res[2], res[0]), // Shadow Iron
                new(084, 0.5, res[3], res[0]), // Copper
                new(070, 0.5, res[4], res[0]), // Bronze
                new(056, 0.5, res[5], res[0]), // Gold
                new(042, 0.5, res[6], res[0]), // Agapite
                new(028, 0.5, res[7], res[0]), // Verite
                new(014, 0.5, res[8], res[0])  // Valorite
            };

            OreAndStone.Resources = res;
            OreAndStone.Veins = veins;

            if (Core.ML)
            {
                OreAndStone.BonusResources = new[]
                {
                    new BonusHarvestResource(0, 99.4, null, null), // Nothing
                    new BonusHarvestResource(100, .1, 1072562, typeof(BlueDiamond)),
                    new BonusHarvestResource(100, .1, 1072567, typeof(DarkSapphire)),
                    new BonusHarvestResource(100, .1, 1072570, typeof(EcruCitrine)),
                    new BonusHarvestResource(100, .1, 1072564, typeof(FireRuby)),
                    new BonusHarvestResource(100, .1, 1072566, typeof(PerfectEmerald)),
                    new BonusHarvestResource(100, .1, 1072568, typeof(Turquoise))
                };
            }

            OreAndStone.RaceBonus = Core.ML;
            OreAndStone.RandomizeVeins = Core.ML;

            Sand = new HarvestDefinition
            {
                BankWidth = 8,
                BankHeight = 8,
                MinTotal = 6,
                MaxTotal = 12,
                MinRespawn = TimeSpan.FromMinutes(10.0),
                MaxRespawn = TimeSpan.FromMinutes(20.0),
                Skill = SkillName.Mining,
                Tiles = m_SandTiles,
                MaxRange = 2,
                ConsumedPerHarvest = 1,
                ConsumedPerFeluccaHarvest = 1,
                EffectActions = new[] { 11 },
                EffectSounds = new[] { 0x125, 0x126 },
                EffectCounts = new[] { 6 },
                EffectDelay = TimeSpan.FromSeconds(1.6),
                EffectSoundDelay = TimeSpan.FromSeconds(0.9),
                NoResourcesMessage = 1044629, // There is no sand here to mine.
                DoubleHarvestMessage = 1044629, // There is no sand here to mine.
                TimedOutOfRangeMessage = 503041, // You have moved too far away to continue mining.
                OutOfRangeMessage = 500446, // That is too far away.
                FailMessage = 1044630, // You dig for a while but fail to find any of sufficient quality for glassblowing.
                PackFullMessage = 1044632, // Your backpack can't hold the sand, and it is lost!
                ToolBrokeMessage = 1044038 // You have worn out your tool!
            };

            res = new[]
            {
                new HarvestResource(100.0, 70.0, 400.0, 1044631, typeof(Sand))
            };

            veins = new[]
            {
                new HarvestVein(1000, 0.0, res[0], null)
            };

            Sand.Resources = res;
            Sand.Veins = veins;

            Definitions = new[] { OreAndStone, Sand };
        }

        public static Mining System => m_System ?? (m_System = new Mining());

        public HarvestDefinition OreAndStone { get; }

        public HarvestDefinition Sand { get; }

        public override Type GetResourceType(
            Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc,
            HarvestResource resource
        )
        {
            if (def != OreAndStone)
            {
                return base.GetResourceType(from, tool, def, map, loc, resource);
            }

            if (from.Skills.Mining.Base >= 100.0 && from is PlayerMobile pm && pm.StoneMining && pm.ToggleMiningStone
                && Utility.RandomDouble() < 0.1)
            {
                return resource.Types[1];
            }

            return resource.Types[0];
        }

        public override bool CheckHarvest(Mobile from, Item tool)
        {
            if (!base.CheckHarvest(from, tool))
            {
                return false;
            }

            if (from.Mounted)
            {
                from.SendLocalizedMessage(501864); // You can't mine while riding.
                return false;
            }

            if (from.IsBodyMod && !from.Body.IsHuman)
            {
                from.SendLocalizedMessage(501865); // You can't mine while polymorphed.
                return false;
            }

            return true;
        }

        public override void SendSuccessTo(Mobile from, Item item, HarvestResource resource)
        {
            if (item is BaseGranite)
            {
                from.SendLocalizedMessage(1044606); // You carefully extract some workable stone from the ore vein!
            }
            else
            {
                base.SendSuccessTo(from, item, resource);
            }
        }

        public override bool CheckHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            if (!base.CheckHarvest(from, tool, def, toHarvest))
            {
                return false;
            }

            if (def == Sand && !(from is PlayerMobile mobile && mobile.Skills.Mining.Base >= 100.0 &&
                                 mobile.SandMining))
            {
                OnBadHarvestTarget(from, tool, toHarvest);
                return false;
            }

            if (from.Mounted)
            {
                from.SendLocalizedMessage(501864); // You can't mine while riding.
                return false;
            }

            if (from.IsBodyMod && !from.Body.IsHuman)
            {
                from.SendLocalizedMessage(501865); // You can't mine while polymorphed.
                return false;
            }

            return true;
        }

        public override HarvestVein MutateVein(
            Mobile from, Item tool, HarvestDefinition def, HarvestBank bank,
            object toHarvest, HarvestVein vein
        )
        {
            if (tool is GargoylesPickaxe && def == OreAndStone)
            {
                var veinIndex = Array.IndexOf(def.Veins, vein);

                if (veinIndex >= 0 && veinIndex < def.Veins.Length - 1)
                {
                    return def.Veins[veinIndex + 1];
                }
            }

            return base.MutateVein(from, tool, def, bank, toHarvest, vein);
        }

        public override void OnHarvestFinished(
            Mobile from, Item tool, HarvestDefinition def, HarvestVein vein,
            HarvestBank bank, HarvestResource resource, object harvested
        )
        {
            if (tool is GargoylesPickaxe && def == OreAndStone && Utility.RandomDouble() < 0.1)
            {
                var res = vein.PrimaryResource;

                if (res == resource && res.Types.Length >= 3)
                {
                    var map = from.Map;

                    if (map == null)
                    {
                        return;
                    }

                    try
                    {
                        var spawned = res.Types[2].CreateEntityInstance<BaseCreature>(25);
                        if (spawned != null)
                        {
                            var offset = Utility.Random(8) * 2;

                            for (var i = 0; i < m_Offsets.Length; i += 2)
                            {
                                var x = from.X + m_Offsets[(offset + i) % m_Offsets.Length];
                                var y = from.Y + m_Offsets[(offset + i + 1) % m_Offsets.Length];

                                if (map.CanSpawnMobile(x, y, from.Z))
                                {
                                    spawned.OnBeforeSpawn(new Point3D(x, y, from.Z), map);
                                    spawned.MoveToWorld(new Point3D(x, y, from.Z), map);
                                    spawned.Combatant = from;
                                    return;
                                }

                                var z = map.GetAverageZ(x, y);

                                if ((z - from.Z).Abs() < 10 && map.CanSpawnMobile(x, y, z))
                                {
                                    spawned.OnBeforeSpawn(new Point3D(x, y, z), map);
                                    spawned.MoveToWorld(new Point3D(x, y, z), map);
                                    spawned.Combatant = from;
                                    return;
                                }
                            }

                            spawned.OnBeforeSpawn(from.Location, from.Map);
                            spawned.MoveToWorld(from.Location, from.Map);
                            spawned.Combatant = from;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        public override bool BeginHarvesting(Mobile from, Item tool)
        {
            if (!base.BeginHarvesting(from, tool))
            {
                return false;
            }

            from.SendLocalizedMessage(503033); // Where do you wish to dig?
            return true;
        }

        public override void OnHarvestStarted(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            base.OnHarvestStarted(from, tool, def, toHarvest);

            if (Core.ML)
            {
                from.RevealingAction();
            }
        }

        public override void OnBadHarvestTarget(Mobile from, Item tool, object toHarvest)
        {
            if (toHarvest is LandTarget)
            {
                from.SendLocalizedMessage(501862); // You can't mine there.
            }
            else
            {
                from.SendLocalizedMessage(501863); // You can't mine that.
            }
        }
    }
}
