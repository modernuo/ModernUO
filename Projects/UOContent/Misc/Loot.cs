using System;
using System.Runtime.CompilerServices;
using Server.Items;

namespace Server
{
    public static class Loot
    {
        public static Type[] MLWeaponTypes { get; } =
        {
            typeof(AssassinSpike), typeof(DiamondMace), typeof(ElvenMachete),
            typeof(ElvenSpellblade), typeof(Leafblade), typeof(OrnateAxe),
            typeof(RadiantScimitar), typeof(RuneBlade), typeof(WarCleaver),
            typeof(WildStaff)
        };

        public static Type[] MLRangedWeaponTypes { get; } =
        {
            typeof(ElvenCompositeLongbow), typeof(MagicalShortbow)
        };

        public static Type[] MLArmorTypes { get; } =
        {
            typeof(Circlet), typeof(GemmedCirclet), typeof(LeafTonlet),
            typeof(RavenHelm), typeof(RoyalCirclet), typeof(VultureHelm),
            typeof(WingedHelm), typeof(LeafArms), typeof(LeafChest),
            typeof(LeafGloves), typeof(LeafGorget), typeof(LeafLegs),
            typeof(WoodlandArms), typeof(WoodlandChest), typeof(WoodlandGloves),
            typeof(WoodlandGorget), typeof(WoodlandLegs), typeof(HideChest),
            typeof(HideGloves), typeof(HideGorget), typeof(HidePants),
            typeof(HidePauldrons)
        };

        public static Type[] MLClothingTypes { get; } =
        {
            typeof(MaleElvenRobe), typeof(FemaleElvenRobe), typeof(ElvenPants),
            typeof(ElvenShirt), typeof(ElvenDarkShirt), typeof(ElvenBoots),
            typeof(VultureHelm), typeof(WoodlandBelt)
        };

        public static Type[] SEWeaponTypes { get; } =
        {
            typeof(Bokuto), typeof(Daisho), typeof(Kama),
            typeof(Lajatang), typeof(NoDachi), typeof(Nunchaku),
            typeof(Sai), typeof(Tekagi), typeof(Tessen),
            typeof(Tetsubo), typeof(Wakizashi)
        };

        public static Type[] AosWeaponTypes { get; } =
        {
            typeof(Scythe), typeof(BoneHarvester), typeof(Scepter),
            typeof(BladedStaff), typeof(Pike), typeof(DoubleBladedStaff),
            typeof(Lance), typeof(CrescentBlade)
        };

        public static Type[] WeaponTypes { get; } =
        {
            typeof(Axe), typeof(BattleAxe), typeof(DoubleAxe),
            typeof(ExecutionersAxe), typeof(Hatchet), typeof(LargeBattleAxe),
            typeof(TwoHandedAxe), typeof(WarAxe), typeof(Club),
            typeof(Mace), typeof(Maul), typeof(WarHammer),
            typeof(WarMace), typeof(Bardiche), typeof(Halberd),
            typeof(Spear), typeof(ShortSpear), typeof(Pitchfork),
            typeof(WarFork), typeof(BlackStaff), typeof(GnarledStaff),
            typeof(QuarterStaff), typeof(Broadsword), typeof(Cutlass),
            typeof(Katana), typeof(Kryss), typeof(Longsword),
            typeof(Scimitar), typeof(VikingSword), typeof(Pickaxe),
            typeof(HammerPick), typeof(ButcherKnife), typeof(Cleaver),
            typeof(Dagger), typeof(SkinningKnife), typeof(ShepherdsCrook)
        };

        public static Type[] SERangedWeaponTypes { get; } =
        {
            typeof(Yumi)
        };

        public static Type[] AosRangedWeaponTypes { get; } =
        {
            typeof(CompositeBow), typeof(RepeatingCrossbow)
        };

        public static Type[] RangedWeaponTypes { get; } =
        {
            typeof(Bow), typeof(Crossbow), typeof(HeavyCrossbow)
        };

        public static Type[] SEArmorTypes { get; } =
        {
            typeof(ChainHatsuburi), typeof(LeatherDo), typeof(LeatherHaidate),
            typeof(LeatherHiroSode), typeof(LeatherJingasa), typeof(LeatherMempo),
            typeof(LeatherNinjaHood), typeof(LeatherNinjaJacket), typeof(LeatherNinjaMitts),
            typeof(LeatherNinjaPants), typeof(LeatherSuneate), typeof(DecorativePlateKabuto),
            typeof(HeavyPlateJingasa), typeof(LightPlateJingasa), typeof(PlateBattleKabuto),
            typeof(PlateDo), typeof(PlateHaidate), typeof(PlateHatsuburi),
            typeof(PlateHiroSode), typeof(PlateMempo), typeof(PlateSuneate),
            typeof(SmallPlateJingasa), typeof(StandardPlateKabuto), typeof(StuddedDo),
            typeof(StuddedHaidate), typeof(StuddedHiroSode), typeof(StuddedMempo),
            typeof(StuddedSuneate)
        };

        public static Type[] ArmorTypes { get; } =
        {
            typeof(BoneArms), typeof(BoneChest), typeof(BoneGloves),
            typeof(BoneLegs), typeof(BoneHelm), typeof(ChainChest),
            typeof(ChainLegs), typeof(ChainCoif), typeof(Bascinet),
            typeof(CloseHelm), typeof(Helmet), typeof(NorseHelm),
            typeof(OrcHelm), typeof(FemaleLeatherChest), typeof(LeatherArms),
            typeof(LeatherBustierArms), typeof(LeatherChest), typeof(LeatherGloves),
            typeof(LeatherGorget), typeof(LeatherLegs), typeof(LeatherShorts),
            typeof(LeatherSkirt), typeof(LeatherCap), typeof(FemalePlateChest),
            typeof(PlateArms), typeof(PlateChest), typeof(PlateGloves),
            typeof(PlateGorget), typeof(PlateHelm), typeof(PlateLegs),
            typeof(RingmailArms), typeof(RingmailChest), typeof(RingmailGloves),
            typeof(RingmailLegs), typeof(FemaleStuddedChest), typeof(StuddedArms),
            typeof(StuddedBustierArms), typeof(StuddedChest), typeof(StuddedGloves),
            typeof(StuddedGorget), typeof(StuddedLegs)
        };

        public static Type[] AosShieldTypes { get; } =
        {
            typeof(ChaosShield), typeof(OrderShield)
        };

        public static Type[] ShieldTypes { get; } =
        {
            typeof(BronzeShield), typeof(Buckler), typeof(HeaterShield),
            typeof(MetalShield), typeof(MetalKiteShield), typeof(WoodenKiteShield),
            typeof(WoodenShield)
        };

        public static Type[] GemTypes { get; } =
        {
            typeof(Amber), typeof(Amethyst), typeof(Citrine),
            typeof(Diamond), typeof(Emerald), typeof(Ruby),
            typeof(Sapphire), typeof(StarSapphire), typeof(Tourmaline)
        };

        public static Type[] JewelryTypes { get; } =
        {
            typeof(GoldRing), typeof(GoldBracelet),
            typeof(SilverRing), typeof(SilverBracelet)
        };

        public static Type[] RegTypes { get; } =
        {
            typeof(BlackPearl), typeof(Bloodmoss), typeof(Garlic),
            typeof(Ginseng), typeof(MandrakeRoot), typeof(Nightshade),
            typeof(SulfurousAsh), typeof(SpidersSilk)
        };

        public static Type[] NecroRegTypes { get; } =
        {
            typeof(BatWing), typeof(GraveDust), typeof(DaemonBlood),
            typeof(NoxCrystal), typeof(PigIron)
        };

        public static Type[] PotionTypes { get; } =
        {
            typeof(AgilityPotion), typeof(StrengthPotion), typeof(RefreshPotion),
            typeof(LesserCurePotion), typeof(LesserHealPotion), typeof(LesserPoisonPotion)
        };

        public static Type[] SEInstrumentTypes { get; } =
        {
            typeof(BambooFlute)
        };

        public static Type[] InstrumentTypes { get; } =
        {
            typeof(Drums), typeof(Harp), typeof(LapHarp),
            typeof(Lute), typeof(Tambourine), typeof(TambourineTassel)
        };

        public static Type[] StatueTypes { get; } =
        {
            typeof(StatueSouth), typeof(StatueSouth2), typeof(StatueNorth),
            typeof(StatueWest), typeof(StatueEast), typeof(StatueEast2),
            typeof(StatueSouthEast), typeof(BustSouth), typeof(BustEast)
        };

        public static Type[] RegularScrollTypes { get; } =
        {
            typeof(ReactiveArmorScroll), typeof(ClumsyScroll), typeof(CreateFoodScroll), typeof(FeeblemindScroll),
            typeof(HealScroll), typeof(MagicArrowScroll), typeof(NightSightScroll), typeof(WeakenScroll),
            typeof(AgilityScroll), typeof(CunningScroll), typeof(CureScroll), typeof(HarmScroll),
            typeof(MagicTrapScroll), typeof(MagicUnTrapScroll), typeof(ProtectionScroll), typeof(StrengthScroll),
            typeof(BlessScroll), typeof(FireballScroll), typeof(MagicLockScroll), typeof(PoisonScroll),
            typeof(TelekinesisScroll), typeof(TeleportScroll), typeof(UnlockScroll), typeof(WallOfStoneScroll),
            typeof(ArchCureScroll), typeof(ArchProtectionScroll), typeof(CurseScroll), typeof(FireFieldScroll),
            typeof(GreaterHealScroll), typeof(LightningScroll), typeof(ManaDrainScroll), typeof(RecallScroll),
            typeof(BladeSpiritsScroll), typeof(DispelFieldScroll), typeof(IncognitoScroll), typeof(MagicReflectScroll),
            typeof(MindBlastScroll), typeof(ParalyzeScroll), typeof(PoisonFieldScroll), typeof(SummonCreatureScroll),
            typeof(DispelScroll), typeof(EnergyBoltScroll), typeof(ExplosionScroll), typeof(InvisibilityScroll),
            typeof(MarkScroll), typeof(MassCurseScroll), typeof(ParalyzeFieldScroll), typeof(RevealScroll),
            typeof(ChainLightningScroll), typeof(EnergyFieldScroll), typeof(FlamestrikeScroll), typeof(GateTravelScroll),
            typeof(ManaVampireScroll), typeof(MassDispelScroll), typeof(MeteorSwarmScroll), typeof(PolymorphScroll),
            typeof(EarthquakeScroll), typeof(EnergyVortexScroll), typeof(ResurrectionScroll),
            typeof(SummonAirElementalScroll),
            typeof(SummonDaemonScroll), typeof(SummonEarthElementalScroll), typeof(SummonFireElementalScroll),
            typeof(SummonWaterElementalScroll)
        };

        public static Type[] NecromancyScrollTypes { get; } =
        {
            typeof(AnimateDeadScroll), typeof(BloodOathScroll), typeof(CorpseSkinScroll), typeof(CurseWeaponScroll),
            typeof(EvilOmenScroll), typeof(HorrificBeastScroll), typeof(LichFormScroll), typeof(MindRotScroll),
            typeof(PainSpikeScroll), typeof(PoisonStrikeScroll), typeof(StrangleScroll), typeof(SummonFamiliarScroll),
            typeof(VampiricEmbraceScroll), typeof(VengefulSpiritScroll), typeof(WitherScroll), typeof(WraithFormScroll)
        };

        public static Type[] SENecromancyScrollTypes { get; } =
        {
            typeof(AnimateDeadScroll), typeof(BloodOathScroll), typeof(CorpseSkinScroll), typeof(CurseWeaponScroll),
            typeof(EvilOmenScroll), typeof(HorrificBeastScroll), typeof(LichFormScroll), typeof(MindRotScroll),
            typeof(PainSpikeScroll), typeof(PoisonStrikeScroll), typeof(StrangleScroll), typeof(SummonFamiliarScroll),
            typeof(VampiricEmbraceScroll), typeof(VengefulSpiritScroll), typeof(WitherScroll), typeof(WraithFormScroll),
            typeof(ExorcismScroll)
        };

        public static Type[] PaladinScrollTypes { get; } = Array.Empty<Type>();

        public static Type[] ArcanistScrollTypes { get; } =
        {
            typeof(ArcaneCircleScroll), typeof(GiftOfRenewalScroll), typeof(ImmolatingWeaponScroll),
            typeof(AttuneWeaponScroll),
            typeof(ThunderstormScroll),
            typeof(NatureFuryScroll), /*typeof( SummonFeyScroll ), typeof( SummonFiendScroll ),*/
            typeof(ReaperFormScroll), typeof(WildfireScroll), typeof(EssenceOfWindScroll), typeof(DryadAllureScroll),
            typeof(EtherealVoyageScroll), typeof(WordOfDeathScroll), typeof(GiftOfLifeScroll),
            typeof(ArcaneEmpowermentScroll)
        };

        public static Type[] GrimmochJournalTypes { get; } =
        {
            typeof(GrimmochJournal1), typeof(GrimmochJournal2), typeof(GrimmochJournal3),
            typeof(GrimmochJournal6), typeof(GrimmochJournal7), typeof(GrimmochJournal11),
            typeof(GrimmochJournal14), typeof(GrimmochJournal17), typeof(GrimmochJournal23)
        };

        public static Type[] LysanderNotebookTypes { get; } =
        {
            typeof(LysanderNotebook1), typeof(LysanderNotebook2), typeof(LysanderNotebook3),
            typeof(LysanderNotebook7), typeof(LysanderNotebook8), typeof(LysanderNotebook11)
        };

        public static Type[] TavarasJournalTypes { get; } =
        {
            typeof(TavarasJournal1), typeof(TavarasJournal2), typeof(TavarasJournal3),
            typeof(TavarasJournal6), typeof(TavarasJournal7), typeof(TavarasJournal8),
            typeof(TavarasJournal9), typeof(TavarasJournal11), typeof(TavarasJournal14),
            typeof(TavarasJournal16), typeof(TavarasJournal16b), typeof(TavarasJournal17),
            typeof(TavarasJournal19)
        };

        public static Type[] NewWandTypes { get; } =
        {
            typeof(FireballWand), typeof(LightningWand), typeof(MagicArrowWand),
            typeof(GreaterHealWand), typeof(HarmWand), typeof(HealWand)
        };

        public static Type[] WandTypes { get; } =
        {
            typeof(ClumsyWand), typeof(FeebleWand),
            typeof(ManaDrainWand), typeof(WeaknessWand)
        };

        public static Type[] OldWandTypes { get; } =
        {
            typeof(IDWand)
        };

        public static Type[] SEClothingTypes { get; } =
        {
            typeof(ClothNinjaJacket), typeof(FemaleKimono), typeof(Hakama),
            typeof(HakamaShita), typeof(JinBaori), typeof(Kamishimo),
            typeof(MaleKimono), typeof(NinjaTabi), typeof(Obi),
            typeof(SamuraiTabi), typeof(TattsukeHakama), typeof(Waraji)
        };

        public static Type[] AosClothingTypes { get; } =
        {
            typeof(FurSarong), typeof(FurCape), typeof(FlowerGarland),
            typeof(GildedDress), typeof(FurBoots), typeof(FormalShirt)
        };

        public static Type[] ClothingTypes { get; } =
        {
            typeof(Cloak),
            typeof(Bonnet), typeof(Cap), typeof(FeatheredHat),
            typeof(FloppyHat), typeof(JesterHat), typeof(Surcoat),
            typeof(SkullCap), typeof(StrawHat), typeof(TallStrawHat),
            typeof(TricorneHat), typeof(WideBrimHat), typeof(WizardsHat),
            typeof(BodySash), typeof(Doublet), typeof(Boots),
            typeof(FullApron), typeof(JesterSuit), typeof(Sandals),
            typeof(Tunic), typeof(Shoes), typeof(Shirt),
            typeof(Kilt), typeof(Skirt), typeof(FancyShirt),
            typeof(FancyDress), typeof(ThighBoots), typeof(LongPants),
            typeof(PlainDress), typeof(Robe), typeof(ShortPants),
            typeof(HalfApron)
        };

        public static Type[] SEHatTypes { get; } =
        {
            typeof(ClothNinjaHood), typeof(Kasa)
        };

        public static Type[] AosHatTypes { get; } =
        {
            typeof(FlowerGarland), typeof(BearMask),
            typeof(DeerMask) // Are Bear& Deer mask inside the Pre-AoS loottables too?
        };

        public static Type[] HatTypes { get; } =
        {
            typeof(SkullCap), typeof(Bandana), typeof(FloppyHat),
            typeof(Cap), typeof(WideBrimHat), typeof(StrawHat),
            typeof(TallStrawHat), typeof(WizardsHat), typeof(Bonnet),
            typeof(FeatheredHat), typeof(TricorneHat), typeof(JesterHat)
        };

        public static Type[] LibraryBookTypes { get; } =
        {
            typeof(GrammarOfOrcish), typeof(CallToAnarchy), typeof(ArmsAndWeaponsPrimer),
            typeof(SongOfSamlethe), typeof(TaleOfThreeTribes), typeof(GuideToGuilds),
            typeof(BirdsOfBritannia), typeof(BritannianFlora), typeof(ChildrenTalesVol2),
            typeof(TalesOfVesperVol1), typeof(DeceitDungeonOfHorror), typeof(DimensionalTravel),
            typeof(EthicalHedonism), typeof(MyStory), typeof(DiversityOfOurLand),
            typeof(QuestOfVirtues), typeof(RegardingLlamas), typeof(TalkingToWisps),
            typeof(TamingDragons), typeof(BoldStranger), typeof(BurningOfTrinsic),
            typeof(TheFight), typeof(LifeOfATravellingMinstrel), typeof(MajorTradeAssociation),
            typeof(RankingsOfTrades), typeof(WildGirlOfTheForest), typeof(TreatiseOnAlchemy),
            typeof(VirtueBook)
        };

        private static readonly Type[][] _wandTypes = [WandTypes, NewWandTypes];
        private static readonly Type[][] _oldWandTypes = [OldWandTypes, WandTypes, NewWandTypes];

        public static BaseWand RandomWand()
        {
            if (Core.ML)
            {
                return Construct<BaseWand>(NewWandTypes);
            }

            if (Core.AOS)
            {
                return Construct<BaseWand>(_wandTypes);
            }

            return Construct<BaseWand>(_oldWandTypes);
        }

        private static readonly Type[][] _mlClothingTypes = [MLClothingTypes, AosClothingTypes, ClothingTypes];
        private static readonly Type[][] _seClothingTypes = [SEClothingTypes, AosClothingTypes, ClothingTypes];
        private static readonly Type[][] _aosClothingTypes = [AosClothingTypes, ClothingTypes];

        public static BaseClothing RandomClothing(bool inTokuno = false, bool isMondain = false)
        {
            if (Core.ML && isMondain)
            {
                return Construct<BaseClothing>(_mlClothingTypes);
            }

            if (Core.SE && inTokuno)
            {
                return Construct<BaseClothing>(_seClothingTypes);
            }

            if (Core.AOS)
            {
                return Construct<BaseClothing>(_aosClothingTypes);
            }

            return Construct<BaseClothing>(ClothingTypes);
        }

        private static readonly Type[][] _mlRangedWeaponTypes = [MLRangedWeaponTypes, AosRangedWeaponTypes, RangedWeaponTypes];
        private static readonly Type[][] _seRangedWeaponTypes = [SERangedWeaponTypes, AosRangedWeaponTypes, RangedWeaponTypes];
        private static readonly Type[][] _aosRangedWeaponTypes = [AosRangedWeaponTypes, RangedWeaponTypes];

        public static BaseWeapon RandomRangedWeapon(bool inTokuno = false, bool isMondain = false)
        {
            if (Core.ML && isMondain)
            {
                return Construct<BaseWeapon>(_mlRangedWeaponTypes);
            }

            if (Core.SE && inTokuno)
            {
                return Construct<BaseWeapon>(_seRangedWeaponTypes);
            }

            if (Core.AOS)
            {
                return Construct<BaseWeapon>(_aosRangedWeaponTypes);
            }

            return Construct<BaseWeapon>(RangedWeaponTypes);
        }

        private static readonly Type[][] _mlWeaponTypes = [MLWeaponTypes, AosWeaponTypes, WeaponTypes];
        private static readonly Type[][] _seWeaponTypes = [SEWeaponTypes, AosWeaponTypes, WeaponTypes];
        private static readonly Type[][] _aosWeaponTypes = [AosWeaponTypes, WeaponTypes];

        public static BaseWeapon RandomWeapon(bool inTokuno = false, bool isMondain = false)
        {
            if (Core.ML && isMondain)
            {
                return Construct<BaseWeapon>(_mlWeaponTypes);
            }

            if (Core.SE && inTokuno)
            {
                return Construct<BaseWeapon>(_seWeaponTypes);
            }

            if (Core.AOS)
            {
                return Construct<BaseWeapon>(_aosWeaponTypes);
            }

            return Construct<BaseWeapon>(WeaponTypes);
        }

        private static readonly Type[][] _mlWeaponOrJewelryTypes = [MLWeaponTypes, AosWeaponTypes, WeaponTypes, JewelryTypes];
        private static readonly Type[][] _seWeaponOrJewelryTypes = [SEWeaponTypes, AosWeaponTypes, WeaponTypes, JewelryTypes];
        private static readonly Type[][] _aosWeaponOrJewelryTypes = [AosWeaponTypes, WeaponTypes, JewelryTypes];
        private static readonly Type[][] _oldWeaponOrJewelryTypes = [WeaponTypes, JewelryTypes];

        public static Item RandomWeaponOrJewelry(bool inTokuno = false, bool isMondain = false)
        {
            if (Core.ML && isMondain)
            {
                return Construct(_mlWeaponOrJewelryTypes);
            }

            if (Core.SE && inTokuno)
            {
                return Construct(_seWeaponOrJewelryTypes);
            }

            if (Core.AOS)
            {
                return Construct(_aosWeaponOrJewelryTypes);
            }

            return Construct(_oldWeaponOrJewelryTypes);
        }

        public static BaseJewel RandomJewelry() => Construct<BaseJewel>(JewelryTypes);

        private static readonly Type[][] _mlArmorTypes = [MLArmorTypes, ArmorTypes];
        private static readonly Type[][] _seArmorTypes = [SEArmorTypes, ArmorTypes];

        public static BaseArmor RandomArmor(bool inTokuno = false, bool isMondain = false)
        {
            if (Core.ML && isMondain)
            {
                return Construct<BaseArmor>(_mlArmorTypes);
            }

            if (Core.SE && inTokuno)
            {
                return Construct<BaseArmor>(_seArmorTypes);
            }

            return Construct<BaseArmor>(ArmorTypes);
        }

        private static readonly Type[][] _seHatTypes = [SEHatTypes, AosHatTypes, HatTypes];
        private static readonly Type[][] _aosHatTypes = [AosHatTypes, HatTypes];

        public static BaseHat RandomHat(bool inTokuno = false)
        {
            if (Core.SE && inTokuno)
            {
                return Construct<BaseHat>(_seHatTypes);
            }

            if (Core.AOS)
            {
                return Construct<BaseHat>(_aosHatTypes);
            }

            return Construct<BaseHat>(HatTypes);
        }

        private static readonly Type[][] _mlArmorOrHatTypes = [MLArmorTypes, ArmorTypes, AosHatTypes, HatTypes];
        private static readonly Type[][] _seArmorOrHatTypes = [SEArmorTypes, ArmorTypes, SEHatTypes, AosHatTypes, HatTypes];
        private static readonly Type[][] _aosArmorOrHatTypes = [ArmorTypes, AosHatTypes, HatTypes];
        private static readonly Type[][] _oldArmorOrHatTypes = [ArmorTypes, HatTypes];

        public static Item RandomArmorOrHat(bool inTokuno = false, bool isMondain = false)
        {
            if (Core.ML && isMondain)
            {
                return Construct(_mlArmorOrHatTypes);
            }

            if (Core.SE && inTokuno)
            {
                return Construct(_seArmorOrHatTypes);
            }

            if (Core.AOS)
            {
                return Construct(_aosArmorOrHatTypes);
            }

            return Construct(_oldArmorOrHatTypes);
        }

        private static readonly Type[][] _aosShieldTypes = [AosShieldTypes, ShieldTypes];

        public static BaseShield RandomShield()
        {
            if (Core.AOS)
            {
                return Construct<BaseShield>(_aosShieldTypes);
            }

            return Construct<BaseShield>(ShieldTypes);
        }

        private static readonly Type[][] _mlArmorOrShieldTypes = [MLArmorTypes, ArmorTypes, AosShieldTypes, ShieldTypes];
        private static readonly Type[][] _seArmorOrShieldTypes = [SEArmorTypes, ArmorTypes, AosShieldTypes, ShieldTypes];
        private static readonly Type[][] _aosArmorOrShieldTypes = [ArmorTypes, AosShieldTypes, ShieldTypes];
        private static readonly Type[][] _oldArmorOrShieldTypes = [ArmorTypes, ShieldTypes];

        public static BaseArmor RandomArmorOrShield(bool inTokuno = false, bool isMondain = false)
        {
            if (Core.ML && isMondain)
            {
                return Construct<BaseArmor>(_mlArmorOrShieldTypes);
            }

            if (Core.SE && inTokuno)
            {
                return Construct<BaseArmor>(_seArmorOrShieldTypes);
            }

            if (Core.AOS)
            {
                return Construct<BaseArmor>(_aosArmorOrShieldTypes);
            }

            return Construct<BaseArmor>(_oldArmorOrShieldTypes);
        }

        private static readonly Type[][] _mlArmorOrHatOrShieldOrJewelryTypes =
            [MLArmorTypes, ArmorTypes, AosHatTypes, HatTypes, AosShieldTypes, ShieldTypes, JewelryTypes];
        private static readonly Type[][] _seArmorOrHatOrShieldOrJewelryTypes =
            [SEArmorTypes, ArmorTypes, SEHatTypes, AosHatTypes, HatTypes, AosShieldTypes, ShieldTypes, JewelryTypes];
        private static readonly Type[][] _aosArmorOrHatOrShieldOrJewelryTypes =
            [ArmorTypes, AosHatTypes, HatTypes, AosShieldTypes, ShieldTypes, JewelryTypes];
        private static readonly Type[][] _oldArmorOrHatOrShieldOrJewelryTypes =
            [ArmorTypes, HatTypes, ShieldTypes, JewelryTypes];

        public static Item RandomArmorOrShieldOrJewelry(bool inTokuno = false, bool isMondain = false)
        {
            if (Core.ML && isMondain)
            {
                return Construct(_mlArmorOrHatOrShieldOrJewelryTypes);
            }

            if (Core.SE && inTokuno)
            {
                return Construct(_seArmorOrHatOrShieldOrJewelryTypes);
            }

            if (Core.AOS)
            {
                return Construct(_aosArmorOrHatOrShieldOrJewelryTypes);
            }

            return Construct(_oldArmorOrHatOrShieldOrJewelryTypes);
        }

        private static readonly Type[][] _mlWeaponOrRangedOrArmorOrHatOrShieldTypes =
        [
            MLWeaponTypes, AosWeaponTypes, WeaponTypes, MLRangedWeaponTypes, AosRangedWeaponTypes, RangedWeaponTypes,
            MLArmorTypes, ArmorTypes, AosHatTypes, HatTypes, AosShieldTypes, ShieldTypes
        ];

        private static readonly Type[][] _seWeaponOrRangedOrArmorOrHatOrShieldTypes =
        [
            SEWeaponTypes, AosWeaponTypes, WeaponTypes, SERangedWeaponTypes, AosRangedWeaponTypes, RangedWeaponTypes,
            SEArmorTypes, ArmorTypes, SEHatTypes, AosHatTypes, HatTypes, AosShieldTypes, ShieldTypes
        ];

        private static readonly Type[][] _aosWeaponOrRangedOrArmorOrHatOrShieldTypes =
        [
            AosWeaponTypes, WeaponTypes, AosRangedWeaponTypes, RangedWeaponTypes, ArmorTypes, AosHatTypes, HatTypes,
            AosShieldTypes, ShieldTypes
        ];

        private static readonly Type[][] _oldWeaponOrRangedOrArmorOrHatOrShieldTypes =
        [
            WeaponTypes, RangedWeaponTypes, ArmorTypes, HatTypes, ShieldTypes
        ];

        public static Item RandomArmorOrShieldOrWeapon(bool inTokuno = false, bool isMondain = false)
        {
            if (Core.ML && isMondain)
            {
                return Construct(_mlWeaponOrRangedOrArmorOrHatOrShieldTypes);
            }

            if (Core.SE && inTokuno)
            {
                return Construct(_seWeaponOrRangedOrArmorOrHatOrShieldTypes);
            }

            if (Core.AOS)
            {
                return Construct(_aosWeaponOrRangedOrArmorOrHatOrShieldTypes);
            }

            return Construct(_oldWeaponOrRangedOrArmorOrHatOrShieldTypes);
        }

        private static readonly Type[][] _mlWeaponOrRangedOrArmorOrHatOrShieldOrJewelryTypes =
        [
            MLWeaponTypes, AosWeaponTypes, WeaponTypes, MLRangedWeaponTypes, AosRangedWeaponTypes, RangedWeaponTypes,
            MLArmorTypes, ArmorTypes, AosHatTypes, HatTypes, AosShieldTypes, ShieldTypes, JewelryTypes
        ];

        private static readonly Type[][] _seWeaponOrRangedOrArmorOrHatOrShieldOrJewelryTypes =
        [
            SEWeaponTypes, AosWeaponTypes, WeaponTypes, SERangedWeaponTypes, AosRangedWeaponTypes, RangedWeaponTypes,
            SEArmorTypes, ArmorTypes, SEHatTypes, AosHatTypes, HatTypes, AosShieldTypes, ShieldTypes, JewelryTypes
        ];

        private static readonly Type[][] _aosWeaponOrRangedOrArmorOrHatOrShieldOrJewelryTypes =
        [
            AosWeaponTypes, WeaponTypes, AosRangedWeaponTypes, RangedWeaponTypes, ArmorTypes, AosHatTypes, HatTypes,
            AosShieldTypes, ShieldTypes, JewelryTypes
        ];

        private static readonly Type[][] _oldWeaponOrRangedOrArmorOrHatOrShieldOrJewelryTypes =
        [
            WeaponTypes, RangedWeaponTypes, ArmorTypes, HatTypes, ShieldTypes, JewelryTypes
        ];

        public static Item RandomArmorOrShieldOrWeaponOrJewelry(bool inTokuno = false, bool isMondain = false)
        {
            if (Core.ML && isMondain)
            {
                return Construct(_mlWeaponOrRangedOrArmorOrHatOrShieldOrJewelryTypes);
            }

            if (Core.SE && inTokuno)
            {
                return Construct(_seWeaponOrRangedOrArmorOrHatOrShieldOrJewelryTypes);
            }

            if (Core.AOS)
            {
                return Construct(_aosWeaponOrRangedOrArmorOrHatOrShieldOrJewelryTypes);
            }

            return Construct(_oldWeaponOrRangedOrArmorOrHatOrShieldOrJewelryTypes);
        }

        private static readonly Type[][] _chestOfHeirloomsContentTypes =
        [
            SEArmorTypes, SEHatTypes, SEWeaponTypes, SERangedWeaponTypes, JewelryTypes
        ];

        public static Item RandomChestOfHeirloomsContent() => Construct(_chestOfHeirloomsContentTypes);

        public static Item RandomGem() => Construct(GemTypes);

        public static Item RandomReagent() => Construct(RegTypes);

        public static Item RandomNecromancyReagent() => Construct(NecroRegTypes);

        private static readonly Type[][] _regOrNecroRegTypes = [RegTypes, NecroRegTypes];

        public static Item RandomPossibleReagent() => Core.AOS ? Construct(_regOrNecroRegTypes) : Construct(RegTypes);

        public static Item RandomPotion() => Construct(PotionTypes);

        private static readonly Type[][] _seInstrumentTypes = [SEInstrumentTypes, InstrumentTypes];

        public static BaseInstrument RandomInstrument() => Core.SE
            ? Construct<BaseInstrument>(_seInstrumentTypes)
            : Construct<BaseInstrument>(InstrumentTypes);

        public static Item RandomStatue() => Construct(StatueTypes);

        public static SpellScroll RandomScroll(int minIndex, int maxIndex, SpellbookType type)
        {
            var types = type switch
            {
                SpellbookType.Regular     => RegularScrollTypes,
                SpellbookType.Necromancer => Core.SE ? SENecromancyScrollTypes : NecromancyScrollTypes,
                SpellbookType.Paladin     => PaladinScrollTypes,
                SpellbookType.Arcanist    => ArcanistScrollTypes,
                _                         => RegularScrollTypes
            };

            return Construct<SpellScroll>(types, Utility.RandomMinMax(minIndex, maxIndex));
        }

        public static BaseBook RandomGrimmochJournal() => Construct<BaseBook>(GrimmochJournalTypes);

        public static BaseBook RandomLysanderNotebook() => Construct<BaseBook>(LysanderNotebookTypes);

        public static BaseBook RandomTavarasJournal() => Construct<BaseBook>(TavarasJournalTypes);

        public static BaseBook RandomLibraryBook() => Construct<BaseBook>(LibraryBookTypes);

        public static BaseTalisman RandomTalisman()
        {
            var talisman = new BaseTalisman(BaseTalisman.GetRandomItemID());

            talisman.Summoner = BaseTalisman.GetRandomSummoner();

            if (talisman.Summoner.IsEmpty)
            {
                talisman.Removal = BaseTalisman.GetRandomRemoval();

                if (talisman.Removal != TalismanRemoval.None)
                {
                    talisman.MaxCharges = BaseTalisman.GetRandomCharges();
                    talisman.MaxChargeTime = 1200;
                }
            }
            else
            {
                talisman.MaxCharges = Utility.RandomMinMax(10, 50);
                talisman.MaxChargeTime = talisman.Summoner.IsItem ? 60 : 1800;
            }

            talisman.Blessed = BaseTalisman.GetRandomBlessed();
            talisman.Slayer = BaseTalisman.GetRandomSlayer();
            talisman.Protection = BaseTalisman.GetRandomProtection();
            talisman.Killer = BaseTalisman.GetRandomKiller();
            talisman.Skill = BaseTalisman.GetRandomSkill();
            talisman.ExceptionalBonus = BaseTalisman.GetRandomExceptional();
            talisman.SuccessBonus = BaseTalisman.GetRandomSuccessful();
            talisman.Charges = talisman.MaxCharges;

            return talisman;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Item Construct(Type type) => Construct<Item>(type);

        public static T Construct<T>(Type type) where T : Item
        {
            if (type == null)
            {
                return null;
            }

            try
            {
                return type.CreateInstance<T>();
            }
            catch
            {
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Item Construct(params ReadOnlySpan<Type> types) => Construct<Item>(types.RandomElement());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Construct<T>(params ReadOnlySpan<Type> types) where T : Item => Construct<T>(types.RandomElement());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Item Construct(ReadOnlySpan<Type> types, int index) => Construct<Item>(types, index);

        public static T Construct<T>(ReadOnlySpan<Type> types, int index) where T : Item
        {
            if (index >= 0 && index < types.Length)
            {
                return Construct<T>(types[index]);
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Item Construct(params Type[][] types) => Construct<Item>(types);

        public static T Construct<T>(params Type[][] types) where T : Item
        {
            var totalLength = 0;

            for (var i = 0; i < types.Length; ++i)
            {
                totalLength += types[i].Length;
            }

            if (totalLength > 0)
            {
                var index = Utility.Random(totalLength);

                for (var i = 0; i < types.Length; ++i)
                {
                    if (index >= 0 && index < types[i].Length)
                    {
                        return Construct<T>(types[i][index]);
                    }

                    index -= types[i].Length;
                }
            }

            return null;
        }
    }
}
