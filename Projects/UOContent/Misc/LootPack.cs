using System;
using Server.Items;
using Server.Mobiles;
using Server.Utilities;

namespace Server
{
    public class LootPack
    {
        public static readonly LootPackItem[] Gold =
        {
            new(typeof(Gold), 1)
        };

        public static readonly LootPackItem[] Instruments =
        {
            new(typeof(BaseInstrument), 1)
        };

        public static readonly LootPackItem[] LowScrollItems =
        {
            new(typeof(ClumsyScroll), 1)
        };

        public static readonly LootPackItem[] MedScrollItems =
        {
            new(typeof(ArchCureScroll), 1)
        };

        public static readonly LootPackItem[] HighScrollItems =
        {
            new(typeof(SummonAirElementalScroll), 1)
        };

        public static readonly LootPackItem[] GemItems =
        {
            new(typeof(Amber), 1)
        };

        public static readonly LootPackItem[] PotionItems =
        {
            new(typeof(AgilityPotion), 1),
            new(typeof(StrengthPotion), 1),
            new(typeof(RefreshPotion), 1),
            new(typeof(LesserCurePotion), 1),
            new(typeof(LesserHealPotion), 1),
            new(typeof(LesserPoisonPotion), 1)
        };

        public static readonly LootPackItem[] OldMagicItems =
        {
            new(typeof(BaseJewel), 1),
            new(typeof(BaseArmor), 4),
            new(typeof(BaseWeapon), 3),
            new(typeof(BaseRanged), 1),
            new(typeof(BaseShield), 1)
        };

        public static readonly LootPack LowScrolls = new(
            new[]
            {
                new LootPackEntry(false, LowScrollItems, 100.00, 1)
            }
        );

        public static readonly LootPack MedScrolls = new(
            new[]
            {
                new LootPackEntry(false, MedScrollItems, 100.00, 1)
            }
        );

        public static readonly LootPack HighScrolls = new(
            new[]
            {
                new LootPackEntry(false, HighScrollItems, 100.00, 1)
            }
        );

        public static readonly LootPack Gems = new(
            new[]
            {
                new LootPackEntry(false, GemItems, 100.00, 1)
            }
        );

        public static readonly LootPack Potions = new(
            new[]
            {
                new LootPackEntry(false, PotionItems, 100.00, 1)
            }
        );

        private readonly LootPackEntry[] m_Entries;

        public LootPack(LootPackEntry[] entries) => m_Entries = entries;

        public static int GetLuckChance(Mobile killer, Mobile victim)
        {
            if (!Core.AOS)
            {
                return 0;
            }

            var luck = killer.Luck;

            if (killer is PlayerMobile pmKiller && pmKiller.SentHonorContext != null &&
                pmKiller.SentHonorContext.Target == victim)
            {
                luck += pmKiller.SentHonorContext.PerfectionLuckBonus;
            }

            if (luck < 0)
            {
                return 0;
            }

            if (!Core.SE && luck > 1200)
            {
                luck = 1200;
            }

            return (int)(Math.Pow(luck, 1 / 1.8) * 100);
        }

        public static int GetLuckChanceForKiller(Mobile dead)
        {
            var list = BaseCreature.GetLootingRights(dead.DamageEntries, dead.HitsMax);

            DamageStore highest = null;

            for (var i = 0; i < list.Count; ++i)
            {
                var ds = list[i];

                if (ds.m_HasRight && (highest == null || ds.m_Damage > highest.m_Damage))
                {
                    highest = ds;
                }
            }

            if (highest == null)
            {
                return 0;
            }

            return GetLuckChance(highest.m_Mobile, dead);
        }

        public static bool CheckLuck(int chance) => chance > Utility.Random(10000);

        public void Generate(Mobile from, Container cont, bool spawning, int luckChance)
        {
            if (cont == null)
            {
                return;
            }

            var checkLuck = Core.AOS;

            for (var i = 0; i < m_Entries.Length; ++i)
            {
                var entry = m_Entries[i];

                var shouldAdd = entry.Chance > Utility.Random(10000);

                if (!shouldAdd && checkLuck)
                {
                    checkLuck = false;

                    if (CheckLuck(luckChance))
                    {
                        shouldAdd = entry.Chance > Utility.Random(10000);
                    }
                }

                if (!shouldAdd)
                {
                    continue;
                }

                var item = entry.Construct(from, luckChance, spawning);

                if (item != null)
                {
                    if (!item.Stackable || !cont.TryDropItem(from, item, false))
                    {
                        cont.DropItem(item);
                    }
                }
            }
        }

        public static readonly LootPackItem[] AosMagicItemsRichType1 =
        {
            new(typeof(BaseWeapon), 211),
            new(typeof(BaseRanged), 53),
            new(typeof(BaseArmor), 303),
            new(typeof(BaseShield), 39),
            new(typeof(BaseJewel), 158)
        };

        public static readonly LootPack MlRich = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "4d50+450"),
                new LootPackEntry(false, AosMagicItemsRichType1, 100.00, 1, 3, 0, 75),
                new LootPackEntry(false, AosMagicItemsRichType1, 80.00, 1, 3, 0, 75),
                new LootPackEntry(false, AosMagicItemsRichType1, 60.00, 1, 5, 0, 100),
                new LootPackEntry(false, Instruments, 1.00, 1)
            }
        );

        public static readonly LootPackItem[] AosMagicItemsPoor =
        {
            new(typeof(BaseWeapon), 3),
            new(typeof(BaseRanged), 1),
            new(typeof(BaseArmor), 4),
            new(typeof(BaseShield), 1),
            new(typeof(BaseJewel), 2)
        };

        public static readonly LootPackItem[] AosMagicItemsMeagerType1 =
        {
            new(typeof(BaseWeapon), 56),
            new(typeof(BaseRanged), 14),
            new(typeof(BaseArmor), 81),
            new(typeof(BaseShield), 11),
            new(typeof(BaseJewel), 42)
        };

        public static readonly LootPackItem[] AosMagicItemsMeagerType2 =
        {
            new(typeof(BaseWeapon), 28),
            new(typeof(BaseRanged), 7),
            new(typeof(BaseArmor), 40),
            new(typeof(BaseShield), 5),
            new(typeof(BaseJewel), 21)
        };

        public static readonly LootPackItem[] AosMagicItemsAverageType1 =
        {
            new(typeof(BaseWeapon), 90),
            new(typeof(BaseRanged), 23),
            new(typeof(BaseArmor), 130),
            new(typeof(BaseShield), 17),
            new(typeof(BaseJewel), 68)
        };

        public static readonly LootPackItem[] AosMagicItemsAverageType2 =
        {
            new(typeof(BaseWeapon), 54),
            new(typeof(BaseRanged), 13),
            new(typeof(BaseArmor), 77),
            new(typeof(BaseShield), 10),
            new(typeof(BaseJewel), 40)
        };

        public static readonly LootPackItem[] AosMagicItemsRichType2 =
        {
            new(typeof(BaseWeapon), 170),
            new(typeof(BaseRanged), 43),
            new(typeof(BaseArmor), 245),
            new(typeof(BaseShield), 32),
            new(typeof(BaseJewel), 128)
        };

        public static readonly LootPackItem[] AosMagicItemsFilthyRichType1 =
        {
            new(typeof(BaseWeapon), 219),
            new(typeof(BaseRanged), 55),
            new(typeof(BaseArmor), 315),
            new(typeof(BaseShield), 41),
            new(typeof(BaseJewel), 164)
        };

        public static readonly LootPackItem[] AosMagicItemsFilthyRichType2 =
        {
            new(typeof(BaseWeapon), 239),
            new(typeof(BaseRanged), 60),
            new(typeof(BaseArmor), 343),
            new(typeof(BaseShield), 90),
            new(typeof(BaseJewel), 45)
        };

        public static readonly LootPackItem[] AosMagicItemsUltraRich =
        {
            new(typeof(BaseWeapon), 276),
            new(typeof(BaseRanged), 69),
            new(typeof(BaseArmor), 397),
            new(typeof(BaseShield), 52),
            new(typeof(BaseJewel), 207)
        };

        public static readonly LootPack SePoor = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "2d10+20"),
                new LootPackEntry(false, AosMagicItemsPoor, 1.00, 1, 5, 0, 100),
                new LootPackEntry(false, Instruments, 0.02, 1)
            }
        );

        public static readonly LootPack SeMeager = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "4d10+40"),
                new LootPackEntry(false, AosMagicItemsMeagerType1, 20.40, 1, 2, 0, 50),
                new LootPackEntry(false, AosMagicItemsMeagerType2, 10.20, 1, 5, 0, 100),
                new LootPackEntry(false, Instruments, 0.10, 1)
            }
        );

        public static readonly LootPack SeAverage = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "8d10+100"),
                new LootPackEntry(false, AosMagicItemsAverageType1, 32.80, 1, 3, 0, 50),
                new LootPackEntry(false, AosMagicItemsAverageType1, 32.80, 1, 4, 0, 75),
                new LootPackEntry(false, AosMagicItemsAverageType2, 19.50, 1, 5, 0, 100),
                new LootPackEntry(false, Instruments, 0.40, 1)
            }
        );

        public static readonly LootPack SeRich = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "15d10+225"),
                new LootPackEntry(false, AosMagicItemsRichType1, 76.30, 1, 4, 0, 75),
                new LootPackEntry(false, AosMagicItemsRichType1, 76.30, 1, 4, 0, 75),
                new LootPackEntry(false, AosMagicItemsRichType2, 61.70, 1, 5, 0, 100),
                new LootPackEntry(false, Instruments, 1.00, 1)
            }
        );

        public static readonly LootPack SeFilthyRich = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "3d100+400"),
                new LootPackEntry(false, AosMagicItemsFilthyRichType1, 79.50, 1, 5, 0, 100),
                new LootPackEntry(false, AosMagicItemsFilthyRichType1, 79.50, 1, 5, 0, 100),
                new LootPackEntry(false, AosMagicItemsFilthyRichType2, 77.60, 1, 5, 25, 100),
                new LootPackEntry(false, Instruments, 2.00, 1)
            }
        );

        public static readonly LootPack SeUltraRich = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "6d100+600"),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 33, 100),
                new LootPackEntry(false, Instruments, 2.00, 1)
            }
        );

        public static readonly LootPack SeSuperBoss = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "10d100+800"),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 33, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 33, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 33, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 33, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 50, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 50, 100),
                new LootPackEntry(false, Instruments, 2.00, 1)
            }
        );

        public static readonly LootPack AosPoor = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "1d10+10"),
                new LootPackEntry(false, AosMagicItemsPoor, 0.02, 1, 5, 0, 90),
                new LootPackEntry(false, Instruments, 0.02, 1)
            }
        );

        public static readonly LootPack AosMeager = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "3d10+20"),
                new LootPackEntry(false, AosMagicItemsMeagerType1, 1.00, 1, 2, 0, 10),
                new LootPackEntry(false, AosMagicItemsMeagerType2, 0.20, 1, 5, 0, 90),
                new LootPackEntry(false, Instruments, 0.10, 1)
            }
        );

        public static readonly LootPack AosAverage = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "5d10+50"),
                new LootPackEntry(false, AosMagicItemsAverageType1, 5.00, 1, 4, 0, 20),
                new LootPackEntry(false, AosMagicItemsAverageType1, 2.00, 1, 3, 0, 50),
                new LootPackEntry(false, AosMagicItemsAverageType2, 0.50, 1, 5, 0, 90),
                new LootPackEntry(false, Instruments, 0.40, 1)
            }
        );

        public static readonly LootPack AosRich = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "10d10+150"),
                new LootPackEntry(false, AosMagicItemsRichType1, 20.00, 1, 4, 0, 40),
                new LootPackEntry(false, AosMagicItemsRichType1, 10.00, 1, 5, 0, 60),
                new LootPackEntry(false, AosMagicItemsRichType2, 1.00, 1, 5, 0, 90),
                new LootPackEntry(false, Instruments, 1.00, 1)
            }
        );

        public static readonly LootPack AosFilthyRich = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "2d100+200"),
                new LootPackEntry(false, AosMagicItemsFilthyRichType1, 33.00, 1, 4, 0, 50),
                new LootPackEntry(false, AosMagicItemsFilthyRichType1, 33.00, 1, 4, 0, 60),
                new LootPackEntry(false, AosMagicItemsFilthyRichType2, 20.00, 1, 5, 0, 75),
                new LootPackEntry(false, AosMagicItemsFilthyRichType2, 5.00, 1, 5, 0, 100),
                new LootPackEntry(false, Instruments, 2.00, 1)
            }
        );

        public static readonly LootPack AosUltraRich = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "5d100+500"),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 35, 100),
                new LootPackEntry(false, Instruments, 2.00, 1)
            }
        );

        public static readonly LootPack AosSuperBoss = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "5d100+500"),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 25, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 33, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 33, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 33, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 33, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 50, 100),
                new LootPackEntry(false, AosMagicItemsUltraRich, 100.00, 1, 5, 50, 100),
                new LootPackEntry(false, Instruments, 2.00, 1)
            }
        );

        public static readonly LootPack OldPoor = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "1d25"),
                new LootPackEntry(false, Instruments, 0.02, 1)
            }
        );

        public static readonly LootPack OldMeager = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "5d10+25"),
                new LootPackEntry(false, Instruments, 0.10, 1),
                new LootPackEntry(false, OldMagicItems, 1.00, 1, 1, 0, 60),
                new LootPackEntry(false, OldMagicItems, 0.20, 1, 1, 10, 70)
            }
        );

        public static readonly LootPack OldAverage = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "10d10+50"),
                new LootPackEntry(false, Instruments, 0.40, 1),
                new LootPackEntry(false, OldMagicItems, 5.00, 1, 1, 20, 80),
                new LootPackEntry(false, OldMagicItems, 2.00, 1, 1, 30, 90),
                new LootPackEntry(false, OldMagicItems, 0.50, 1, 1, 40, 100)
            }
        );

        public static readonly LootPack OldRich = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "10d10+250"),
                new LootPackEntry(false, Instruments, 1.00, 1),
                new LootPackEntry(false, OldMagicItems, 20.00, 1, 1, 60, 100),
                new LootPackEntry(false, OldMagicItems, 10.00, 1, 1, 65, 100),
                new LootPackEntry(false, OldMagicItems, 1.00, 1, 1, 70, 100)
            }
        );

        public static readonly LootPack OldFilthyRich = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "2d125+400"),
                new LootPackEntry(false, Instruments, 2.00, 1),
                new LootPackEntry(false, OldMagicItems, 33.00, 1, 1, 50, 100),
                new LootPackEntry(false, OldMagicItems, 33.00, 1, 1, 60, 100),
                new LootPackEntry(false, OldMagicItems, 20.00, 1, 1, 70, 100),
                new LootPackEntry(false, OldMagicItems, 5.00, 1, 1, 80, 100)
            }
        );

        public static readonly LootPack OldUltraRich = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "5d100+500"),
                new LootPackEntry(false, Instruments, 2.00, 1),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 40, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 40, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 50, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 50, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 60, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 60, 100)
            }
        );

        public static readonly LootPack OldSuperBoss = new(
            new[]
            {
                new LootPackEntry(true, Gold, 100.00, "5d100+500"),
                new LootPackEntry(false, Instruments, 2.00, 1),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 40, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 40, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 40, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 50, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 50, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 50, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 60, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 60, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 60, 100),
                new LootPackEntry(false, OldMagicItems, 100.00, 1, 1, 70, 100)
            }
        );

        public static LootPack Poor => Core.SE ? SePoor :
            Core.AOS ? AosPoor : OldPoor;

        public static LootPack Meager => Core.SE ? SeMeager :
            Core.AOS ? AosMeager : OldMeager;

        public static LootPack Average => Core.SE ? SeAverage :
            Core.AOS ? AosAverage : OldAverage;

        public static LootPack Rich => Core.SE ? SeRich :
            Core.AOS ? AosRich : OldRich;

        public static LootPack FilthyRich => Core.SE ? SeFilthyRich :
            Core.AOS ? AosFilthyRich : OldFilthyRich;

        public static LootPack UltraRich => Core.SE ? SeUltraRich :
            Core.AOS ? AosUltraRich : OldUltraRich;

        public static LootPack SuperBoss => Core.SE ? SeSuperBoss :
            Core.AOS ? AosSuperBoss : OldSuperBoss;

        /*
        // TODO: Uncomment once added Legacy
        public static readonly LootPackItem[] ParrotItem = new LootPackItem[]
          {
            new LootPackItem( typeof( ParrotItem ), 1 )
          };

        public static readonly LootPack Parrot = new LootPack( new LootPackEntry[]
          {
            new LootPackEntry( false, ParrotItem, 10.00, 1 )
          } );
        */
    }

    public class LootPackEntry
    {
        private readonly bool m_AtSpawnTime;

        public LootPackEntry(bool atSpawnTime, LootPackItem[] items, double chance, string quantity) : this(
            atSpawnTime,
            items,
            chance,
            new LootPackDice(quantity)
        )
        {
        }

        public LootPackEntry(bool atSpawnTime, LootPackItem[] items, double chance, int quantity) : this(
            atSpawnTime,
            items,
            chance,
            new LootPackDice(0, 0, quantity)
        )
        {
        }

        public LootPackEntry(
            bool atSpawnTime, LootPackItem[] items, double chance, string quantity, int maxProps,
            int minIntensity, int maxIntensity
        ) : this(
            atSpawnTime,
            items,
            chance,
            new LootPackDice(quantity),
            maxProps,
            minIntensity,
            maxIntensity
        )
        {
        }

        public LootPackEntry(
            bool atSpawnTime, LootPackItem[] items, double chance, int quantity, int maxProps,
            int minIntensity, int maxIntensity
        ) : this(
            atSpawnTime,
            items,
            chance,
            new LootPackDice(0, 0, quantity),
            maxProps,
            minIntensity,
            maxIntensity
        )
        {
        }

        public LootPackEntry(
            bool atSpawnTime, LootPackItem[] items, double chance, LootPackDice quantity, int maxProps = 0,
            int minIntensity = 0, int maxIntensity = 0
        )
        {
            m_AtSpawnTime = atSpawnTime;
            Items = items;
            Chance = (int)(100 * chance);
            Quantity = quantity;
            MaxProps = maxProps;
            MinIntensity = minIntensity;
            MaxIntensity = maxIntensity;
        }

        public int Chance { get; set; }

        public LootPackDice Quantity { get; set; }

        public int MaxProps { get; set; }

        public int MinIntensity { get; set; }

        public int MaxIntensity { get; set; }

        public LootPackItem[] Items { get; set; }

        private static bool IsInTokuno(Mobile m)
        {
            if (m.Region.IsPartOf("Fan Dancer's Dojo"))
            {
                return true;
            }

            if (m.Region.IsPartOf("Yomotsu Mines"))
            {
                return true;
            }

            return m.Map == Map.Tokuno;
        }

        private static bool IsMondain(Mobile m) => MondainsLegacy.IsMLRegion(m.Region);

        public Item Construct(Mobile from, int luckChance, bool spawning)
        {
            if (m_AtSpawnTime != spawning)
            {
                return null;
            }

            var totalChance = 0;

            for (var i = 0; i < Items.Length; ++i)
            {
                totalChance += Items[i].Chance;
            }

            var rnd = Utility.Random(totalChance);

            for (var i = 0; i < Items.Length; ++i)
            {
                var item = Items[i];

                if (rnd < item.Chance)
                {
                    return Mutate(from, luckChance, item.Construct(IsInTokuno(from), IsMondain(from)));
                }

                rnd -= item.Chance;
            }

            return null;
        }

        private int GetRandomOldBonus()
        {
            var rnd = Utility.RandomMinMax(MinIntensity, MaxIntensity);

            if (rnd < 50)
            {
                return 1;
            }

            rnd -= 50;

            if (rnd < 25)
            {
                return 2;
            }

            rnd -= 25;

            if (rnd < 14)
            {
                return 3;
            }

            rnd -= 14;

            if (rnd < 8)
            {
                return 4;
            }

            return 5;
        }

        public Item Mutate(Mobile from, int luckChance, Item item)
        {
            if (item != null)
            {
                if (item is BaseWeapon && Utility.Random(100) < 1)
                {
                    item.Delete();
                    item = new FireHorn();
                    return item;
                }

                if (item is BaseWeapon or BaseArmor or BaseJewel or BaseHat)
                {
                    if (Core.AOS)
                    {
                        var bonusProps = GetBonusProperties();
                        var min = MinIntensity;
                        var max = MaxIntensity;

                        if (bonusProps < MaxProps && LootPack.CheckLuck(luckChance))
                        {
                            ++bonusProps;
                        }

                        var props = 1 + bonusProps;

                        // Make sure we're not spawning items with 6 properties.
                        if (props > MaxProps)
                        {
                            props = MaxProps;
                        }

                        if (item is BaseWeapon weapon)
                        {
                            BaseRunicTool.ApplyAttributesTo(weapon, false, luckChance, props, MinIntensity, MaxIntensity);
                        }
                        else if (item is BaseArmor armor)
                        {
                            BaseRunicTool.ApplyAttributesTo(armor, false, luckChance, props, MinIntensity, MaxIntensity);
                        }
                        else if (item is BaseJewel jewel)
                        {
                            BaseRunicTool.ApplyAttributesTo(jewel, false, luckChance, props, MinIntensity, MaxIntensity);
                        }
                        else
                        {
                            BaseRunicTool.ApplyAttributesTo(
                                (BaseHat)item,
                                false,
                                luckChance,
                                props,
                                MinIntensity,
                                MaxIntensity
                            );
                        }
                    }
                    else // not aos
                    {
                        if (item is BaseWeapon weapon)
                        {
                            if (Utility.Random(100) < 80)
                            {
                                weapon.AccuracyLevel = (WeaponAccuracyLevel)GetRandomOldBonus();
                            }

                            if (Utility.Random(100) < 60)
                            {
                                weapon.DamageLevel = (WeaponDamageLevel)GetRandomOldBonus();
                            }

                            if (Utility.Random(100) < 40)
                            {
                                weapon.DurabilityLevel = (WeaponDurabilityLevel)GetRandomOldBonus();
                            }

                            if (Utility.Random(100) < 5)
                            {
                                weapon.Slayer = SlayerName.Silver;
                            }

                            if (from != null && weapon.AccuracyLevel == 0 && weapon.DamageLevel == 0 &&
                                weapon.DurabilityLevel == 0 && weapon.Slayer == SlayerName.None && Utility.Random(100) < 5)
                            {
                                weapon.Slayer = SlayerGroup.GetLootSlayerType(from.GetType());
                            }
                        }
                        else if (item is BaseArmor armor)
                        {
                            if (Utility.Random(100) < 80)
                            {
                                armor.ProtectionLevel = (ArmorProtectionLevel)GetRandomOldBonus();
                            }

                            if (Utility.Random(100) < 40)
                            {
                                armor.Durability = (ArmorDurabilityLevel)GetRandomOldBonus();
                            }
                        }
                    }
                }
                else if (item is BaseInstrument instr)
                {
                    SlayerName slayer;

                    if (Core.AOS)
                    {
                        slayer = BaseRunicTool.GetRandomSlayer();
                    }
                    else
                    {
                        slayer = SlayerGroup.GetLootSlayerType(from.GetType());
                    }

                    if (slayer == SlayerName.None)
                    {
                        instr.Delete();
                        return null;
                    }

                    instr.Quality = InstrumentQuality.Regular;
                    instr.Slayer = slayer;
                }

                if (item.Stackable)
                {
                    item.Amount = Quantity.Roll();
                }
            }

            return item;
        }

        public int GetBonusProperties()
        {
            int p0 = 0, p1 = 0, p2 = 0, p3 = 0, p4 = 0, p5 = 0;

            switch (MaxProps)
            {
                case 1:
                    p0 = 3;
                    p1 = 1;
                    break;
                case 2:
                    p0 = 6;
                    p1 = 3;
                    p2 = 1;
                    break;
                case 3:
                    p0 = 10;
                    p1 = 6;
                    p2 = 3;
                    p3 = 1;
                    break;
                case 4:
                    p0 = 16;
                    p1 = 12;
                    p2 = 6;
                    p3 = 5;
                    p4 = 1;
                    break;
                case 5:
                    p0 = 30;
                    p1 = 25;
                    p2 = 20;
                    p3 = 15;
                    p4 = 9;
                    p5 = 1;
                    break;
            }

            var pc = p0 + p1 + p2 + p3 + p4 + p5;

            var rnd = Utility.Random(pc);

            if (rnd < p5)
            {
                return 5;
            }

            rnd -= p5;

            if (rnd < p4)
            {
                return 4;
            }

            rnd -= p4;

            if (rnd < p3)
            {
                return 3;
            }

            rnd -= p3;

            if (rnd < p2)
            {
                return 2;
            }

            return rnd - p2 < p1 ? 1 : 0;
        }
    }

    public class LootPackItem
    {
        private static readonly Type[] m_BlankTypes = { typeof(BlankScroll) };

        private static readonly Type[][] m_NecroTypes =
        {
            new[] // low
            {
                typeof(AnimateDeadScroll), typeof(BloodOathScroll), typeof(CorpseSkinScroll), typeof(CurseWeaponScroll),
                typeof(EvilOmenScroll), typeof(HorrificBeastScroll), typeof(MindRotScroll), typeof(PainSpikeScroll),
                typeof(SummonFamiliarScroll), typeof(WraithFormScroll)
            },
            new[] // med
            {
                typeof(LichFormScroll), typeof(PoisonStrikeScroll), typeof(StrangleScroll), typeof(WitherScroll)
            },

            Core.SE
                ? new[] // high
                {
                    typeof(VengefulSpiritScroll), typeof(VampiricEmbraceScroll), typeof(ExorcismScroll)
                }
                : new[] // high
                {
                    typeof(VengefulSpiritScroll), typeof(VampiricEmbraceScroll)
                }
        };

        public LootPackItem(Type type, int chance)
        {
            Type = type;
            Chance = chance;
        }

        public Type Type { get; set; }

        public int Chance { get; set; }

        public static Item RandomScroll(int index, int minCircle, int maxCircle)
        {
            --minCircle;
            --maxCircle;

            var scrollCount = (maxCircle - minCircle + 1) * 8;

            if (index == 0)
            {
                scrollCount += m_BlankTypes.Length;
            }

            if (Core.AOS)
            {
                scrollCount += m_NecroTypes[index].Length;
            }

            var rnd = Utility.Random(scrollCount);

            if (index == 0 && rnd < m_BlankTypes.Length)
            {
                return Loot.Construct(m_BlankTypes);
            }

            if (index == 0)
            {
                rnd -= m_BlankTypes.Length;
            }

            if (Core.AOS && rnd < m_NecroTypes.Length)
            {
                return Loot.Construct(m_NecroTypes[index]);
            }

            return Loot.RandomScroll(minCircle * 8, maxCircle * 8 + 7, SpellbookType.Regular);
        }

        public Item Construct(bool inTokuno, bool isMondain)
        {
            try
            {
                Item item;

                if (Type == typeof(BaseRanged))
                {
                    item = Loot.RandomRangedWeapon(inTokuno, isMondain);
                }
                else if (Type == typeof(BaseWeapon))
                {
                    item = Loot.RandomWeapon(inTokuno, isMondain);
                }
                else if (Type == typeof(BaseArmor))
                {
                    item = Loot.RandomArmorOrHat(inTokuno, isMondain);
                }
                else if (Type == typeof(BaseShield))
                {
                    item = Loot.RandomShield();
                }
                else if (Type == typeof(BaseJewel))
                {
                    item = Core.AOS ? Loot.RandomJewelry() : Loot.RandomArmorOrShieldOrWeapon();
                }
                else if (Type == typeof(BaseInstrument))
                {
                    item = Loot.RandomInstrument();
                }
                else if (Type == typeof(Amber)) // gem
                {
                    item = Loot.RandomGem();
                }
                else if (Type == typeof(ClumsyScroll)) // low scroll
                {
                    item = RandomScroll(0, 1, 3);
                }
                else if (Type == typeof(ArchCureScroll)) // med scroll
                {
                    item = RandomScroll(1, 4, 7);
                }
                else if (Type == typeof(SummonAirElementalScroll)) // high scroll
                {
                    item = RandomScroll(2, 8, 8);
                }
                else
                {
                    item = Type.CreateInstance<Item>();
                }

                return item;
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }

    public class LootPackDice
    {
        public LootPackDice(string str)
        {
            var start = 0;
            var index = str.IndexOf('d', start);

            if (index < start)
            {
                return;
            }

            Count = Utility.ToInt32(str.AsSpan(start, index));

            start = index + 1;
            index = str.IndexOf('+', start);

            var negative = index < start;

            if (negative)
            {
                index = str.IndexOf('-', start);
            }

            if (index < start)
            {
                index = str.Length;
            }

            Sides = Utility.ToInt32(str.AsSpan(start, index - start));

            if (index == str.Length)
            {
                return;
            }

            start = index + 1;
            index = str.Length;

            Bonus = Utility.ToInt32(str.AsSpan(start, index - start));

            if (negative)
            {
                Bonus *= -1;
            }
        }

        public LootPackDice(int count, int sides, int bonus)
        {
            Count = count;
            Sides = sides;
            Bonus = bonus;
        }

        public int Count { get; set; }

        public int Sides { get; set; }

        public int Bonus { get; set; }

        public int Roll()
        {
            var v = Bonus;

            for (var i = 0; i < Count; ++i)
            {
                v += Utility.Random(1, Sides);
            }

            return v;
        }
    }
}
