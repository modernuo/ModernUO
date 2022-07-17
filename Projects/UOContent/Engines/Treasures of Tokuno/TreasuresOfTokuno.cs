using System;
using System.Collections.Generic;
using System.Linq;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.Utilities;

namespace Server.Misc
{
    public enum TreasuresOfTokunoEra
    {
        None,
        ToTOne,
        ToTTwo,
        ToTThree
    }

    public static class TreasuresOfTokuno
    {
        public const int ItemsPerReward = 10;

        private static readonly Type[][] m_LesserArtifacts =
        {
            // ToT One Rewards
            new[]
            {
                typeof(AncientFarmersKasa), typeof(AncientSamuraiDo), typeof(ArmsOfTacticalExcellence),
                typeof(BlackLotusHood),
                typeof(DaimyosHelm), typeof(DemonForks), typeof(DragonNunchaku), typeof(Exiler), typeof(GlovesOfTheSun),
                typeof(HanzosBow), typeof(LegsOfStability), typeof(PeasantsBokuto), typeof(PilferedDancerFans),
                typeof(TheDestroyer),
                typeof(TomeOfEnlightenment), typeof(AncientUrn), typeof(HonorableSwords), typeof(PigmentsOfTokuno),
                typeof(FluteOfRenewal), typeof(ChestOfHeirlooms)
            },
            // ToT Two Rewards
            new[]
            {
                typeof(MetalPigmentsOfTokuno), typeof(AncientFarmersKasa), typeof(AncientSamuraiDo),
                typeof(ArmsOfTacticalExcellence),
                typeof(MetalPigmentsOfTokuno), typeof(BlackLotusHood), typeof(DaimyosHelm), typeof(DemonForks),
                typeof(MetalPigmentsOfTokuno), typeof(DragonNunchaku), typeof(Exiler), typeof(GlovesOfTheSun),
                typeof(HanzosBow),
                typeof(MetalPigmentsOfTokuno), typeof(LegsOfStability), typeof(PeasantsBokuto), typeof(PilferedDancerFans),
                typeof(TheDestroyer),
                typeof(MetalPigmentsOfTokuno), typeof(TomeOfEnlightenment), typeof(AncientUrn), typeof(HonorableSwords),
                typeof(MetalPigmentsOfTokuno), typeof(FluteOfRenewal), typeof(ChestOfHeirlooms)
            },
            // ToT Three Rewards
            new[]
            {
                typeof(LesserPigmentsOfTokuno), typeof(AncientFarmersKasa), typeof(AncientSamuraiDo),
                typeof(ArmsOfTacticalExcellence),
                typeof(LesserPigmentsOfTokuno), typeof(BlackLotusHood), typeof(DaimyosHelm), typeof(HanzosBow),
                typeof(LesserPigmentsOfTokuno), typeof(DemonForks), typeof(DragonNunchaku), typeof(Exiler),
                typeof(GlovesOfTheSun),
                typeof(LesserPigmentsOfTokuno), typeof(LegsOfStability), typeof(PeasantsBokuto), typeof(PilferedDancerFans),
                typeof(TheDestroyer),
                typeof(LesserPigmentsOfTokuno), typeof(TomeOfEnlightenment), typeof(AncientUrn), typeof(HonorableSwords),
                typeof(FluteOfRenewal),
                typeof(LesserPigmentsOfTokuno), typeof(LeurociansMempoOfFortune), typeof(ChestOfHeirlooms)
            }
        };

        private static Type[][] m_GreaterArtifacts;

        public static Type[] LesserArtifactsTotal { get; } =
        {
            typeof(AncientFarmersKasa), typeof(AncientSamuraiDo), typeof(ArmsOfTacticalExcellence), typeof(BlackLotusHood),
            typeof(DaimyosHelm), typeof(DemonForks), typeof(DragonNunchaku), typeof(Exiler), typeof(GlovesOfTheSun),
            typeof(HanzosBow), typeof(LegsOfStability), typeof(PeasantsBokuto), typeof(PilferedDancerFans),
            typeof(TheDestroyer),
            typeof(TomeOfEnlightenment), typeof(AncientUrn), typeof(HonorableSwords), typeof(PigmentsOfTokuno),
            typeof(FluteOfRenewal),
            typeof(LeurociansMempoOfFortune), typeof(LesserPigmentsOfTokuno), typeof(MetalPigmentsOfTokuno),
            typeof(ChestOfHeirlooms)
        };

        public static Type[] TokunoDyable { get; } =
        {
            typeof(DupresShield), typeof(CrimsonCincture), typeof(OssianGrimoire), typeof(QuiverOfInfinity),
            typeof(BaseFormTalisman), typeof(BaseWand), typeof(JesterHatofChuckles)
        };

        public static TreasuresOfTokunoEra DropEra { get; set; } = TreasuresOfTokunoEra.None;

        public static TreasuresOfTokunoEra RewardEra { get; set; } = TreasuresOfTokunoEra.ToTOne;

        public static Type[] LesserArtifacts => m_LesserArtifacts[(int)RewardEra - 1];

        public static Type[] GreaterArtifacts
        {
            get
            {
                if (m_GreaterArtifacts == null)
                {
                    m_GreaterArtifacts = new Type[ToTRedeemGump.NormalRewards.Length][];

                    for (var i = 0; i < m_GreaterArtifacts.Length; i++)
                    {
                        m_GreaterArtifacts[i] = new Type[ToTRedeemGump.NormalRewards[i].Length];

                        for (var j = 0; j < m_GreaterArtifacts[i].Length; j++)
                        {
                            m_GreaterArtifacts[i][j] = ToTRedeemGump.NormalRewards[i][j].Type;
                        }
                    }
                }

                return m_GreaterArtifacts[(int)RewardEra - 1];
            }
        }

        private static bool CheckLocation(Mobile m)
        {
            var r = m.Region;

            if (r.IsPartOf<HouseRegion>() || BaseBoat.FindBoatAt(m.Location, m.Map) != null)
            {
                return false;
            }

            // TODO: a CanReach of something check as opposed to above?
            if (r.IsPartOf("Yomotsu Mines") || r.IsPartOf("Fan Dancer's Dojo"))
            {
                return true;
            }

            return m.Map == Map.Tokuno;
        }

        public static void HandleKill(Mobile victim, Mobile killer)
        {
            if (DropEra == TreasuresOfTokunoEra.None || killer is not PlayerMobile pm || victim is not BaseCreature bc ||
                !CheckLocation(bc) || !CheckLocation(pm) || !killer.InRange(victim, 18))
            {
                return;
            }

            if (bc.Controlled || bc.Owners.Count > 0 || bc.Fame <= 0)
            {
                return;
            }

            // 25000 for 1/100 chance, 10 hyrus
            // 1500, 1/1000 chance, 20 lizard men for that chance.

            pm.ToTTotalMonsterFame += (int)(bc.Fame * (1 + Math.Sqrt(pm.Luck) / 100));

            // This is the Exponential regression with only 2 datapoints.
            // A log. func would also work, but it didn't make as much sense.
            // This function isn't OSI exact being that I don't know OSI's func they used ;p
            var x = pm.ToTTotalMonsterFame;

            // const double A = 8.63316841 * Math.Pow( 10, -4 );
            const double A = 0.000863316841;
            // const double B = 4.25531915 * Math.Pow( 10, -6 );
            const double B = 0.00000425531915;

            var chance = A * Math.Pow(10, B * x);

            if (chance <= Utility.RandomDouble())
            {
                return;
            }

            Item i;

            try
            {
                i = m_LesserArtifacts[(int)DropEra - 1].RandomElement().CreateInstance<Item>();
            }
            catch
            {
                return;
            }

            // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
            pm.SendLocalizedMessage(1062317);

            if (!pm.PlaceInBackpack(i))
            {
                if (pm.BankBox?.TryDropItem(killer, i, false) == true)
                {
                    pm.SendLocalizedMessage(1079730); // The item has been placed into your bank box.
                }
                else
                {
                    // You find an artifact, but your backpack and bank are too full to hold it.
                    pm.SendLocalizedMessage(1072523);
                    i.MoveToWorld(pm.Location, pm.Map);
                }
            }

            pm.ToTTotalMonsterFame = 0;
        }
    }
}

namespace Server.Mobiles
{
    public class IharaSoko : BaseVendor
    {
        protected List<SBInfo> m_SBInfos = new();

        [Constructible]
        public IharaSoko() : base("the Imperial Minister of Trade")
        {
            Female = false;
            Body = 0x190;
            Hue = 0x8403;
        }

        public IharaSoko(Serial serial) : base(serial)
        {
        }

        public override bool IsActiveVendor => false;
        public override bool IsInvulnerable => true;
        public override bool DisallowAllMoves => true;
        public override bool ClickTitle => true;
        public override bool CanTeach => false;
        public override string DefaultName => "Ihara Soko";
        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
        }

        public override void InitOutfit()
        {
            AddItem(new Waraji(0x711));
            AddItem(new Backpack());
            AddItem(new Kamishimo(0x483));

            Item item = new LightPlateJingasa();
            item.Hue = 0x711;

            AddItem(item);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public override bool CanBeDamaged() => false;

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m.Alive && m is PlayerMobile pm)
            {
                if (pm.Alive && (Z - pm.Z).Abs() < 16 && InRange(m, 3) && !InRange(oldLocation, 3))
                {
                    if (pm.ToTItemsTurnedIn >= TreasuresOfTokuno.ItemsPerReward)
                    {
                        // Congratulations! You have turned in enough minor treasures to earn a greater reward.
                        SayTo(pm, 1070980);

                        pm.CloseGump<ToTTurnInGump>(); // Sanity

                        if (!pm.HasGump<ToTRedeemGump>())
                        {
                            pm.SendGump(new ToTRedeemGump(this, false));
                        }
                    }
                    else
                    {
                        if (pm.ToTItemsTurnedIn == 0)
                        {
                            // Bring me 10 of the lost treasures of Tokuno and I will reward you with a valuable item.
                            SayTo(pm, 1071013);
                        }
                        else
                        {
                            SayTo(
                                pm,
                                1070981, // You have turned in ~1_COUNT~ minor artifacts. Turn in ~2_NUM~ to receive a reward.
                                $"{pm.ToTItemsTurnedIn}\t{TreasuresOfTokuno.ItemsPerReward}"
                            );
                        }

                        var buttons = ToTTurnInGump.FindRedeemableItems(pm);

                        if (buttons.Count > 0 && !pm.HasGump<ToTTurnInGump>())
                        {
                            pm.SendGump(new ToTTurnInGump(this, buttons));
                        }
                    }
                }

                var leaveRange = 7;

                if (!InRange(m, leaveRange) && InRange(oldLocation, leaveRange))
                {
                    pm.CloseGump<ToTRedeemGump>();
                    pm.CloseGump<ToTTurnInGump>();
                }
            }
        }

        public override void TurnToTokuno()
        {
        }
    }
}

namespace Server.Gumps
{
    public class ItemTileButtonInfo : ImageTileButtonInfo
    {
        public ItemTileButtonInfo(Item i) : base(
            i.ItemID,
            i.Hue,
            i.Name is not { Length: > 0 } ? i.LabelNumber : i.Name
        ) => Item = i;

        public Item Item { get; set; }
    }

    public class ToTTurnInGump : BaseImageTileButtonsGump
    {
        private readonly Mobile m_Collector;

        // Click a minor artifact to give it to Ihara Soko.
        public ToTTurnInGump(Mobile collector, List<ImageTileButtonInfo> buttons)
            : base(1071012, buttons) => m_Collector = collector;

        public static List<ImageTileButtonInfo> FindRedeemableItems(Mobile m)
        {
            var pack = m.Backpack;
            if (pack == null)
            {
                return new List<ImageTileButtonInfo>();
            }

            var buttons = new List<ImageTileButtonInfo>();

            var items = pack.FindItemsByType(TreasuresOfTokuno.LesserArtifactsTotal);

            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item is ChestOfHeirlooms heirlooms && (!heirlooms.Locked || heirlooms.TrapLevel != 10))
                {
                    continue;
                }

                if (item is PigmentsOfTokuno tokuno && tokuno.Type != PigmentType.None)
                {
                    continue;
                }

                buttons.Add(new ItemTileButtonInfo(item));
            }

            return buttons;
        }

        public override void HandleButtonResponse(NetState sender, int adjustedButton, ImageTileButtonInfo buttonInfo)
        {
            var pm = sender.Mobile as PlayerMobile;

            var item = ((ItemTileButtonInfo)buttonInfo).Item;

            if (!(pm != null && item.IsChildOf(pm.Backpack) && pm.InRange(m_Collector.Location, 7)))
            {
                return;
            }

            item.Delete();

            if (++pm.ToTItemsTurnedIn >= TreasuresOfTokuno.ItemsPerReward)
            {
                // Congratulations! You have turned in enough minor treasures to earn a greater reward.
                m_Collector.SayTo(pm, 1070980);

                pm.CloseGump<ToTTurnInGump>(); // Sanity

                if (!pm.HasGump<ToTRedeemGump>())
                {
                    pm.SendGump(new ToTRedeemGump(m_Collector, false));
                }
            }
            else
            {
                m_Collector.SayTo(
                    pm,
                    1070981, // You have turned in ~1_COUNT~ minor artifacts. Turn in ~2_NUM~ to receive a reward.
                    $"{pm.ToTItemsTurnedIn}\t{TreasuresOfTokuno.ItemsPerReward}"
                );

                var buttons = FindRedeemableItems(pm);

                pm.CloseGump<ToTTurnInGump>(); // Sanity

                if (buttons.Count > 0)
                {
                    pm.SendGump(new ToTTurnInGump(m_Collector, buttons));
                }
            }
        }

        public override void HandleCancel(NetState sender)
        {
            if (sender.Mobile is not PlayerMobile pm || !pm.InRange(m_Collector.Location, 7))
            {
                return;
            }

            if (pm.ToTItemsTurnedIn == 0)
            {
                // Bring me 10 of the lost treasures of Tokuno and I will reward you with a valuable item.
                m_Collector.SayTo(pm, 1071013);
            }
            // This case should ALWAYS be true with this gump, just a sanity check
            else if (pm.ToTItemsTurnedIn < TreasuresOfTokuno.ItemsPerReward)
            {
                m_Collector.SayTo(
                    pm,
                    1070981, // You have turned in ~1_COUNT~ minor artifacts. Turn in ~2_NUM~ to receive a reward.
                    $"{pm.ToTItemsTurnedIn}\t{TreasuresOfTokuno.ItemsPerReward}"
                );
            }
            else
            {
                m_Collector.SayTo(pm, 1070982); // When you wish to choose your reward, you have but to approach me again.
            }
        }
    }

    public class ToTRedeemGump : BaseImageTileButtonsGump
    {
        private readonly Mobile m_Collector;

        public ToTRedeemGump(Mobile collector, bool pigments) : base(
            pigments ? 1070986 : 1070985,
            pigments
                ? PigmentRewards[(int)TreasuresOfTokuno.RewardEra - 1].ToArray<ImageTileButtonInfo>()
                : NormalRewards[(int)TreasuresOfTokuno.RewardEra - 1].ToArray<ImageTileButtonInfo>()
        ) =>
            m_Collector = collector;

        public static TypeTileButtonInfo[][] NormalRewards { get; } =
        {
            // ToT One Rewards
            new[]
            {
                new TypeTileButtonInfo(typeof(SwordsOfProsperity), 0x27A9, 1070963, 1071002),
                new TypeTileButtonInfo(typeof(SwordOfTheStampede), 0x27A2, 1070964, 1070978),
                new TypeTileButtonInfo(typeof(WindsEdge), 0x27A3, 1070965, 1071003),
                new TypeTileButtonInfo(typeof(DarkenedSky), 0x27AD, 1070966, 1071004),
                new TypeTileButtonInfo(typeof(TheHorselord), 0x27A5, 1070967, 1071005),
                new TypeTileButtonInfo(typeof(RuneBeetleCarapace), 0x277D, 1070968, 1071006),
                new TypeTileButtonInfo(typeof(KasaOfTheRajin), 0x2798, 1070969, 1071007),
                new TypeTileButtonInfo(typeof(Stormgrip), 0x2792, 1070970, 1071008),
                new TypeTileButtonInfo(typeof(TomeOfLostKnowledge), 0x0EFA, 0x530, 1070971, 1071009),
                new TypeTileButtonInfo(typeof(PigmentsOfTokuno), 0x0EFF, 1070933, 1071011)
            },
            // ToT Two Rewards
            new[]
            {
                new TypeTileButtonInfo(typeof(SwordsOfProsperity), 0x27A9, 1070963, 1071002),
                new TypeTileButtonInfo(typeof(SwordOfTheStampede), 0x27A2, 1070964, 1070978),
                new TypeTileButtonInfo(typeof(WindsEdge), 0x27A3, 1070965, 1071003),
                new TypeTileButtonInfo(typeof(DarkenedSky), 0x27AD, 1070966, 1071004),
                new TypeTileButtonInfo(typeof(TheHorselord), 0x27A5, 1070967, 1071005),
                new TypeTileButtonInfo(typeof(RuneBeetleCarapace), 0x277D, 1070968, 1071006),
                new TypeTileButtonInfo(typeof(KasaOfTheRajin), 0x2798, 1070969, 1071007),
                new TypeTileButtonInfo(typeof(Stormgrip), 0x2792, 1070970, 1071008),
                new TypeTileButtonInfo(typeof(TomeOfLostKnowledge), 0x0EFA, 0x530, 1070971, 1071009),
                new TypeTileButtonInfo(typeof(PigmentsOfTokuno), 0x0EFF, 1070933, 1071011)
            },
            // ToT Three Rewards
            new[]
            {
                new TypeTileButtonInfo(typeof(SwordsOfProsperity), 0x27A9, 1070963, 1071002),
                new TypeTileButtonInfo(typeof(SwordOfTheStampede), 0x27A2, 1070964, 1070978),
                new TypeTileButtonInfo(typeof(WindsEdge), 0x27A3, 1070965, 1071003),
                new TypeTileButtonInfo(typeof(DarkenedSky), 0x27AD, 1070966, 1071004),
                new TypeTileButtonInfo(typeof(TheHorselord), 0x27A5, 1070967, 1071005),
                new TypeTileButtonInfo(typeof(RuneBeetleCarapace), 0x277D, 1070968, 1071006),
                new TypeTileButtonInfo(typeof(KasaOfTheRajin), 0x2798, 1070969, 1071007),
                new TypeTileButtonInfo(typeof(Stormgrip), 0x2792, 1070970, 1071008),
                new TypeTileButtonInfo(typeof(TomeOfLostKnowledge), 0x0EFA, 0x530, 1070971, 1071009)
            }
        };

        public static PigmentsTileButtonInfo[][] PigmentRewards { get; } =
        {
            // ToT One Pigment Rewards
            new[]
            {
                new PigmentsTileButtonInfo(PigmentType.ParagonGold),
                new PigmentsTileButtonInfo(PigmentType.VioletCouragePurple),
                new PigmentsTileButtonInfo(PigmentType.InvulnerabilityBlue),
                new PigmentsTileButtonInfo(PigmentType.LunaWhite),
                new PigmentsTileButtonInfo(PigmentType.DryadGreen),
                new PigmentsTileButtonInfo(PigmentType.ShadowDancerBlack),
                new PigmentsTileButtonInfo(PigmentType.BerserkerRed),
                new PigmentsTileButtonInfo(PigmentType.NoxGreen),
                new PigmentsTileButtonInfo(PigmentType.RumRed),
                new PigmentsTileButtonInfo(PigmentType.FireOrange)
            },
            // ToT Two Pigment Rewards
            new[]
            {
                new PigmentsTileButtonInfo(PigmentType.FadedCoal),
                new PigmentsTileButtonInfo(PigmentType.Coal),
                new PigmentsTileButtonInfo(PigmentType.FadedGold),
                new PigmentsTileButtonInfo(PigmentType.StormBronze),
                new PigmentsTileButtonInfo(PigmentType.Rose),
                new PigmentsTileButtonInfo(PigmentType.MidnightCoal),
                new PigmentsTileButtonInfo(PigmentType.FadedBronze),
                new PigmentsTileButtonInfo(PigmentType.FadedRose),
                new PigmentsTileButtonInfo(PigmentType.DeepRose)
            },
            // ToT Three Pigment Rewards
            new[]
            {
                new PigmentsTileButtonInfo(PigmentType.ParagonGold),
                new PigmentsTileButtonInfo(PigmentType.VioletCouragePurple),
                new PigmentsTileButtonInfo(PigmentType.InvulnerabilityBlue),
                new PigmentsTileButtonInfo(PigmentType.LunaWhite),
                new PigmentsTileButtonInfo(PigmentType.DryadGreen),
                new PigmentsTileButtonInfo(PigmentType.ShadowDancerBlack),
                new PigmentsTileButtonInfo(PigmentType.BerserkerRed),
                new PigmentsTileButtonInfo(PigmentType.NoxGreen),
                new PigmentsTileButtonInfo(PigmentType.RumRed),
                new PigmentsTileButtonInfo(PigmentType.FireOrange)
            }
        };

        public override void HandleButtonResponse(NetState sender, int adjustedButton, ImageTileButtonInfo buttonInfo)
        {
            if (sender.Mobile is not PlayerMobile pm || !pm.InRange(m_Collector.Location, 7) ||
                !(pm.ToTItemsTurnedIn >= TreasuresOfTokuno.ItemsPerReward))
            {
                return;
            }

            Item item = null;

            if (buttonInfo is PigmentsTileButtonInfo p)
            {
                item = new PigmentsOfTokuno(p.Pigment);
            }
            else
            {
                var t = (TypeTileButtonInfo)buttonInfo;

                if (t.Type == typeof(PigmentsOfTokuno)) // Special case of course.
                {
                    pm.CloseGump<ToTTurnInGump>(); // Sanity
                    pm.CloseGump<ToTRedeemGump>();

                    pm.SendGump(new ToTRedeemGump(m_Collector, true));

                    return;
                }

                try
                {
                    item = t.Type.CreateInstance<Item>();
                }
                catch
                {
                    // ignored
                }
            }

            if (item == null)
            {
                return; // Sanity
            }

            if (pm.AddToBackpack(item))
            {
                pm.ToTItemsTurnedIn -= TreasuresOfTokuno.ItemsPerReward;
                m_Collector.SayTo(
                    pm,
                    1070984, // You have earned the gratitude of the Empire. I have placed the ~1_OBJTYPE~ in your backpack.
                    item.Name?.Length > 0 ? item.Name : $"#{item.LabelNumber}"
                );
            }
            else
            {
                item.Delete();
                m_Collector.SayTo(pm, 500722);  // You don't have enough room in your backpack!
                m_Collector.SayTo(pm, 1070982); // When you wish to choose your reward, you have but to approach me again.
            }
        }

        public override void HandleCancel(NetState sender)
        {
            if (sender.Mobile is not PlayerMobile pm || !pm.InRange(m_Collector.Location, 7))
            {
                return;
            }

            if (pm.ToTItemsTurnedIn == 0)
            {
                // Bring me 10 of the lost treasures of Tokuno and I will reward you with a valuable item.
                m_Collector.SayTo(pm, 1071013);
            }
            // This and above case should ALWAYS be FALSE with this gump, just a sanity check
            else if (pm.ToTItemsTurnedIn < TreasuresOfTokuno.ItemsPerReward)
            {
                m_Collector.SayTo(
                    pm,
                    1070981, // You have turned in ~1_COUNT~ minor artifacts. Turn in ~2_NUM~ to receive a reward.
                    $"{pm.ToTItemsTurnedIn}\t{TreasuresOfTokuno.ItemsPerReward}"
                );
            }
            else
            {
                m_Collector.SayTo(pm, 1070982); // When you wish to choose your reward, you have but to approach me again.
            }
        }

        public class TypeTileButtonInfo : ImageTileButtonInfo
        {
            public TypeTileButtonInfo(Type type, int itemID, TextDefinition label, int localizedToolTip = -1) : this(
                type,
                itemID,
                0,
                label,
                localizedToolTip
            )
            {
            }

            public TypeTileButtonInfo(
                Type type, int itemID, int hue, TextDefinition label, int localizedToolTip = -1
            ) : base(
                itemID,
                hue,
                label,
                localizedToolTip
            ) =>
                Type = type;

            public Type Type { get; }
        }

        public class PigmentsTileButtonInfo : ImageTileButtonInfo
        {
            public PigmentsTileButtonInfo(PigmentType p) : base(
                0xEFF,
                PigmentsOfTokuno.GetInfo(p)[0],
                PigmentsOfTokuno.GetInfo(p)[1]
            ) =>
                Pigment = p;

            public PigmentType Pigment { get; set; }
        }
    }
}

/* Notes

Pigments of tokuno do NOT check for if item is already hued 0;  APPARENTLY he still accepts it if it's < 10 charges.

Chest of Heirlooms don't show if unlocked.

Chest of heirlooms, locked, HARD to pick at 100 lock picking but not impossible.  had 95 health to 0, cause it's trapped >< (explosion i think)
*/
