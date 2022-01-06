using System;
using Server.Items;

namespace Server.Engines.BulkOrders
{
    public delegate Item ConstructCallback(int type);

    public sealed class RewardType
    {
        public RewardType(int points, params Type[] types)
        {
            Points = points;
            Types = types;
        }

        public int Points { get; }

        public Type[] Types { get; }

        public bool Contains(Type type)
        {
            for (var i = 0; i < Types.Length; ++i)
            {
                if (Types[i] == type)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class RewardItem
    {
        public RewardItem(int weight, ConstructCallback constructor, int type = 0)
        {
            Weight = weight;
            Constructor = constructor;
            Type = type;
        }

        public int Weight { get; }

        public ConstructCallback Constructor { get; }

        public int Type { get; }

        public Item Construct()
        {
            try
            {
                return Constructor(Type);
            }
            catch
            {
                return null;
            }
        }
    }

    public sealed class RewardGroup
    {
        public RewardGroup(int points, params RewardItem[] items)
        {
            Points = points;
            Items = items;
        }

        public int Points { get; }

        public RewardItem[] Items { get; }

        public RewardItem AcquireItem()
        {
            if (Items.Length == 0)
            {
                return null;
            }

            if (Items.Length == 1)
            {
                return Items[0];
            }

            var totalWeight = 0;

            for (var i = 0; i < Items.Length; ++i)
            {
                totalWeight += Items[i].Weight;
            }

            var randomWeight = Utility.Random(totalWeight);

            for (var i = 0; i < Items.Length; ++i)
            {
                var item = Items[i];

                if (randomWeight < item.Weight)
                {
                    return item;
                }

                randomWeight -= item.Weight;
            }

            return null;
        }
    }

    public abstract class RewardCalculator
    {
        public RewardGroup[] Groups { get; set; }

        public abstract int ComputePoints(
            int quantity, bool exceptional, BulkMaterialType material, int itemCount,
            Type type
        );

        public abstract int ComputeGold(int quantity, bool exceptional, BulkMaterialType material, int itemCount, Type type);

        public virtual int ComputeFame(SmallBOD bod)
        {
            var points = ComputePoints(bod) / 50;
            return points * points;
        }

        public virtual int ComputeFame(LargeBOD bod)
        {
            var points = ComputePoints(bod) / 50;
            return points * points;
        }

        public virtual int ComputePoints(SmallBOD bod) => ComputePoints(
            bod.AmountMax,
            bod.RequireExceptional,
            bod.Material,
            1,
            bod.Type
        );

        public virtual int ComputePoints(LargeBOD bod) =>
            ComputePoints(
                bod.AmountMax,
                bod.RequireExceptional,
                bod.Material,
                bod.Entries.Length,
                bod.Entries[0].Details.Type
            );

        public virtual int ComputeGold(SmallBOD bod) => ComputeGold(
            bod.AmountMax,
            bod.RequireExceptional,
            bod.Material,
            1,
            bod.Type
        );

        public virtual int ComputeGold(LargeBOD bod) =>
            ComputeGold(
                bod.AmountMax,
                bod.RequireExceptional,
                bod.Material,
                bod.Entries.Length,
                bod.Entries[0].Details.Type
            );

        public virtual RewardGroup LookupRewards(int points)
        {
            for (var i = Groups.Length - 1; i >= 1; --i)
            {
                var group = Groups[i];

                if (points >= group.Points)
                {
                    return group;
                }
            }

            return Groups[0];
        }

        public virtual int LookupTypePoints(RewardType[] types, Type type)
        {
            for (var i = 0; i < types.Length; ++i)
            {
                if (types[i].Contains(type))
                {
                    return types[i].Points;
                }
            }

            return 0;
        }
    }

    public sealed class SmithRewardCalculator : RewardCalculator
    {
        private static readonly ConstructCallback SturdyShovel = CreateSturdyShovel;
        private static readonly ConstructCallback SturdyPickaxe = CreateSturdyPickaxe;
        private static readonly ConstructCallback MiningGloves = CreateMiningGloves;
        private static readonly ConstructCallback GargoylesPickaxe = CreateGargoylesPickaxe;
        private static readonly ConstructCallback ProspectorsTool = CreateProspectorsTool;
        private static readonly ConstructCallback PowderOfTemperament = CreatePowderOfTemperament;
        private static readonly ConstructCallback RunicHammer = CreateRunicHammer;
        private static readonly ConstructCallback PowerScroll = CreatePowerScroll;
        private static readonly ConstructCallback ColoredAnvil = CreateColoredAnvil;
        private static readonly ConstructCallback AncientHammer = CreateAncientHammer;
        public static readonly SmithRewardCalculator Instance = new();

        private static readonly int[][][] m_GoldTable =
        {
            new[] // 1-part (regular)
            {
                new[] { 150, 250, 250, 400, 400, 750, 750, 1200, 1200 },
                new[] { 225, 375, 375, 600, 600, 1125, 1125, 1800, 1800 },
                new[] { 300, 500, 750, 800, 1050, 1500, 2250, 2400, 4000 }
            },
            new[] // 1-part (exceptional)
            {
                new[] { 250, 400, 400, 750, 750, 1500, 1500, 3000, 3000 },
                new[] { 375, 600, 600, 1125, 1125, 2250, 2250, 4500, 4500 },
                new[] { 500, 800, 1200, 1500, 2500, 3000, 6000, 6000, 12000 }
            },
            new[] // Ringmail (regular)
            {
                new[] { 3000, 5000, 5000, 7500, 7500, 10000, 10000, 15000, 15000 },
                new[] { 4500, 7500, 7500, 11250, 11500, 15000, 15000, 22500, 22500 },
                new[] { 6000, 10000, 15000, 15000, 20000, 20000, 30000, 30000, 50000 }
            },
            new[] // Ringmail (exceptional)
            {
                new[] { 5000, 10000, 10000, 15000, 15000, 25000, 25000, 50000, 50000 },
                new[] { 7500, 15000, 15000, 22500, 22500, 37500, 37500, 75000, 75000 },
                new[] { 10000, 20000, 30000, 30000, 50000, 50000, 100000, 100000, 200000 }
            },
            new[] // Chainmail (regular)
            {
                new[] { 4000, 7500, 7500, 10000, 10000, 15000, 15000, 25000, 25000 },
                new[] { 6000, 11250, 11250, 15000, 15000, 22500, 22500, 37500, 37500 },
                new[] { 8000, 15000, 20000, 20000, 30000, 30000, 50000, 50000, 100000 }
            },
            new[] // Chainmail (exceptional)
            {
                new[] { 7500, 15000, 15000, 25000, 25000, 50000, 50000, 100000, 100000 },
                new[] { 11250, 22500, 22500, 37500, 37500, 75000, 75000, 150000, 150000 },
                new[] { 15000, 30000, 50000, 50000, 100000, 100000, 200000, 200000, 200000 }
            },
            new[] // Platemail (regular)
            {
                new[] { 5000, 10000, 10000, 15000, 15000, 25000, 25000, 50000, 50000 },
                new[] { 7500, 15000, 15000, 22500, 22500, 37500, 37500, 75000, 75000 },
                new[] { 10000, 20000, 30000, 30000, 50000, 50000, 100000, 100000, 200000 }
            },
            new[] // Platemail (exceptional)
            {
                new[] { 10000, 25000, 25000, 50000, 50000, 100000, 100000, 100000, 100000 },
                new[] { 15000, 37500, 37500, 75000, 75000, 150000, 150000, 150000, 150000 },
                new[] { 20000, 50000, 100000, 100000, 200000, 200000, 200000, 200000, 200000 }
            },
            new[] // 2-part weapons (regular)
            {
                new[] { 3000, 0, 0, 0, 0, 0, 0, 0, 0 },
                new[] { 4500, 0, 0, 0, 0, 0, 0, 0, 0 },
                new[] { 6000, 0, 0, 0, 0, 0, 0, 0, 0 }
            },
            new[] // 2-part weapons (exceptional)
            {
                new[] { 5000, 0, 0, 0, 0, 0, 0, 0, 0 },
                new[] { 7500, 0, 0, 0, 0, 0, 0, 0, 0 },
                new[] { 10000, 0, 0, 0, 0, 0, 0, 0, 0 }
            },
            new[] // 5-part weapons (regular)
            {
                new[] { 4000, 0, 0, 0, 0, 0, 0, 0, 0 },
                new[] { 6000, 0, 0, 0, 0, 0, 0, 0, 0 },
                new[] { 8000, 0, 0, 0, 0, 0, 0, 0, 0 }
            },
            new[] // 5-part weapons (exceptional)
            {
                new[] { 7500, 0, 0, 0, 0, 0, 0, 0, 0 },
                new[] { 11250, 0, 0, 0, 0, 0, 0, 0, 0 },
                new[] { 15000, 0, 0, 0, 0, 0, 0, 0, 0 }
            },
            new[] // 6-part weapons (regular)
            {
                new[] { 4000, 0, 0, 0, 0, 0, 0, 0, 0 },
                new[] { 6000, 0, 0, 0, 0, 0, 0, 0, 0 },
                new[] { 10000, 0, 0, 0, 0, 0, 0, 0, 0 }
            },
            new[] // 6-part weapons (exceptional)
            {
                new[] { 7500, 0, 0, 0, 0, 0, 0, 0, 0 },
                new[] { 11250, 0, 0, 0, 0, 0, 0, 0, 0 },
                new[] { 15000, 0, 0, 0, 0, 0, 0, 0, 0 }
            }
        };

        private readonly RewardType[] m_Types =
        {
            // Armors
            new(200, typeof(RingmailGloves), typeof(RingmailChest), typeof(RingmailArms), typeof(RingmailLegs)),
            new(300, typeof(ChainCoif), typeof(ChainLegs), typeof(ChainChest)),
            new(
                400,
                typeof(PlateArms),
                typeof(PlateLegs),
                typeof(PlateHelm),
                typeof(PlateGorget),
                typeof(PlateGloves),
                typeof(PlateChest)
            ),

            // Weapons
            new(200, typeof(Bardiche), typeof(Halberd)),
            new(
                300,
                typeof(Dagger),
                typeof(ShortSpear),
                typeof(Spear),
                typeof(WarFork),
                typeof(Kryss)
            ), // OSI put the dagger in there.  Odd, ain't it.
            new(
                350,
                typeof(Axe),
                typeof(BattleAxe),
                typeof(DoubleAxe),
                typeof(ExecutionersAxe),
                typeof(LargeBattleAxe),
                typeof(TwoHandedAxe)
            ),
            new(
                350,
                typeof(Broadsword),
                typeof(Cutlass),
                typeof(Katana),
                typeof(Longsword),
                typeof(Scimitar), /*typeof( ThinLongsword ),*/
                typeof(VikingSword)
            ),
            new(
                350,
                typeof(WarAxe),
                typeof(HammerPick),
                typeof(Mace),
                typeof(Maul),
                typeof(WarHammer),
                typeof(WarMace)
            )
        };

        public SmithRewardCalculator()
        {
            Groups = new[]
            {
                new RewardGroup(0, new RewardItem(1, SturdyShovel)),
                new RewardGroup(25, new RewardItem(1, SturdyPickaxe)),
                new RewardGroup(
                    50,
                    new RewardItem(45, SturdyShovel),
                    new RewardItem(45, SturdyPickaxe),
                    new RewardItem(10, MiningGloves, 1)
                ),
                new RewardGroup(
                    200,
                    new RewardItem(45, GargoylesPickaxe),
                    new RewardItem(45, ProspectorsTool),
                    new RewardItem(10, MiningGloves, 3)
                ),
                new RewardGroup(
                    400,
                    new RewardItem(2, GargoylesPickaxe),
                    new RewardItem(2, ProspectorsTool),
                    new RewardItem(1, PowderOfTemperament)
                ),
                new RewardGroup(450, new RewardItem(9, PowderOfTemperament), new RewardItem(1, MiningGloves, 5)),
                new RewardGroup(500, new RewardItem(1, RunicHammer, 1)),
                new RewardGroup(550, new RewardItem(3, RunicHammer, 1), new RewardItem(2, RunicHammer, 2)),
                new RewardGroup(600, new RewardItem(1, RunicHammer, 2)),
                new RewardGroup(
                    625,
                    new RewardItem(3, RunicHammer, 2),
                    new RewardItem(6, PowerScroll, 5),
                    new RewardItem(1, ColoredAnvil)
                ),
                new RewardGroup(650, new RewardItem(1, RunicHammer, 3)),
                new RewardGroup(
                    675,
                    new RewardItem(1, ColoredAnvil),
                    new RewardItem(6, PowerScroll, 10),
                    new RewardItem(3, RunicHammer, 3)
                ),
                new RewardGroup(700, new RewardItem(1, RunicHammer, 4)),
                new RewardGroup(750, new RewardItem(1, AncientHammer, 10)),
                new RewardGroup(800, new RewardItem(1, PowerScroll, 15)),
                new RewardGroup(850, new RewardItem(1, AncientHammer, 15)),
                new RewardGroup(900, new RewardItem(1, PowerScroll, 20)),
                new RewardGroup(950, new RewardItem(1, RunicHammer, 5)),
                new RewardGroup(1000, new RewardItem(1, AncientHammer, 30)),
                new RewardGroup(1050, new RewardItem(1, RunicHammer, 6)),
                new RewardGroup(1100, new RewardItem(1, AncientHammer, 60)),
                new RewardGroup(1150, new RewardItem(1, RunicHammer, 7)),
                new RewardGroup(1200, new RewardItem(1, RunicHammer, 8))
            };
        }

        public override int ComputePoints(
            int quantity, bool exceptional, BulkMaterialType material, int itemCount,
            Type type
        )
        {
            var points = 0;

            if (quantity == 10)
            {
                points += 10;
            }
            else if (quantity == 15)
            {
                points += 25;
            }
            else if (quantity == 20)
            {
                points += 50;
            }

            if (exceptional)
            {
                points += 200;
            }

            if (itemCount > 1)
            {
                points += LookupTypePoints(m_Types, type);
            }

            if (material >= BulkMaterialType.DullCopper && material <= BulkMaterialType.Valorite)
            {
                points += 200 + 50 * (material - BulkMaterialType.DullCopper);
            }

            return points;
        }

        private int ComputeType(Type type, int itemCount)
        {
            // Item count of 1 means it's a small BOD.
            if (itemCount == 1)
            {
                return 0;
            }

            var typeIdx = 0;

            // Loop through the RewardTypes defined earlier and find the correct one.
            for (; typeIdx < 7; ++typeIdx)
            {
                if (m_Types[typeIdx].Contains(type))
                {
                    break;
                }
            }

            // Types 5, 6 and 7 are Large Weapon BODs with the same rewards.
            if (typeIdx > 5)
            {
                typeIdx = 5;
            }

            return (typeIdx + 1) * 2;
        }

        public override int ComputeGold(int quantity, bool exceptional, BulkMaterialType material, int itemCount, Type type)
        {
            var goldTable = m_GoldTable;

            var typeIndex = ComputeType(type, itemCount);
            var quanIndex = quantity switch
            {
                20 => 2,
                15 => 1,
                _  => 0
            };

            var mtrlIndex = material >= BulkMaterialType.DullCopper && material <= BulkMaterialType.Valorite
                ? 1 + (material - BulkMaterialType.DullCopper)
                : 0;

            if (exceptional)
            {
                typeIndex++;
            }

            var gold = goldTable[typeIndex][quanIndex][mtrlIndex];

            var min = gold * 9 / 10;
            var max = gold * 10 / 9;

            return Utility.RandomMinMax(min, max);
        }

        private static Item CreateSturdyShovel(int type) => new SturdyShovel();

        private static Item CreateSturdyPickaxe(int type) => new SturdyPickaxe();

        private static Item CreateMiningGloves(int type)
        {
            return type switch
            {
                1 => new LeatherGlovesOfMining(1),
                3 => new StuddedGlovesOfMining(3),
                5 => new RingmailGlovesOfMining(5),
                _ => throw new InvalidOperationException()
            };
        }

        private static Item CreateGargoylesPickaxe(int type) => new GargoylesPickaxe();

        private static Item CreateProspectorsTool(int type) => new ProspectorsTool();

        private static Item CreatePowderOfTemperament(int type) => new PowderOfTemperament();

        private static Item CreateRunicHammer(int type)
        {
            if (type >= 1 && type <= 8)
            {
                return new RunicHammer(CraftResource.Iron + type, Core.AOS ? 55 - type * 5 : 50);
            }

            throw new InvalidOperationException();
        }

        private static Item CreatePowerScroll(int type)
        {
            if (type is 5 or 10 or 15 or 20)
            {
                return new PowerScroll(SkillName.Blacksmith, 100 + type);
            }

            throw new InvalidOperationException();
        }

        private static Item CreateColoredAnvil(int type) => new ColoredAnvil();

        private static Item CreateAncientHammer(int type)
        {
            if (type is 10 or 15 or 30 or 60)
            {
                return new AncientSmithyHammer(type);
            }

            throw new InvalidOperationException();
        }
    }

    public sealed class TailorRewardCalculator : RewardCalculator
    {
        private static readonly ConstructCallback Cloth = CreateCloth;
        private static readonly ConstructCallback Sandals = CreateSandals;
        private static readonly ConstructCallback StretchedHide = CreateStretchedHide;
        private static readonly ConstructCallback RunicKit = CreateRunicKit;
        private static readonly ConstructCallback Tapestry = CreateTapestry;
        private static readonly ConstructCallback PowerScroll = CreatePowerScroll;
        private static readonly ConstructCallback BearRug = CreateBearRug;
        private static readonly ConstructCallback ClothingBlessDeed = CreateCBD;
        public static readonly TailorRewardCalculator Instance = new();

        private static readonly int[][][] m_AosGoldTable =
        {
            new[] // 1-part (regular)
            {
                new[] { 150, 150, 300, 300 },
                new[] { 225, 225, 450, 450 },
                new[] { 300, 400, 600, 750 }
            },
            new[] // 1-part (exceptional)
            {
                new[] { 300, 300, 600, 600 },
                new[] { 450, 450, 900, 900 },
                new[] { 600, 750, 1200, 1800 }
            },
            new[] // 4-part (regular)
            {
                new[] { 4000, 4000, 5000, 5000 },
                new[] { 6000, 6000, 7500, 7500 },
                new[] { 8000, 10000, 10000, 15000 }
            },
            new[] // 4-part (exceptional)
            {
                new[] { 5000, 5000, 7500, 7500 },
                new[] { 7500, 7500, 11250, 11250 },
                new[] { 10000, 15000, 15000, 20000 }
            },
            new[] // 5-part (regular)
            {
                new[] { 5000, 5000, 7500, 7500 },
                new[] { 7500, 7500, 11250, 11250 },
                new[] { 10000, 15000, 15000, 20000 }
            },
            new[] // 5-part (exceptional)
            {
                new[] { 7500, 7500, 10000, 10000 },
                new[] { 11250, 11250, 15000, 15000 },
                new[] { 15000, 20000, 20000, 30000 }
            },
            new[] // 6-part (regular)
            {
                new[] { 7500, 7500, 10000, 10000 },
                new[] { 11250, 11250, 15000, 15000 },
                new[] { 15000, 20000, 20000, 30000 }
            },
            new[] // 6-part (exceptional)
            {
                new[] { 10000, 10000, 15000, 15000 },
                new[] { 15000, 15000, 22500, 22500 },
                new[] { 20000, 30000, 30000, 50000 }
            }
        };

        private static readonly int[][][] m_OldGoldTable =
        {
            new[] // 1-part (regular)
            {
                new[] { 150, 150, 300, 300 },
                new[] { 225, 225, 450, 450 },
                new[] { 300, 400, 600, 750 }
            },
            new[] // 1-part (exceptional)
            {
                new[] { 300, 300, 600, 600 },
                new[] { 450, 450, 900, 900 },
                new[] { 600, 750, 1200, 1800 }
            },
            new[] // 4-part (regular)
            {
                new[] { 3000, 3000, 4000, 4000 },
                new[] { 4500, 4500, 6000, 6000 },
                new[] { 6000, 8000, 8000, 10000 }
            },
            new[] // 4-part (exceptional)
            {
                new[] { 4000, 4000, 5000, 5000 },
                new[] { 6000, 6000, 7500, 7500 },
                new[] { 8000, 10000, 10000, 15000 }
            },
            new[] // 5-part (regular)
            {
                new[] { 4000, 4000, 5000, 5000 },
                new[] { 6000, 6000, 7500, 7500 },
                new[] { 8000, 10000, 10000, 15000 }
            },
            new[] // 5-part (exceptional)
            {
                new[] { 5000, 5000, 7500, 7500 },
                new[] { 7500, 7500, 11250, 11250 },
                new[] { 10000, 15000, 15000, 20000 }
            },
            new[] // 6-part (regular)
            {
                new[] { 5000, 5000, 7500, 7500 },
                new[] { 7500, 7500, 11250, 11250 },
                new[] { 10000, 15000, 15000, 20000 }
            },
            new[] // 6-part (exceptional)
            {
                new[] { 7500, 7500, 10000, 10000 },
                new[] { 11250, 11250, 15000, 15000 },
                new[] { 15000, 20000, 20000, 30000 }
            }
        };

        private static readonly int[][] m_ClothHues =
        {
            new[] { 0x483, 0x48C, 0x488, 0x48A },
            new[] { 0x495, 0x48B, 0x486, 0x485 },
            new[] { 0x48D, 0x490, 0x48E, 0x491 },
            new[] { 0x48F, 0x494, 0x484, 0x497 },
            new[] { 0x489, 0x47F, 0x482, 0x47E }
        };

        private static readonly int[] m_SandalHues =
        {
            0x489, 0x47F, 0x482,
            0x47E, 0x48F, 0x494,
            0x484, 0x497
        };

        public TailorRewardCalculator()
        {
            Groups = new[]
            {
                new RewardGroup(0, new RewardItem(1, Cloth)),
                new RewardGroup(50, new RewardItem(1, Cloth, 1)),
                new RewardGroup(100, new RewardItem(1, Cloth, 2)),
                new RewardGroup(150, new RewardItem(9, Cloth, 3), new RewardItem(1, Sandals)),
                new RewardGroup(200, new RewardItem(4, Cloth, 4), new RewardItem(1, Sandals)),
                new RewardGroup(300, new RewardItem(1, StretchedHide)),
                new RewardGroup(350, new RewardItem(1, RunicKit, 1)),
                new RewardGroup(400, new RewardItem(2, PowerScroll, 5), new RewardItem(3, Tapestry)),
                new RewardGroup(450, new RewardItem(1, BearRug)),
                new RewardGroup(500, new RewardItem(1, PowerScroll, 10)),
                new RewardGroup(550, new RewardItem(1, ClothingBlessDeed)),
                new RewardGroup(575, new RewardItem(1, PowerScroll, 15)),
                new RewardGroup(600, new RewardItem(1, RunicKit, 2)),
                new RewardGroup(650, new RewardItem(1, PowerScroll, 20)),
                new RewardGroup(700, new RewardItem(1, RunicKit, 3))
            };
        }

        public override int ComputePoints(
            int quantity, bool exceptional, BulkMaterialType material, int itemCount,
            Type type
        )
        {
            var points = 0;

            if (quantity == 10)
            {
                points += 10;
            }
            else if (quantity == 15)
            {
                points += 25;
            }
            else if (quantity == 20)
            {
                points += 50;
            }

            if (exceptional)
            {
                points += 100;
            }

            if (itemCount == 4)
            {
                points += 300;
            }
            else if (itemCount == 5)
            {
                points += 400;
            }
            else if (itemCount == 6)
            {
                points += 500;
            }

            if (material == BulkMaterialType.Spined)
            {
                points += 50;
            }
            else if (material == BulkMaterialType.Horned)
            {
                points += 100;
            }
            else if (material == BulkMaterialType.Barbed)
            {
                points += 150;
            }

            return points;
        }

        public override int ComputeGold(int quantity, bool exceptional, BulkMaterialType material, int itemCount, Type type)
        {
            var goldTable = Core.AOS ? m_AosGoldTable : m_OldGoldTable;

            var typeIndex = itemCount switch
            {
                6 => 3,
                5 => 2,
                4 => 1,
                _ => 0
            } * 2 + (exceptional ? 1 : 0);

            var quanIndex = quantity switch
            {
                20 => 2,
                15 => 1,
                _  => 0
            };

            var mtrlIndex = material switch
            {
                BulkMaterialType.Barbed => 3,
                BulkMaterialType.Horned => 2,
                BulkMaterialType.Spined => 1,
                _                       => 0
            };

            var gold = goldTable[typeIndex][quanIndex][mtrlIndex];

            var min = gold * 9 / 10;
            var max = gold * 10 / 9;

            return Utility.RandomMinMax(min, max);
        }

        private static Item CreateCloth(int type)
        {
            if (type >= 0 && type < m_ClothHues.Length)
            {
                return new UncutCloth(100) { Hue = m_ClothHues[type].RandomElement() };
            }

            throw new InvalidOperationException();
        }

        private static Item CreateSandals(int type) => new Sandals(m_SandalHues.RandomElement());

        private static Item CreateStretchedHide(int type) =>
            Utility.Random(4) switch
            {
                1 => new SmallStretchedHideSouthDeed(),
                2 => new MediumStretchedHideEastDeed(),
                3 => new MediumStretchedHideSouthDeed(),
                _ => new SmallStretchedHideEastDeed()
            };

        private static Item CreateTapestry(int type) =>
            Utility.Random(4) switch
            {
                1 => new LightFlowerTapestrySouthDeed(),
                2 => new DarkFlowerTapestryEastDeed(),
                3 => new DarkFlowerTapestrySouthDeed(),
                _ => new LightFlowerTapestryEastDeed()
            };

        private static Item CreateBearRug(int type) =>
            Utility.Random(4) switch
            {
                1 => new BrownBearRugSouthDeed(),
                2 => new PolarBearRugEastDeed(),
                3 => new PolarBearRugSouthDeed(),
                _ => new BrownBearRugEastDeed()
            };

        private static Item CreateRunicKit(int type)
        {
            if (type >= 1 && type <= 3)
            {
                return new RunicSewingKit(CraftResource.RegularLeather + type, 60 - type * 15);
            }

            throw new InvalidOperationException();
        }

        private static Item CreatePowerScroll(int type)
        {
            if (type is 5 or 10 or 15 or 20)
            {
                return new PowerScroll(SkillName.Tailoring, 100 + type);
            }

            throw new InvalidOperationException();
        }

        private static Item CreateCBD(int type) => new ClothingBlessDeed();
    }
}
