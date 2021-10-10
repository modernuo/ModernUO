using System;
using System.Runtime.CompilerServices;
using Server.Accounting;
using Server.Factions;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Network;

namespace Server.Misc
{
    public static class CharacterCreation
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(CharacterCreation));

        private static readonly TimeSpan BadStartMessageDelay = TimeSpan.FromSeconds(3.5);

        private static readonly CityInfo m_NewHavenInfo =
            new("New Haven", "The Bountiful Harvest Inn", 3503, 2574, 14, Map.Trammel);

        private static Mobile m_Mobile;

        public static void Initialize()
        {
            // Register our event handler
            EventSink.CharacterCreated += EventSink_CharacterCreated;
        }

        private static void AddBackpack(Mobile m)
        {
            var pack = m.Backpack;

            if (pack == null)
            {
                pack = new Backpack();
                pack.Movable = false;

                m.AddItem(pack);
            }

            PackItem(new RedBook("a book", m.Name, 20, true));
            PackItem(new Gold(1000)); // Starting gold can be customized here
            PackItem(new Dagger());
            PackItem(new Candle());
        }

        private static Item MakeNewbie(Item item)
        {
            if (!Core.AOS)
            {
                item.LootType = LootType.Newbied;
            }

            return item;
        }

        private static void PlaceItemIn(Container parent, int x, int y, Item item)
        {
            parent.AddItem(item);
            item.Location = new Point3D(x, y, 0);
        }

        private static Item MakePotionKeg(PotionEffect type, int hue)
        {
            var keg = new PotionKeg();

            keg.Held = 100;
            keg.Type = type;
            keg.Hue = hue;

            return MakeNewbie(keg);
        }

        private static void FillBankAOS(Mobile m)
        {
            var bank = m.BankBox;

            // The new AOS bankboxes don't have powerscrolls, they are automatically 'applied':

            for (var i = 0; i < PowerScroll.Skills.Count; ++i)
            {
                m.Skills[PowerScroll.Skills[i]].Cap = 120.0;
            }

            m.StatCap = 250;

            Container cont;

            // Begin box of money
            cont = new WoodenBox { ItemID = 0xE7D, Hue = 0x489 };

            PlaceItemIn(cont, 16, 51, new BankCheck(500000));
            PlaceItemIn(cont, 28, 51, new BankCheck(250000));
            PlaceItemIn(cont, 40, 51, new BankCheck(100000));
            PlaceItemIn(cont, 52, 51, new BankCheck(100000));
            PlaceItemIn(cont, 64, 51, new BankCheck(50000));

            PlaceItemIn(cont, 16, 115, new Silver(9000));
            PlaceItemIn(cont, 34, 115, new Gold(60000));

            PlaceItemIn(bank, 18, 169, cont);
            // End box of money

            // Begin bag of potion kegs
            cont = new Backpack { Name = "Various Potion Kegs" };

            PlaceItemIn(cont, 45, 149, MakePotionKeg(PotionEffect.CureGreater, 0x2D));
            PlaceItemIn(cont, 69, 149, MakePotionKeg(PotionEffect.HealGreater, 0x499));
            PlaceItemIn(cont, 93, 149, MakePotionKeg(PotionEffect.PoisonDeadly, 0x46));
            PlaceItemIn(cont, 117, 149, MakePotionKeg(PotionEffect.RefreshTotal, 0x21));
            PlaceItemIn(cont, 141, 149, MakePotionKeg(PotionEffect.ExplosionGreater, 0x74));

            PlaceItemIn(cont, 93, 82, new Bottle(1000));

            PlaceItemIn(bank, 53, 169, cont);
            // End bag of potion kegs

            // Begin bag of tools
            cont = new Bag { Name = "Tool Bag" };

            PlaceItemIn(cont, 30, 35, new TinkerTools(1000));
            PlaceItemIn(cont, 60, 35, new HousePlacementTool());
            PlaceItemIn(cont, 90, 35, new DovetailSaw(1000));
            PlaceItemIn(cont, 30, 68, new Scissors());
            PlaceItemIn(cont, 45, 68, new MortarPestle(1000));
            PlaceItemIn(cont, 75, 68, new ScribesPen(1000));
            PlaceItemIn(cont, 90, 68, new SmithHammer(1000));
            PlaceItemIn(cont, 30, 118, new TwoHandedAxe());
            PlaceItemIn(cont, 60, 118, new FletcherTools(1000));
            PlaceItemIn(cont, 90, 118, new SewingKit(1000));

            PlaceItemIn(cont, 36, 51, new RunicHammer(CraftResource.DullCopper, 1000));
            PlaceItemIn(cont, 42, 51, new RunicHammer(CraftResource.ShadowIron, 1000));
            PlaceItemIn(cont, 48, 51, new RunicHammer(CraftResource.Copper, 1000));
            PlaceItemIn(cont, 54, 51, new RunicHammer(CraftResource.Bronze, 1000));
            PlaceItemIn(cont, 61, 51, new RunicHammer(CraftResource.Gold, 1000));
            PlaceItemIn(cont, 67, 51, new RunicHammer(CraftResource.Agapite, 1000));
            PlaceItemIn(cont, 73, 51, new RunicHammer(CraftResource.Verite, 1000));
            PlaceItemIn(cont, 79, 51, new RunicHammer(CraftResource.Valorite, 1000));

            PlaceItemIn(cont, 36, 55, new RunicSewingKit(CraftResource.SpinedLeather, 1000));
            PlaceItemIn(cont, 42, 55, new RunicSewingKit(CraftResource.HornedLeather, 1000));
            PlaceItemIn(cont, 48, 55, new RunicSewingKit(CraftResource.BarbedLeather, 1000));

            PlaceItemIn(bank, 118, 169, cont);
            // End bag of tools

            // Begin bag of archery ammo
            cont = new Bag { Name = "Bag Of Archery Ammo" };

            PlaceItemIn(cont, 48, 76, new Arrow(5000));
            PlaceItemIn(cont, 72, 76, new Bolt(5000));

            PlaceItemIn(bank, 118, 124, cont);
            // End bag of archery ammo

            // Begin bag of treasure maps
            cont = new Bag { Name = "Bag Of Treasure Maps" };

            PlaceItemIn(cont, 30, 35, new TreasureMap(1, Map.Trammel));
            PlaceItemIn(cont, 45, 35, new TreasureMap(2, Map.Trammel));
            PlaceItemIn(cont, 60, 35, new TreasureMap(3, Map.Trammel));
            PlaceItemIn(cont, 75, 35, new TreasureMap(4, Map.Trammel));
            PlaceItemIn(cont, 90, 35, new TreasureMap(5, Map.Trammel));
            PlaceItemIn(cont, 90, 35, new TreasureMap(6, Map.Trammel));

            PlaceItemIn(cont, 30, 50, new TreasureMap(1, Map.Trammel));
            PlaceItemIn(cont, 45, 50, new TreasureMap(2, Map.Trammel));
            PlaceItemIn(cont, 60, 50, new TreasureMap(3, Map.Trammel));
            PlaceItemIn(cont, 75, 50, new TreasureMap(4, Map.Trammel));
            PlaceItemIn(cont, 90, 50, new TreasureMap(5, Map.Trammel));
            PlaceItemIn(cont, 90, 50, new TreasureMap(6, Map.Trammel));

            PlaceItemIn(cont, 55, 100, new Lockpick(30));
            PlaceItemIn(cont, 60, 100, new Pickaxe());

            PlaceItemIn(bank, 98, 124, cont);
            // End bag of treasure maps

            // Begin bag of raw materials
            cont = new Bag { Hue = 0x835, Name = "Raw Materials Bag" };

            PlaceItemIn(cont, 92, 60, new BarbedLeather(5000));
            PlaceItemIn(cont, 92, 68, new HornedLeather(5000));
            PlaceItemIn(cont, 92, 76, new SpinedLeather(5000));
            PlaceItemIn(cont, 92, 84, new Leather(5000));

            PlaceItemIn(cont, 30, 118, new Cloth(5000));
            PlaceItemIn(cont, 30, 84, new Board(5000));
            PlaceItemIn(cont, 57, 80, new BlankScroll(500));

            PlaceItemIn(cont, 30, 35, new DullCopperIngot(5000));
            PlaceItemIn(cont, 37, 35, new ShadowIronIngot(5000));
            PlaceItemIn(cont, 44, 35, new CopperIngot(5000));
            PlaceItemIn(cont, 51, 35, new BronzeIngot(5000));
            PlaceItemIn(cont, 58, 35, new GoldIngot(5000));
            PlaceItemIn(cont, 65, 35, new AgapiteIngot(5000));
            PlaceItemIn(cont, 72, 35, new VeriteIngot(5000));
            PlaceItemIn(cont, 79, 35, new ValoriteIngot(5000));
            PlaceItemIn(cont, 86, 35, new IronIngot(5000));

            PlaceItemIn(cont, 30, 59, new RedScales(5000));
            PlaceItemIn(cont, 36, 59, new YellowScales(5000));
            PlaceItemIn(cont, 42, 59, new BlackScales(5000));
            PlaceItemIn(cont, 48, 59, new GreenScales(5000));
            PlaceItemIn(cont, 54, 59, new WhiteScales(5000));
            PlaceItemIn(cont, 60, 59, new BlueScales(5000));

            PlaceItemIn(bank, 98, 169, cont);
            // End bag of raw materials

            // Begin bag of spell casting stuff
            cont = new Backpack { Hue = 0x480, Name = "Spell Casting Stuff" };

            PlaceItemIn(cont, 45, 105, new Spellbook(ulong.MaxValue));
            PlaceItemIn(cont, 65, 105, new NecromancerSpellbook(0xFFFFUL));
            PlaceItemIn(cont, 85, 105, new BookOfChivalry());
            PlaceItemIn(cont, 105, 105, new BookOfBushido());  // Default ctor = full
            PlaceItemIn(cont, 125, 105, new BookOfNinjitsu()); // Default ctor = full

            var runebook = new Runebook(10);
            runebook.CurCharges = runebook.MaxCharges;
            PlaceItemIn(cont, 145, 105, runebook);

            Item toHue = new BagOfReagents(150) { Hue = 0x2D };
            PlaceItemIn(cont, 45, 150, toHue);

            toHue = new BagOfNecroReagents(150) { Hue = 0x488 };
            PlaceItemIn(cont, 65, 150, toHue);

            PlaceItemIn(cont, 140, 150, new BagOfAllReagents(500));

            for (var i = 0; i < 9; ++i)
            {
                PlaceItemIn(cont, 45 + i * 10, 75, new RecallRune());
            }

            PlaceItemIn(cont, 141, 74, new FireHorn());

            PlaceItemIn(bank, 78, 169, cont);
            // End bag of spell casting stuff

            // Begin bag of ethereals
            cont = new Backpack { Hue = 0x490, Name = "Bag Of Ethy's!" };

            PlaceItemIn(cont, 45, 66, new EtherealHorse());
            PlaceItemIn(cont, 69, 82, new EtherealOstard());
            PlaceItemIn(cont, 93, 99, new EtherealLlama());
            PlaceItemIn(cont, 117, 115, new EtherealKirin());
            PlaceItemIn(cont, 45, 132, new EtherealUnicorn());
            PlaceItemIn(cont, 69, 66, new EtherealRidgeback());
            PlaceItemIn(cont, 93, 82, new EtherealSwampDragon());
            PlaceItemIn(cont, 117, 99, new EtherealBeetle());

            PlaceItemIn(bank, 38, 124, cont);
            // End bag of ethereals

            // Begin first bag of artifacts
            cont = new Backpack { Hue = 0x48F, Name = "Bag of Artifacts" };

            PlaceItemIn(cont, 45, 66, new TitansHammer());
            PlaceItemIn(cont, 69, 82, new InquisitorsResolution());
            PlaceItemIn(cont, 93, 99, new BladeOfTheRighteous());
            PlaceItemIn(cont, 117, 115, new ZyronicClaw());

            PlaceItemIn(bank, 58, 124, cont);
            // End first bag of artifacts

            // Begin second bag of artifacts
            cont = new Backpack { Hue = 0x48F, Name = "Bag of Artifacts" };

            PlaceItemIn(cont, 45, 66, new GauntletsOfNobility());
            PlaceItemIn(cont, 69, 82, new MidnightBracers());
            PlaceItemIn(cont, 93, 99, new VoiceOfTheFallenKing());
            PlaceItemIn(cont, 117, 115, new OrnateCrownOfTheHarrower());
            PlaceItemIn(cont, 45, 132, new HelmOfInsight());
            PlaceItemIn(cont, 69, 66, new HolyKnightsBreastplate());
            PlaceItemIn(cont, 93, 82, new ArmorOfFortune());
            PlaceItemIn(cont, 117, 99, new TunicOfFire());
            PlaceItemIn(cont, 45, 115, new LeggingsOfBane());
            PlaceItemIn(cont, 69, 132, new ArcaneShield());
            PlaceItemIn(cont, 93, 66, new Aegis());
            PlaceItemIn(cont, 117, 82, new RingOfTheVile());
            PlaceItemIn(cont, 45, 99, new BraceletOfHealth());
            PlaceItemIn(cont, 69, 115, new RingOfTheElements());
            PlaceItemIn(cont, 93, 132, new OrnamentOfTheMagician());
            PlaceItemIn(cont, 117, 66, new DivineCountenance());
            PlaceItemIn(cont, 45, 82, new JackalsCollar());
            PlaceItemIn(cont, 69, 99, new HuntersHeaddress());
            PlaceItemIn(cont, 93, 115, new HatOfTheMagi());
            PlaceItemIn(cont, 117, 132, new ShadowDancerLeggings());
            PlaceItemIn(cont, 45, 66, new SpiritOfTheTotem());
            PlaceItemIn(cont, 69, 82, new BladeOfInsanity());
            PlaceItemIn(cont, 93, 99, new AxeOfTheHeavens());
            PlaceItemIn(cont, 117, 115, new TheBeserkersMaul());
            PlaceItemIn(cont, 45, 132, new Frostbringer());
            PlaceItemIn(cont, 69, 66, new BreathOfTheDead());
            PlaceItemIn(cont, 93, 82, new TheDragonSlayer());
            PlaceItemIn(cont, 117, 99, new BoneCrusher());
            PlaceItemIn(cont, 45, 115, new StaffOfTheMagi());
            PlaceItemIn(cont, 69, 132, new SerpentsFang());
            PlaceItemIn(cont, 93, 66, new LegacyOfTheDreadLord());
            PlaceItemIn(cont, 117, 82, new TheTaskmaster());
            PlaceItemIn(cont, 45, 99, new TheDryadBow());

            PlaceItemIn(bank, 78, 124, cont);
            // End second bag of artifacts

            // Begin bag of minor artifacts
            cont = new Backpack { Hue = 0x48F, Name = "Bag of Minor Artifacts" };

            PlaceItemIn(cont, 45, 66, new LunaLance());
            PlaceItemIn(cont, 69, 82, new VioletCourage());
            PlaceItemIn(cont, 93, 99, new CavortingClub());
            PlaceItemIn(cont, 117, 115, new CaptainQuacklebushsCutlass());
            PlaceItemIn(cont, 45, 132, new NightsKiss());
            PlaceItemIn(cont, 69, 66, new ShipModelOfTheHMSCape());
            PlaceItemIn(cont, 93, 82, new AdmiralsHeartyRum());
            PlaceItemIn(cont, 117, 99, new CandelabraOfSouls());
            PlaceItemIn(cont, 45, 115, new IolosLute());
            PlaceItemIn(cont, 69, 132, new GwennosHarp());
            PlaceItemIn(cont, 93, 66, new ArcticDeathDealer());
            PlaceItemIn(cont, 117, 82, new EnchantedTitanLegBone());
            PlaceItemIn(cont, 45, 99, new NoxRangersHeavyCrossbow());
            PlaceItemIn(cont, 69, 115, new BlazeOfDeath());
            PlaceItemIn(cont, 93, 132, new DreadPirateHat());
            PlaceItemIn(cont, 117, 66, new BurglarsBandana());
            PlaceItemIn(cont, 45, 82, new GoldBricks());
            PlaceItemIn(cont, 69, 99, new AlchemistsBauble());
            PlaceItemIn(cont, 93, 115, new PhillipsWoodenSteed());
            PlaceItemIn(cont, 117, 132, new PolarBearMask());
            PlaceItemIn(cont, 45, 66, new BowOfTheJukaKing());
            PlaceItemIn(cont, 69, 82, new GlovesOfThePugilist());
            PlaceItemIn(cont, 93, 99, new OrcishVisage());
            PlaceItemIn(cont, 117, 115, new StaffOfPower());
            PlaceItemIn(cont, 45, 132, new ShieldOfInvulnerability());
            PlaceItemIn(cont, 69, 66, new HeartOfTheLion());
            PlaceItemIn(cont, 93, 82, new ColdBlood());
            PlaceItemIn(cont, 117, 99, new GhostShipAnchor());
            PlaceItemIn(cont, 45, 115, new SeahorseStatuette());
            PlaceItemIn(cont, 69, 132, new WrathOfTheDryad());
            PlaceItemIn(cont, 93, 66, new PixieSwatter());

            for (var i = 0; i < 10; i++)
            {
                PlaceItemIn(cont, 117, 128, new MessageInABottle(Utility.RandomBool() ? Map.Trammel : Map.Felucca, 4));
            }

            PlaceItemIn(bank, 18, 124, cont);

            if (Core.SE)
            {
                cont = new Bag { Hue = 0x501, Name = "Tokuno Minor Artifacts" };

                PlaceItemIn(cont, 42, 70, new Exiler());
                PlaceItemIn(cont, 38, 53, new HanzosBow());
                PlaceItemIn(cont, 45, 40, new TheDestroyer());
                PlaceItemIn(cont, 92, 80, new DragonNunchaku());
                PlaceItemIn(cont, 42, 56, new PeasantsBokuto());
                PlaceItemIn(cont, 44, 71, new TomeOfEnlightenment());
                PlaceItemIn(cont, 35, 35, new ChestOfHeirlooms());
                PlaceItemIn(cont, 29, 0, new HonorableSwords());
                PlaceItemIn(cont, 49, 85, new AncientUrn());
                PlaceItemIn(cont, 51, 58, new FluteOfRenewal());
                PlaceItemIn(cont, 70, 51, new PigmentsOfTokuno());
                PlaceItemIn(cont, 40, 79, new AncientSamuraiDo());
                PlaceItemIn(cont, 51, 61, new LegsOfStability());
                PlaceItemIn(cont, 88, 78, new GlovesOfTheSun());
                PlaceItemIn(cont, 55, 62, new AncientFarmersKasa());
                PlaceItemIn(cont, 55, 83, new ArmsOfTacticalExcellence());
                PlaceItemIn(cont, 50, 85, new DaimyosHelm());
                PlaceItemIn(cont, 52, 78, new BlackLotusHood());
                PlaceItemIn(cont, 52, 79, new DemonForks());
                PlaceItemIn(cont, 33, 49, new PilferedDancerFans());

                PlaceItemIn(bank, 58, 124, cont);
            }

            if (Core.SE) // This bag came only after SE.
            {
                cont = new Bag { Name = "Bag of Bows" };

                PlaceItemIn(cont, 31, 84, new Bow());
                PlaceItemIn(cont, 78, 74, new CompositeBow());
                PlaceItemIn(cont, 53, 71, new Crossbow());
                PlaceItemIn(cont, 56, 39, new HeavyCrossbow());
                PlaceItemIn(cont, 82, 72, new RepeatingCrossbow());
                PlaceItemIn(cont, 49, 45, new Yumi());

                for (var i = 0; i < cont.Items.Count; i++)
                {
                    if (cont.Items[i] is BaseRanged bow)
                    {
                        bow.Attributes.WeaponSpeed = 35;
                        bow.Attributes.WeaponDamage = 35;
                    }
                }

                PlaceItemIn(bank, 108, 135, cont);
            }
        }

        private static void FillBankbox(Mobile m)
        {
            if (Core.AOS)
            {
                FillBankAOS(m);
                return;
            }

            var bank = m.BankBox;

            bank.DropItem(new BankCheck(1000000));

            // Full spellbook
            var book = new Spellbook { Content = ulong.MaxValue };

            bank.DropItem(book);

            var bag = new Bag();

            for (var i = 0; i < 5; ++i)
            {
                bag.DropItem(new Moonstone(MoonstoneType.Felucca));
            }

            // Felucca moonstones
            bank.DropItem(bag);

            bag = new Bag();

            for (var i = 0; i < 5; ++i)
            {
                bag.DropItem(new Moonstone(MoonstoneType.Trammel));
            }

            // Trammel moonstones
            bank.DropItem(bag);

            // Treasure maps
            bank.DropItem(new TreasureMap(1, Map.Trammel));
            bank.DropItem(new TreasureMap(2, Map.Trammel));
            bank.DropItem(new TreasureMap(3, Map.Trammel));
            bank.DropItem(new TreasureMap(4, Map.Trammel));
            bank.DropItem(new TreasureMap(5, Map.Trammel));

            // Bag containing 50 of each reagent
            bank.DropItem(new BagOfReagents());

            // Craft tools
            bank.DropItem(MakeNewbie(new Scissors()));
            bank.DropItem(MakeNewbie(new SewingKit(1000)));
            bank.DropItem(MakeNewbie(new SmithHammer(1000)));
            bank.DropItem(MakeNewbie(new FletcherTools(1000)));
            bank.DropItem(MakeNewbie(new DovetailSaw(1000)));
            bank.DropItem(MakeNewbie(new MortarPestle(1000)));
            bank.DropItem(MakeNewbie(new ScribesPen(1000)));
            bank.DropItem(MakeNewbie(new TinkerTools(1000)));

            // A few dye tubs
            bank.DropItem(new Dyes());
            bank.DropItem(new DyeTub());
            bank.DropItem(new DyeTub());
            bank.DropItem(new BlackDyeTub());
            bank.DropItem(new DyeTub { DyedHue = 0x485, Redyable = false });

            // Some food
            bank.DropItem(MakeNewbie(new Apple(1000)));

            // Resources
            bank.DropItem(MakeNewbie(new Feather(1000)));
            bank.DropItem(MakeNewbie(new BoltOfCloth(1000)));
            bank.DropItem(MakeNewbie(new BlankScroll(1000)));
            bank.DropItem(MakeNewbie(new Hides(1000)));
            bank.DropItem(MakeNewbie(new Bandage(1000)));
            bank.DropItem(MakeNewbie(new Bottle(1000)));
            bank.DropItem(MakeNewbie(new Log(1000)));

            bank.DropItem(MakeNewbie(new IronIngot(5000)));
            bank.DropItem(MakeNewbie(new DullCopperIngot(5000)));
            bank.DropItem(MakeNewbie(new ShadowIronIngot(5000)));
            bank.DropItem(MakeNewbie(new CopperIngot(5000)));
            bank.DropItem(MakeNewbie(new BronzeIngot(5000)));
            bank.DropItem(MakeNewbie(new GoldIngot(5000)));
            bank.DropItem(MakeNewbie(new AgapiteIngot(5000)));
            bank.DropItem(MakeNewbie(new VeriteIngot(5000)));
            bank.DropItem(MakeNewbie(new ValoriteIngot(5000)));

            // Reagents
            bank.DropItem(MakeNewbie(new BlackPearl(1000)));
            bank.DropItem(MakeNewbie(new Bloodmoss(1000)));
            bank.DropItem(MakeNewbie(new Garlic(1000)));
            bank.DropItem(MakeNewbie(new Ginseng(1000)));
            bank.DropItem(MakeNewbie(new MandrakeRoot(1000)));
            bank.DropItem(MakeNewbie(new Nightshade(1000)));
            bank.DropItem(MakeNewbie(new SulfurousAsh(1000)));
            bank.DropItem(MakeNewbie(new SpidersSilk(1000)));

            // Some extra starting gold
            bank.DropItem(MakeNewbie(new Gold(9000)));

            // 5 blank recall runes
            for (var i = 0; i < 5; ++i)
            {
                bank.DropItem(MakeNewbie(new RecallRune()));
            }

            AddPowerScrolls(bank);
        }

        private static void AddPowerScrolls(BankBox bank)
        {
            var bag = new Bag();

            for (var i = 0; i < PowerScroll.Skills.Count; ++i)
            {
                bag.DropItem(new PowerScroll(PowerScroll.Skills[i], 120.0));
            }

            bag.DropItem(new StatCapScroll(250));

            bank.DropItem(bag);
        }

        private static void AddShirt(Mobile m, int shirtHue)
        {
            var hue = Utility.ClipDyedHue(shirtHue & 0x3FFF);

            if (m.Race == Race.Elf)
            {
                EquipItem(new ElvenShirt(hue), true);
            }
            else
            {
                Item shirt = Utility.Random(3) switch
                {
                    0 => new Shirt(hue),
                    1 => new FancyShirt(hue),
                    _ => new Doublet(hue)
                };

                EquipItem(shirt, true);
            }
        }

        private static void AddPants(Mobile m, int pantsHue)
        {
            var hue = Utility.ClipDyedHue(pantsHue & 0x3FFF);

            if (m.Race == Race.Elf)
            {
                EquipItem(new ElvenPants(hue), true);
            }
            else
            {
                var female = m.Female;
                Item pants = Utility.RandomBool() switch
                {
                    true when female  => new Skirt(hue),
                    true              => new LongPants(hue),
                    false when female => new Kilt(hue),
                    false             => new ShortPants(hue)
                };

                EquipItem(pants, true);
            }
        }

        private static void AddShoes(Mobile m)
        {
            if (m.Race == Race.Elf)
            {
                EquipItem(new ElvenBoots(), true);
            }
            else
            {
                EquipItem(new Shoes(Utility.RandomYellowHue()), true);
            }
        }

        private static Mobile CreateMobile(Account a)
        {
            if (a.Count >= a.Limit)
            {
                return null;
            }

            for (var i = 0; i < a.Length; ++i)
            {
                if (a[i] == null)
                {
                    return a[i] = new PlayerMobile();
                }
            }

            return null;
        }

        private static void EventSink_CharacterCreated(CharacterCreatedEventArgs args)
        {
            if (!VerifyProfession(args.Profession))
            {
                args.Profession = 0;
            }

            var state = args.State;

            if (state == null)
            {
                return;
            }

            var newChar = CreateMobile(args.Account as Account);

            if (newChar == null)
            {
                logger.Information("Login: {0}: Character creation failed, account full", state);
                return;
            }

            args.Mobile = newChar;
            m_Mobile = newChar;

            newChar.Player = true;
            newChar.AccessLevel = args.Account.AccessLevel;
            newChar.Female = args.Female;
            newChar.Race = Core.Expansion >= args.Race.RequiredExpansion ? args.Race : Race.DefaultRace;
            newChar.Hue = newChar.Race.ClipSkinHue(args.Hue & 0x3FFF) | 0x8000;
            newChar.Hunger = 20;

            var young = false;

            if (newChar is PlayerMobile pm)
            {
                pm.Profession = args.Profession;

                if (pm.AccessLevel == AccessLevel.Player && ((Account)pm.Account).Young)
                {
                    young = pm.Young = true;
                }
            }

            SetName(newChar, args.Name);

            AddBackpack(newChar);

            SetStats(newChar, state, args.Str, args.Dex, args.Int);
            SetSkills(newChar, args.Skills, args.Profession);

            var race = newChar.Race;

            if (race.ValidateHair(newChar, args.HairID))
            {
                newChar.HairItemID = args.HairID;
                newChar.HairHue = race.ClipHairHue(args.HairHue & 0x3FFF);
            }

            if (race.ValidateFacialHair(newChar, args.BeardID))
            {
                newChar.FacialHairItemID = args.BeardID;
                newChar.FacialHairHue = race.ClipHairHue(args.BeardHue & 0x3FFF);
            }

            if (args.Profession <= 3)
            {
                AddShirt(newChar, args.ShirtHue);
                AddPants(newChar, args.PantsHue);
                AddShoes(newChar);
            }

            if (TestCenter.Enabled)
            {
                FillBankbox(newChar);
            }

            if (young)
            {
                var ticket = new NewPlayerTicket();
                ticket.Owner = newChar;
                newChar.BankBox.DropItem(ticket);
            }

            var city = GetStartLocation(args, young);

            newChar.MoveToWorld(city.Location, city.Map);

            logger.Information(
                "Login: {0}: New character being created (account={1}, character={2}, serial={3}, started.city={4}, started.location={5}, started.map={6})",
                state,
                args.Account.Username,
                newChar.Name,
                newChar.Serial,
                city.City,
                city.Location,
                city.Map);

            new WelcomeTimer(newChar).Start();
        }

        public static bool VerifyProfession(int profession) =>
            profession >= 0 && (profession < 4 || Core.AOS && profession < 6 || Core.SE && profession < 8);

        private static CityInfo GetStartLocation(CharacterCreatedEventArgs args, bool isYoung)
        {
            if (Core.ML)
            {
                return m_NewHavenInfo; // We don't get the client Version until AFTER Character creation
            }

            var useHaven = isYoung;

            var flags = args.State?.Flags ?? ClientFlags.None;
            var m = args.Mobile;

            switch (args.Profession)
            {
                case 4: // Necro
                    {
                        if ((flags & ClientFlags.Malas) != 0)
                        {
                            return new CityInfo("Umbra", "Mardoth's Tower", 2114, 1301, -50, Map.Malas);
                        }

                        useHaven = true;

                        /*
                         * Unfortunately you are playing on a *NON-Age-Of-Shadows* game
                         * installation and cannot be transported to Malas.
                         * You will not be able to take your new player quest in Malas
                         * without an AOS client.  You are now being taken to the city of
                         * Haven on the Trammel facet.
                         */
                        Timer.StartTimer(BadStartMessageDelay, () => m.SendLocalizedMessage(1062205));

                        break;
                    }
                case 5: // Paladin
                    {
                        return m_NewHavenInfo;
                    }
                case 6: // Samurai
                    {
                        if ((flags & ClientFlags.Tokuno) != 0)
                        {
                            return new CityInfo("Samurai DE", "Haoti's Grounds", 368, 780, -1, Map.Malas);
                        }

                        useHaven = true;

                        /*
                         * Unfortunately you are playing on a *NON-Samurai-Empire* game
                         * installation and cannot be transported to Tokuno.
                         * You will not be able to take your new player quest in Tokuno
                         * without an SE client. You are now being taken to the city of
                         * Haven on the Trammel facet.
                         */
                        Timer.StartTimer(BadStartMessageDelay, () => m.SendLocalizedMessage(1063487));

                        break;
                    }
                case 7: // Ninja
                    {
                        if ((flags & ClientFlags.Tokuno) != 0)
                        {
                            return new CityInfo("Ninja DE", "Enimo's Residence", 414, 823, -1, Map.Malas);
                        }

                        useHaven = true;

                        /*
                         * Unfortunately you are playing on a *NON-Samurai-Empire* game
                         * installation and cannot be transported to Tokuno.
                         * You will not be able to take your new player quest in Tokuno
                         * without an SE client. You are now being taken to the city of
                         * Haven on the Trammel facet.
                         */
                        Timer.StartTimer(BadStartMessageDelay, () => m.SendLocalizedMessage(1063487));

                        break;
                    }
            }

            return useHaven ? m_NewHavenInfo : args.City;
        }

        private static void FixStats(ref int str, ref int dex, ref int intel, int max)
        {
            var vMax = max - 30;

            var vStr = Math.Max(str - 10, 0);
            var vDex = Math.Max(dex - 10, 0);
            var vInt = Math.Max(intel - 10, 0);

            var total = vStr + vDex + vInt;

            if (total == 0 || total == vMax)
            {
                return;
            }

            var scalar = vMax / (double)total;

            vStr = (int)(vStr * scalar);
            vDex = (int)(vDex * scalar);
            vInt = (int)(vInt * scalar);

            FixStat(ref vStr, vStr + vDex + vInt - vMax, vMax);
            FixStat(ref vDex, vStr + vDex + vInt - vMax, vMax);
            FixStat(ref vInt, vStr + vDex + vInt - vMax, vMax);

            str = vStr + 10;
            dex = vDex + 10;
            intel = vInt + 10;
        }

        private static void FixStat(ref int stat, int diff, int max)
        {
            stat = Math.Clamp(stat + diff, 0, max);
        }

        private static void SetStats(Mobile m, NetState state, int str, int dex, int intel)
        {
            var max = state.NewCharacterCreation ? 90 : 80;

            FixStats(ref str, ref dex, ref intel, max);

            if (str is < 10 or > 60 || dex is < 10 or > 60 || intel is < 10 or > 60 || str + dex + intel != max)
            {
                str = 10;
                dex = 10;
                intel = 10;
            }

            m.InitStats(str, dex, intel);
        }

        private static void SetName(Mobile m, string name)
        {
            name = name.Trim();

            if (!NameVerification.Validate(name, 2, 16, true, false, true, 1, NameVerification.SpaceDashPeriodQuote))
            {
                name = "Generic Player";
            }

            m.Name = name;
        }

        private static bool ValidSkills(SkillNameValue[] skills)
        {
            var total = 0;

            for (var i = 0; i < skills.Length; ++i)
            {
                if (skills[i].Value is < 0 or > 50)
                {
                    return false;
                }

                total += skills[i].Value;

                for (var j = i + 1; j < skills.Length; ++j)
                {
                    if (skills[j].Value > 0 && skills[j].Name == skills[i].Name)
                    {
                        return false;
                    }
                }
            }

            return total is 100 or 120;
        }

        private static void SetSkills(Mobile m, SkillNameValue[] skills, int prof)
        {
            if (prof is < 1 or > 7 && !ValidSkills(skills))
            {
                return;
            }

            skills = prof switch
            {
                1 => // Warrior
                    new[]
                    {
                        new SkillNameValue(SkillName.Anatomy, 30), new SkillNameValue(SkillName.Healing, 45),
                        new SkillNameValue(SkillName.Swords, 35), new SkillNameValue(SkillName.Tactics, 50)
                    },
                2 => // Magician
                    new[]
                    {
                        new SkillNameValue(SkillName.EvalInt, 30), new SkillNameValue(SkillName.Wrestling, 30),
                        new SkillNameValue(SkillName.Magery, 50), new SkillNameValue(SkillName.Meditation, 50)
                    },
                3 => // Blacksmith
                    new[]
                    {
                        new SkillNameValue(SkillName.Mining, 30), new SkillNameValue(SkillName.ArmsLore, 30),
                        new SkillNameValue(SkillName.Blacksmith, 50), new SkillNameValue(SkillName.Tinkering, 50)
                    },
                4 => // Necromancer
                    new[]
                    {
                        new SkillNameValue(SkillName.Necromancy, 50), new SkillNameValue(SkillName.Focus, 30),
                        new SkillNameValue(SkillName.SpiritSpeak, 30), new SkillNameValue(SkillName.Swords, 30),
                        new SkillNameValue(SkillName.Tactics, 20)
                    },
                5 => // Paladin
                    new[]
                    {
                        new SkillNameValue(SkillName.Chivalry, 51), new SkillNameValue(SkillName.Swords, 49),
                        new SkillNameValue(SkillName.Focus, 30), new SkillNameValue(SkillName.Tactics, 30)
                    },
                6 => // Samurai
                    new[]
                    {
                        new SkillNameValue(SkillName.Bushido, 50), new SkillNameValue(SkillName.Swords, 50),
                        new SkillNameValue(SkillName.Anatomy, 30), new SkillNameValue(SkillName.Healing, 30)
                    },
                7 => new[] // Ninja
                {
                    new SkillNameValue(SkillName.Ninjitsu, 50), new SkillNameValue(SkillName.Hiding, 50),
                    new SkillNameValue(SkillName.Fencing, 30), new SkillNameValue(SkillName.Stealth, 30)
                },
                _ => skills
            };

            var addSkillItems = true;
            var elf = m.Race == Race.Elf;
            var gargoyle = m.Race == Race.Gargoyle;

            switch (prof)
            {
                case 1: // Warrior
                    {
                        if (elf)
                        {
                            EquipItem(new Circlet());
                            EquipItem(new HideGorget());
                            EquipItem(new HideChest());
                            EquipItem(new HidePauldrons());
                            EquipItem(new HideGloves());
                            EquipItem(new HidePants());
                            EquipItem(new ElvenBoots());
                        }
                        else if (gargoyle)
                        {
                            EquipItem(new DreadSword());
                            EquipItem(m.Female ? new GargishLeatherChestType2() : new GargishLeatherChestType1());
                            EquipItem(m.Female ? new GargishLeatherArmsType2() : new GargishLeatherArmsType1());
                            EquipItem(m.Female ?  new GargishLeatherKiltType2() : new GargishLeatherKiltType1());
                            EquipItem(m.Female ? new GargishLeatherLegsType2() : new GargishLeatherLegsType1());
                        }
                        else
                        {
                            EquipItem(new Bascinet());
                            EquipItem(new StuddedGorget());
                            EquipItem(new StuddedChest());
                            EquipItem(new StuddedArms());
                            EquipItem(new StuddedGloves());
                            EquipItem(new StuddedLegs());
                            EquipItem(new ThighBoots());
                        }
                        break;
                    }
                case 4: // Necromancer
                    {
                        Container regs = new BagOfNecroReagents { LootType = LootType.Regular };

                        if (!Core.AOS)
                        {
                            foreach (var item in regs.Items)
                            {
                                item.LootType = LootType.Newbied;
                            }
                        }

                        PackItem(regs);

                        EquipItem(new BoneHelm());

                        if (elf)
                        {
                            EquipItem(new ElvenMachete());
                            EquipItem(NecroHue(new LeafChest()));
                            EquipItem(NecroHue(new LeafArms()));
                            EquipItem(NecroHue(new LeafGloves()));
                            EquipItem(NecroHue(new LeafGorget()));
                            EquipItem(NecroHue(new LeafLegs()));
                            EquipItem(new ElvenBoots());
                        }
                        else if (gargoyle)
                        {
                            EquipItem(new GlassSword());
                            EquipItem(NecroHue(m.Female ? new GargishLeatherChestType2() : new GargishLeatherChestType1()));
                            EquipItem(NecroHue(m.Female ? new GargishLeatherArmsType2() : new GargishLeatherArmsType1()));
                            EquipItem(NecroHue(m.Female ?  new GargishLeatherKiltType2() : new GargishLeatherKiltType1()));
                            EquipItem(NecroHue(m.Female ? new GargishLeatherLegsType2() : new GargishLeatherLegsType1()));
                        }
                        else
                        {
                            EquipItem(new BoneHarvester());
                            EquipItem(NecroHue(new LeatherChest()));
                            EquipItem(NecroHue(new LeatherArms()));
                            EquipItem(NecroHue(new LeatherGloves()));
                            EquipItem(NecroHue(new LeatherGorget()));
                            EquipItem(NecroHue(new LeatherLegs()));
                            EquipItem(NecroHue(new Skirt()));
                            EquipItem(new Sandals(0x8FD));
                        }

                        // animate dead, evil omen, pain spike, summon familiar, wraith form
                        PackItem(new NecromancerSpellbook(0x8981ul) { LootType = LootType.Blessed });

                        addSkillItems = false;

                        break;
                    }
                case 5: // Paladin
                    {
                        if (elf)
                        {
                            EquipItem(new ElvenMachete());
                            EquipItem(new WingedHelm());
                            EquipItem(new LeafGorget());
                            EquipItem(new LeafArms());
                            EquipItem(new LeafChest());
                            EquipItem(new LeafLegs());
                            EquipItem(new LeafGloves());
                            EquipItem(new ElvenBoots()); // Verify hue
                        }
                        else if (gargoyle)
                        {
                            EquipItem(new GlassSword());
                            EquipItem(m.Female ? new GargishStoneChestType2() : new GargishStoneChestType1());
                            EquipItem(m.Female ? new GargishStoneArmsType2() : new GargishStoneArmsType1());
                            EquipItem(m.Female ? new GargishStoneKiltType2() : new GargishStoneKiltType1());
                            EquipItem(m.Female ? new GargishStoneLegsType2() : new GargishStoneLegsType1());
                        }
                        else
                        {
                            EquipItem(new Broadsword());
                            EquipItem(new Helmet());
                            EquipItem(new PlateGorget());
                            EquipItem(new RingmailArms());
                            EquipItem(new RingmailChest());
                            EquipItem(new RingmailLegs());
                            EquipItem(new RingmailGloves());
                            EquipItem(new ThighBoots(0x748));
                            EquipItem(new Cloak(0xCF));
                            EquipItem(new BodySash(0xCF));
                        }

                        PackItem(new BookOfChivalry { LootType = LootType.Blessed });

                        addSkillItems = false;

                        break;
                    }

                case 6: // Samurai
                    {
                        addSkillItems = false;

                        if (elf)
                        {
                            EquipItem(new RavenHelm());
                            EquipItem(new HakamaShita(0x2C3));
                            EquipItem(new Hakama(0x2C3));
                            EquipItem(new SamuraiTabi(0x2C3));
                            EquipItem(new TattsukeHakama(0x22D));
                            EquipItem(new Bokuto());
                        }
                        else if (gargoyle)
                        {
                            EquipItem(new GargishTalwar());
                            EquipItem(m.Female ? new GargishLeatherChestType2() : new GargishLeatherChestType1());
                            EquipItem(m.Female ? new GargishLeatherArmsType2() : new GargishLeatherArmsType1());
                            EquipItem(m.Female ?  new GargishLeatherKiltType2() : new GargishLeatherKiltType1());
                            EquipItem(m.Female ? new GargishLeatherLegsType2() : new GargishLeatherLegsType1());
                        }
                        else
                        {
                            EquipItem(new LeatherJingasa());
                            EquipItem(new HakamaShita(0x2C3));
                            EquipItem(new Hakama(0x2C3));
                            EquipItem(new SamuraiTabi(0x2C3));
                            EquipItem(new TattsukeHakama(0x22D));
                            EquipItem(new Bokuto());
                        }

                        PackItem(new Scissors());
                        PackItem(new Bandage(50));

                        Spellbook book = new BookOfBushido();
                        PackItem(book);

                        break;
                    }
                case 7: // Ninja
                    {
                        addSkillItems = false;

                        int[] hues = { 0x1A8, 0xEC, 0x99, 0x90, 0xB5, 0x336, 0x89 };
                        // TODO: Verify that's ALL the hues for that above.

                        if (elf)
                        {
                            EquipItem(new AssassinSpike());
                            EquipItem(new TattsukeHakama(hues.RandomElement()));
                            EquipItem(new HakamaShita(0x2C3));
                            EquipItem(new NinjaTabi(0x2C3));
                            EquipItem(new Kasa());
                        }
                        else if (gargoyle)
                        {
                            //EquipItem(new DualPointedSpear()); //IMPLEMENTATION NEEDED
                            EquipItem(m.Female ? new GargishLeatherChestType2() : new GargishLeatherChestType1());
                            EquipItem(m.Female ? new GargishLeatherArmsType2() : new GargishLeatherArmsType1());
                            EquipItem(m.Female ?  new GargishLeatherKiltType2() : new GargishLeatherKiltType1());
                            EquipItem(m.Female ? new GargishLeatherLegsType2() : new GargishLeatherLegsType1());
                        }
                        else
                        {
                            EquipItem(new Tekagi());
                            EquipItem(new TattsukeHakama(hues.RandomElement()));
                            EquipItem(new HakamaShita(0x2C3));
                            EquipItem(new NinjaTabi(0x2C3));
                            EquipItem(new Kasa());
                        }

                        PackItem(new SmokeBomb());
                        PackItem(new SmokeBomb());
                        PackItem(new SmokeBomb());
                        PackItem(new SmokeBomb());
                        PackItem(new SmokeBomb());
                        PackItem(new BookOfNinjitsu());
                        break;
                    }
            }

            for (var i = 0; i < skills.Length; ++i)
            {
                var snv = skills[i];

                if (snv.Value > 0 && (snv.Name != SkillName.Stealth || prof == 7) && snv.Name != SkillName.RemoveTrap &&
                    snv.Name != SkillName.Spellweaving)
                {
                    var skill = m.Skills[snv.Name];

                    if (skill != null)
                    {
                        skill.BaseFixedPoint = snv.Value * 10;

                        if (addSkillItems)
                        {
                            AddSkillItems(snv.Name, m);
                        }
                    }
                }
            }
        }

        private static void EquipItem(Item item, bool mustEquip = false)
        {
            if (item == null)
            {
                return;
            }

            if (!Core.AOS)
            {
                item.LootType = LootType.Newbied;
            }

            if (m_Mobile?.EquipItem(item) == true)
            {
                return;
            }

            var pack = m_Mobile?.Backpack;

            if (!mustEquip && pack != null)
            {
                pack.DropItem(item);
            }
            else
            {
                item.Delete();
            }
        }

        private static void PackItem(Item item)
        {
            if (!Core.AOS)
            {
                item.LootType = LootType.Newbied;
            }

            var pack = m_Mobile.Backpack;

            if (pack != null)
            {
                pack.DropItem(item);
            }
            else
            {
                item.Delete();
            }
        }

        private static void PackInstrument()
        {
            Item instrument = Utility.Random(6) switch
            {
                0 => new Drums(),
                1 => new Harp(),
                2 => new LapHarp(),
                3 => new Lute(),
                4 => new Tambourine(),
                _ => new TambourineTassel()
            };

            PackItem(instrument);
        }

        private static void PackScroll(int circle)
        {
            Item item = (Utility.Random(8) * (circle + 1)) switch
            {
                0  => new ClumsyScroll(),
                1  => new CreateFoodScroll(),
                2  => new FeeblemindScroll(),
                3  => new HealScroll(),
                4  => new MagicArrowScroll(),
                5  => new NightSightScroll(),
                6  => new ReactiveArmorScroll(),
                7  => new WeakenScroll(),
                8  => new AgilityScroll(),
                9  => new CunningScroll(),
                10 => new CureScroll(),
                11 => new HarmScroll(),
                12 => new MagicTrapScroll(),
                13 => new MagicUnTrapScroll(),
                14 => new ProtectionScroll(),
                15 => new StrengthScroll(),
                16 => new BlessScroll(),
                17 => new FireballScroll(),
                18 => new MagicLockScroll(),
                19 => new PoisonScroll(),
                20 => new TelekinesisScroll(),
                21 => new TeleportScroll(),
                22 => new UnlockScroll(),
                _  => new WallOfStoneScroll()
            };

            PackItem(item);
        }

        private static Item NecroHue(Item item)
        {
            item.Hue = 0x2C3;

            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EquipRobe(Mobile m, int hue)
        {
            if (Race.IsAllowedRace(m.Race, Race.AllowElvesOnly))
            {
                EquipItem(m.Female ? new FemaleElvenRobe(hue) : new MaleElvenRobe(hue));
                return;
            }

            EquipItem(new Robe(hue));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Item SwordsWeapon(int raceFlag) =>
            raceFlag switch
            {
                Race.AllowElvesOnly     => new RuneBlade(),
                Race.AllowGargoylesOnly => new DreadSword(),
                _                       => new Katana()
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Item MacingWeapon(int raceFlag) =>
            raceFlag switch
            {
                Race.AllowElvesOnly     => new DiamondMace(),
                Race.AllowGargoylesOnly => null, // new DiscMace(),
                _                       => new Club()
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Item FencingWeapon(int raceFlag) =>
            raceFlag switch
            {
                Race.AllowElvesOnly     => new Leafblade(),
                Race.AllowGargoylesOnly => null, // new BloodBlade(),
                _                       => new Kryss()
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Item StaffWeapon(int raceFlag) =>
            raceFlag switch
            {
                Race.AllowElvesOnly     => new WildStaff(),
                Race.AllowGargoylesOnly => null, // new SerpentStoneStaff(),
                _                       => new GnarledStaff()
            };

        private static void AddSkillItems(SkillName skill, Mobile m)
        {
            var raceFlag = m.Race.RaceFlag;
            var human = m.Race == Race.Human;
            var elf = m.Race == Race.Elf;
            var gargoyle = m.Race == Race.Gargoyle;
            var elfOrHuman = elf || human;

            switch (skill)
            {
                case SkillName.Alchemy:
                    {
                        PackItem(new Bottle(4));
                        PackItem(new MortarPestle());
                        EquipRobe(m, Utility.RandomPinkHue());

                        break;
                    }
                case SkillName.Anatomy:
                    {
                        PackItem(new Bandage(3));
                        EquipRobe(m, Utility.RandomYellowHue());

                        break;
                    }
                case SkillName.AnimalLore:
                    {
                        EquipItem(elf ? new WildStaff() : new ShepherdsCrook());
                        EquipRobe(m, Utility.RandomBlueHue());

                        break;
                    }
                case SkillName.Archery:
                    {
                        PackItem(new Arrow(25));

                        if (elf)
                        {
                            EquipItem(new ElvenCompositeLongbow());
                        }
                        else if (human)
                        {
                            EquipItem(new Bow());
                        }

                        break;
                    }
                case SkillName.ArmsLore:
                    {
                        Item item = Utility.Random(3) switch
                        {
                            0 => FencingWeapon(raceFlag),
                            1 => MacingWeapon(raceFlag),
                            _ => SwordsWeapon(raceFlag)
                        };
                        EquipItem(item);

                        break;
                    }
                case SkillName.Begging:
                    {
                        EquipItem(StaffWeapon(raceFlag));
                        break;
                    }
                case SkillName.Blacksmith:
                    {
                        PackItem(new Tongs());
                        PackItem(new Pickaxe());
                        PackItem(new Pickaxe());
                        PackItem(new IronIngot(50));
                        EquipItem(new HalfApron(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.Bushido:
                    {
                        if (elfOrHuman)
                        {
                            EquipItem(new Hakama());
                            EquipItem(new Kasa());
                        }

                        EquipItem(new BookOfBushido());
                        break;
                    }
                case SkillName.Fletching:
                    {
                        PackItem(new Board(14));
                        PackItem(new Feather(5));
                        PackItem(new Shaft(5));
                        break;
                    }
                case SkillName.Camping:
                    {
                        PackItem(new Bedroll());
                        PackItem(new Kindling(5));
                        break;
                    }
                case SkillName.Carpentry:
                    {
                        PackItem(new Board(10));
                        PackItem(new Saw());

                        if (elfOrHuman)
                        {
                            EquipItem(new HalfApron(Utility.RandomYellowHue()));
                        }

                        break;
                    }
                case SkillName.Cartography:
                    {
                        PackItem(new BlankMap());
                        PackItem(new BlankMap());
                        PackItem(new BlankMap());
                        PackItem(new BlankMap());
                        PackItem(new Sextant());
                        break;
                    }
                case SkillName.Cooking:
                    {
                        PackItem(new Kindling(2));
                        PackItem(new RawLambLeg());
                        PackItem(new RawChickenLeg());
                        PackItem(new RawFishSteak());
                        PackItem(new SackFlour());
                        PackItem(new Pitcher(BeverageType.Water));
                        break;
                    }
                case SkillName.Chivalry:
                    {
                        if (Core.ML)
                        {
                            PackItem(new BookOfChivalry());
                        }

                        break;
                    }
                case SkillName.DetectHidden:
                    {
                        if (elfOrHuman)
                        {
                            EquipItem(new Cloak(0x455));
                        }

                        break;
                    }
                case SkillName.Discordance:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Fencing:
                    {
                        EquipItem(FencingWeapon(raceFlag));
                        break;
                    }
                case SkillName.Fishing:
                    {
                        EquipItem(new FishingPole());

                        var hue = Utility.RandomYellowHue();
                        if (elf)
                        {
                            EquipItem(new Circlet { Hue = hue });

                        }
                        else if (human)
                        {
                            EquipItem(new FloppyHat(hue));
                        }

                        break;
                    }
                case SkillName.Healing:
                    {
                        PackItem(new Bandage(50));
                        PackItem(new Scissors());
                        break;
                    }
                case SkillName.Herding:
                    {
                        EquipItem(elf ? new WildStaff() : new ShepherdsCrook());
                        break;
                    }
                case SkillName.Hiding:
                    {
                        if (elfOrHuman)
                        {
                            EquipItem(new Cloak(0x455));
                        }

                        break;
                    }
                case SkillName.Inscribe:
                    {
                        PackItem(new BlankScroll(2));
                        PackItem(new BlueBook());
                        break;
                    }
                case SkillName.ItemID:
                    {
                        EquipItem(StaffWeapon(raceFlag));
                        break;
                    }
                case SkillName.Lockpicking:
                    {
                        PackItem(new Lockpick(20));
                        break;
                    }
                case SkillName.Lumberjacking:
                    {
                        EquipItem(elfOrHuman ? new Hatchet() : null); // new DualShortAxes()
                        break;
                    }
                case SkillName.Macing:
                    {
                        EquipItem(MacingWeapon(raceFlag));
                        break;
                    }
                case SkillName.Magery:
                    {
                        var regs = new BagOfReagents(30) { LootType = LootType.Regular };

                        if (!Core.AOS)
                        {
                            foreach (var item in regs.Items)
                            {
                                item.LootType = LootType.Newbied;
                            }
                        }

                        PackItem(regs);

                        PackScroll(0);
                        PackScroll(1);
                        PackScroll(2);

                        EquipItem(new Spellbook(0x382A8C38ul) { LootType = LootType.Blessed });
                        EquipRobe(m, Utility.RandomBlueHue());

                        if (elf)
                        {
                            EquipItem(new Circlet());
                        }
                        else if (human)
                        {
                            EquipItem(new WizardsHat());
                        }
                        break;
                    }
                case SkillName.Mining:
                    {
                        PackItem(new Pickaxe());
                        break;
                    }
                case SkillName.Musicianship:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Necromancy:
                    {
                        if (Core.ML)
                        {
                            PackItem(new BagOfNecroReagents { LootType = LootType.Regular });
                        }

                        break;
                    }
                case SkillName.Ninjitsu:
                    {
                        if (elfOrHuman)
                        {
                            EquipItem(new Hakama(0x2C3)); // Only ninjas get the hued one.
                            EquipItem(new Kasa());
                        }

                        EquipItem(new BookOfNinjitsu());
                        break;
                    }
                case SkillName.Parry:
                    {
                        if (gargoyle)
                        {
                            // EquipItem(new GargishWoodenShield());
                        }
                        else
                        {
                            EquipItem(new WoodenShield());
                        }

                        break;
                    }
                case SkillName.Peacemaking:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Poisoning:
                    {
                        PackItem(new LesserPoisonPotion());
                        PackItem(new LesserPoisonPotion());
                        break;
                    }
                case SkillName.Provocation:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Snooping:
                    {
                        PackItem(new Lockpick(20));
                        break;
                    }
                case SkillName.SpiritSpeak:
                    {
                        EquipItem(new Cloak(0x455));
                        break;
                    }
                case SkillName.Stealing:
                    {
                        PackItem(new Lockpick(20));
                        break;
                    }
                case SkillName.Tactics:
                case SkillName.Swords:
                    {
                        EquipItem(SwordsWeapon(raceFlag));

                        break;
                    }
                case SkillName.Tailoring:
                    {
                        PackItem(new BoltOfCloth());
                        PackItem(new SewingKit());
                        break;
                    }
                case SkillName.Tracking:
                    {
                        if (elfOrHuman)
                        {
                            // Delete shoes
                            m_Mobile?.FindItemOnLayer(Layer.Shoes)?.Delete();

                            var hue = Utility.RandomYellowHue();
                            EquipItem(elf ? new ElvenBoots(hue) : new Boots(hue));
                        }

                        EquipItem(new SkinningKnife());
                        break;
                    }
                case SkillName.Veterinary:
                    {
                        PackItem(new Bandage(5));
                        PackItem(new Scissors());
                        break;
                    }
                case SkillName.Wrestling:
                    {
                        Item item = raceFlag switch
                        {
                            Race.AllowElvesOnly                   => new LeafGloves(),
                            Race.AllowGargoylesOnly when m.Female => new GargishLeatherArmsType2(),
                            Race.AllowGargoylesOnly               => new GargishLeatherArmsType1(),
                            _                                     => new LeatherGloves()
                        };
                        EquipItem(item);
                        break;
                    }
                case SkillName.Throwing:
                    {
                        if (gargoyle)
                        {
                            // EquipItem(new Boomerang());
                        }

                        break;
                    }
                case SkillName.Mysticism:
                    {
                        // PackItem(new MysticBook(0xAB));
                        break;
                    }
            }
        }
    }
}
