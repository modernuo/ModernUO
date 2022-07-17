using System;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions
{
    public class Seasons : MLQuest
    {
        public Seasons()
        {
            Activated = true;
            Title = 1072782; // Seasons
            Description =
                1072802; // *rumbling growl* *sniff* ... not-smell ... seek-fight ... not-smell ... fear-stench ... *rumble* ... cold-soon-time comes ... hungry ... eat-fish ... sleep-soon-time ... *deep fang-filled yawn* ... much-fish.
            RefusalMessage = 1072810; // *yawn* ... cold-soon-time ... *growl*
            InProgressMessage = 1072811; // *sniff* *sniff* ... not-much-fish ... hungry ... *grumble*
            CompletionMessage = 1074174; // *sniff* fish! much-fish!

            Objectives.Add(new CollectObjective(20, typeof(RawFishSteak), 1022426)); // raw fish steak

            Rewards.Add(new DummyReward(1072803)); // The boon of Maul.
        }

        public override bool RecordCompletion => true;

        public override void GetRewards(MLQuestInstance instance)
        {
            // You have gained the boon of Maul!  Your understanding of the seasons grows.
            // You are one step closer to claiming your elven heritage.
            instance.Player.SendLocalizedMessage(1074940, "", 0x2A);
            instance.ClaimRewards(); // skip gump
        }
    }

    public class CaretakerOfTheLand : MLQuest
    {
        public CaretakerOfTheLand()
        {
            Activated = true;
            Title = 1072783; // Caretaker of the Land
            Description =
                1072812; // Hrrrrr.  Hurrrr.  Huuuman.  *creaking branches*  Suuun on baaark, roooooots diiig deeeeeep, wiiind caaaresses leeeaves … Hrrrrr.  Saaap of Sooosaria feeeeeeds us.  Hrrrrr.  Huuuman leeearn.  Caaaretaker of plaaants … teeend … prooove.<br>
            RefusalMessage = 1072813; // Hrrrrr.  Hrrrrr.  Huuuman.
            InProgressMessage =
                1072814; // Hrrrr. Hrrrr.  Roooooots neeeeeed saaap of Sooosaria.  Hrrrrr.  Roooooots tiiingle neeeaaar Yeeew.  Seeeaaarch.  Hrrrr!
            CompletionMessage = 1074175; // Thiiirsty. Hurrr. Hurrr.

            Objectives.Add(new CollectObjective(1, typeof(SapOfSosaria), "sap of sosaria"));

            Rewards.Add(new DummyReward(1072804)); // The boon of Strongroot.
        }

        public override bool RecordCompletion => true;

        public override void GetRewards(MLQuestInstance instance)
        {
            // You have gained the boon of Strongroot!  You have been approved by one whose roots touch the bones of Sosaria.
            // You are one step closer to claiming your elven heritage.
            instance.Player.SendLocalizedMessage(1074941, "", 0x2A);
            instance.ClaimRewards(); // skip gump
        }
    }

    public class WisdomOfTheSphynx : MLQuest
    {
        public WisdomOfTheSphynx()
        {
            Activated = true;
            Title = 1072784; // Wisdom of the Sphynx
            Description =
                1072822; // I greet thee human and divine my boon thou seek.  Convey hence the object of my riddle and I shall reward thee with thy desire.<br><br>Three lives have I.<br>Gentle enough to soothe the skin,<br>Light enough to caress the sky,<br>Hard enough to crack rocks<br>What am I?
            RefusalMessage = 1072823; // As thou wish, human.
            InProgressMessage =
                1072824; // I give thee a hint then human.  The answer to my riddle must be held carefully or it cannot be contained at all.  Bring this elusive item to me in a suitable container.
            CompletionMessage = 1074176; // Ah, thus it ends.

            Objectives.Add(new InternalObjective());

            Rewards.Add(new DummyReward(1072805)); // The boon of Enigma.
        }

        public override bool RecordCompletion => true;

        public override void GetRewards(MLQuestInstance instance)
        {
            // You have gained the boon of Enigma!  You are wise enough to know how little you know.
            // You are one step closer to claiming your elven heritage.
            instance.Player.SendLocalizedMessage(1074945, "", 0x2A);
            instance.ClaimRewards(); // skip gump
        }

        private class InternalObjective : CollectObjective
        {
            public InternalObjective()
                : base(1, typeof(Pitcher), 1074869) // The answer to the riddle.
            {
            }

            public override bool ShowDetailed => false;

            public override bool CheckItem(Item item) =>
                item is Pitcher pitcher && pitcher.Content == BeverageType.Water && pitcher.Quantity > 0;
        }
    }

    public class DefendingTheHerd : MLQuest
    {
        public DefendingTheHerd()
        {
            Activated = true;
            Title = 1072785; // Defending the Herd
            Description =
                1072825; // *snort* ... guard-mates ... guard-herd *hoof stomp* ... defend-with-hoof-and-horn ... thirsty-drink.  *proud head-toss*
            RefusalMessage = 1072826; // *snort*
            InProgressMessage = 1072827; // *impatient hoof stomp* ... thirsty herd ... water scent.
            CompletionNotice = CompletionNoticeShort;

            Objectives.Add(
                new EscortObjective(new QuestArea(1074779, "Bravehorn's drinking pool"))
            );

            Rewards.Add(new DummyReward(1072806)); // The boon of Bravehorn.
        }

        public override bool RecordCompletion => true;

        public override void GetRewards(MLQuestInstance instance)
        {
            // You have gained the boon of Bravehorn!
            // You have glimpsed the nobility of those that sacrifice themselves for their people.
            // You are one step closer to claiming your elven heritage.
            instance.Player.SendLocalizedMessage(1074942, "", 0x2A);
            instance.ClaimRewards(); // skip gump
        }
    }

    public class TheBalanceOfNature : MLQuest
    {
        public TheBalanceOfNature()
        {
            Activated = true;
            Title = 1072786; // The Balance of Nature
            Description =
                1072829; // Ho, there human.  Why do you seek out the Huntsman?  The hunter serves the land by culling both predators and prey.  The hunter maintains the essential balance of life and does not kill for sport or glory.  If you seek my favor, human, then demonstrate you are capable of the duty.  Cull the wolves nearby.
            RefusalMessage = 1072830; // Then begone. I have no time to waste on you, human.
            InProgressMessage = 1072831; // The timber wolves are easily tracked, human.

            Objectives.Add(
                new KillObjective(
                    15,
                    new[] { typeof(TimberWolf) },
                    "timber wolves",
                    new QuestArea(1074833, "Huntsman's Forest")
                )
            );

            Rewards.Add(new DummyReward(1072807)); // The boon of the Huntsman.
        }

        public override bool RecordCompletion => true;

        public override void GetRewards(MLQuestInstance instance)
        {
            // You have gained the boon of the Huntsman!
            // You have been given a taste of the bittersweet duty of those who guard the balance.
            // You are one step closer to claiming your elven heritage.
            instance.Player.SendLocalizedMessage(1074943, "", 0x2A);
            instance.ClaimRewards(); // skip gump
        }
    }

    public class TheJoysOfLife : MLQuest
    {
        public TheJoysOfLife()
        {
            Activated = true;
            Title = 1072787; // The Joys of Life
            Description =
                1072832; // *giggle*  So serious, so grim!  *tickle*  Enjoy life!  Have fun!  Laugh!  Be merry!  *giggle*  Find three of my baubles ... *giggle* I hid them! *giggles hysterically*  Hid them!  La la la!  Bring them quickly!  They are magical and will hide themselves again if you are too slow.
            RefusalMessage = 1072833; // *giggle* Too serious.  Too thinky!
            InProgressMessage = 1072834; // Magical baubles hidden, find them as you're bidden!  *giggle*
            CompletionMessage = 1074177; // *giggle* So pretty!

            Objectives.Add(new CollectObjective(3, typeof(ABauble), "arielle's baubles"));

            Rewards.Add(new DummyReward(1072809)); // The boon of Arielle.
        }

        public override bool RecordCompletion => true;

        public override void GetRewards(MLQuestInstance instance)
        {
            // You have gained the boon of Arielle!
            // You have been taught the importance of laughter and light spirits.
            // You are one step closer to claiming your elven heritage.
            instance.Player.SendLocalizedMessage(1074944, "", 0x2A);
            instance.ClaimRewards(); // skip gump
        }
    }

    [QuesterName("Maul")]
    public class MaulTheBear : GrizzlyBear
    {
        [Constructible]
        public MaulTheBear()
        {
            AI = AIType.AI_Vendor;
            FightMode = FightMode.None;
            Tamable = false;
        }

        public MaulTheBear(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Maul";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Strongroot : Treefellow
    {
        [Constructible]
        public Strongroot()
        {
            AI = AIType.AI_Vendor;
            FightMode = FightMode.None;
        }

        public Strongroot(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Strongroot";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Enigma : BaseCreature
    {
        [Constructible]
        public Enigma() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Body = 788;
            BaseSoundID = 0x3EE;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);
        }

        public Enigma(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Enigma";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Bravehorn : BaseEscortable
    {
        [Constructible]
        public Bravehorn()
        {
        }

        public Bravehorn(Serial serial)
            : base(serial)
        {
        }

        public override bool StaticMLQuester => true;
        public override bool InitialInnocent => true;
        public override string DefaultName => "Bravehorn";

        public override void InitBody()
        {
            Body = 0xEA;

            SetStr(41, 71);
            SetDex(47, 77);
            SetInt(27, 57);

            SetHits(27, 41);
            SetMana(0);

            SetDamage(5, 9);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Cold, 5, 10);

            SetSkill(SkillName.MagicResist, 26.8, 44.5);
            SetSkill(SkillName.Tactics, 29.8, 47.5);
            SetSkill(SkillName.Wrestling, 29.8, 47.5);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 24;
        }

        public override void InitOutfit()
        {
        }

        public override int GetAttackSound() => 0x82;

        public override int GetHurtSound() => 0x83;

        public override int GetDeathSound() => 0x84;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class BravehornsMate : Hind
    {
        [Constructible]
        public BravehornsMate() => Tamable = false;

        public BravehornsMate(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "bravehorn's mate";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Huntsman : Centaur
    {
        [Constructible]
        public Huntsman()
        {
            AI = AIType.AI_Vendor;
            FightMode = FightMode.None;
        }

        public Huntsman(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Huntsman";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Arielle : Pixie
    {
        [Constructible]
        public Arielle()
        {
            AI = AIType.AI_Vendor;
            FightMode = FightMode.None;
        }

        public Arielle(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Arielle";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Ingenuity : MLQuest
    {
        public Ingenuity()
        {
            Activated = true;
            Title = 1074350; // Ingenuity
            Description =
                1074462; // The best thing about my job is that I do a little bit of everything, every day.  It's what we're good at really.  Just picking up something and making it do something else.  Listen, I'm really low on parts.  Are you interested in fetching me some supplies?
            RefusalMessage = 1074508; // Okay.  Best of luck with your other endeavors.
            InProgressMessage =
                1074509; // Lord overseers are the best source I know for power crystals of the type I need.  Iron golems too, can have them but they're harder to find.
            CompletionMessage =
                1074510; // Do you have those power crystals?  I'm ready to put the finishing touches on my latest experiment.
            CompletionNotice = CompletionNoticeShortReturn;

            Objectives.Add(new CollectObjective(10, typeof(PowerCrystal), "Power Crystals"));

            Rewards.Add(new DummyReward(1074875)); // Another step closer to becoming human.
        }

        public override bool RecordCompletion => true;

        public override void GetRewards(MLQuestInstance instance)
        {
            // You have demonstrated your ingenuity!
            // Humans are jacks of all trades and know a little about a lot of things.
            // You are one step closer to achieving humanity.
            instance.Player.SendLocalizedMessage(1074946, "", 0x2A);
            instance.ClaimRewards(); // skip gump
        }
    }

    public class HeaveHo : MLQuest
    {
        public HeaveHo()
        {
            Activated = true;
            Title = 1074351; // Heave Ho!
            Description =
                1074519; // Ho there!  There's nothing quite like a day's honest labor to make you appreciate being alive.  Hey, maybe you'd like to help out with this project?  These crates need to be delivered to Sledge.  The only thing is -- it's a bit of a rush job and if you don't make it in time, he won't take them.  Can I trust you to help out?
            RefusalMessage = 1074521; // Oh yah, if you're too busy, no problem.
            InProgressMessage =
                1074522; // Sledge can be found in Buc's Den.  Better hurry, he won't take those crates if you take too long with them.
            CompletionMessage = 1074523; // Hey, if you have cargo for me, you can start unloading over here.
            CompletionNotice = CompletionNoticeShort;

            Objectives.Add(
                new TimedDeliverObjective(
                    TimeSpan.FromHours(1),
                    typeof(CrateForSledge),
                    5,
                    "Crates for Sledge",
                    typeof(Sledge)
                )
            );

            Rewards.Add(new DummyReward(1074875)); // Another step closer to becoming human.
        }

        public override bool RecordCompletion => true;

        public override void GetRewards(MLQuestInstance instance)
        {
            // You have demonstrated your physical strength!
            // Humans can carry vast loads without complaint.  You are one step closer to achieving humanity.
            instance.Player.SendLocalizedMessage(1074948, "", 0x2A);
            instance.ClaimRewards(); // skip gump
        }
    }

    // This is not a real quest, it is only used as a reference
    public class HumanInNeed : MLQuest
    {
        public HumanInNeed()
        {
            Title = 1075011; // A quest that asks you to defend a human in need.
            Description = 0;
            RefusalMessage = 0;
            InProgressMessage = 0;
        }

        public override bool RecordCompletion => true;

        public static void AwardTo(PlayerMobile pm)
        {
            MLQuestSystem.GetOrCreateContext(pm).SetDoneQuest(MLQuestSystem.FindQuest(typeof(HumanInNeed)));
            // You have demonstrated your compassion!  Your kind actions have been noted.
            pm.SendLocalizedMessage(1074949, "", 0x2A);
        }
    }

    public class AllSeasonAdventurer : MLQuest
    {
        public AllSeasonAdventurer()
        {
            Activated = true;
            Title = 1074353; // All Season Adventurer
            Description =
                1074527; // It's all about hardship, suffering, struggle and pain.  Without challenges, you've got nothing to test yourself against -- and that's what life is all about.  Self improvement!  Honing your body and mind!  Overcoming obstacles ... You'll see what I mean if you take on my challenge.
            RefusalMessage = 1074528; // My way of life isn't for everyone, that's true enough.
            InProgressMessage = 1074529; // You're not making much progress in the honing-mind-and-body department, are you?
            CompletionNotice = CompletionNoticeShortReturn;

            Objectives.Add(
                new KillObjective(
                    5,
                    new[] { typeof(Efreet) },
                    "efreets",
                    new QuestArea(1074808, "Fire")
                )
            );
            Objectives.Add(
                new KillObjective(
                    5,
                    new[] { typeof(IceFiend) },
                    "ice fiends",
                    new QuestArea(1074809, "Ice")
                )
            );

            Rewards.Add(new DummyReward(1074875)); // Another step closer to becoming human.
        }

        public override bool RecordCompletion => true;

        public override void GetRewards(MLQuestInstance instance)
        {
            // You have demonstrated your toughness!
            // Humans are able to endure unimaginable hardships in pursuit of their goals.
            // You are one step closer to achieving humanity.
            instance.Player.SendLocalizedMessage(1074947, "", 0x2A);
            instance.ClaimRewards(); // skip gump
        }
    }

    [QuesterName("Sledge (Buc's Den)")]
    public class Sledge : BaseCreature
    {
        [Constructible]
        public Sledge() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Versatile";
            Body = 400;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            AddItem(new Tunic(Utility.RandomNeutralHue()));
            AddItem(new LongPants(Utility.RandomBlueHue()));
            AddItem(new Cloak(Utility.RandomBrightHue()));
            AddItem(new ElvenBoots(Utility.RandomNeutralHue()));
            AddItem(new Backpack());
        }

        public Sledge(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Sledge";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(
                this,
                pm,
                Utility.RandomList(
                    1074188, // Weakling! You are not up to the task I have.
                    1074195  // You there, in the stupid hat!  Come here.
                )
            );
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    [QuesterName("Patricus (Vesper)")]
    public class Patricus : BaseCreature
    {
        [Constructible]
        public Patricus() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Trader";
            Body = 400;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            AddItem(new FancyShirt(Utility.RandomNeutralHue()));
            AddItem(new LongPants(Utility.RandomBrightHue()));
            AddItem(new Cloak(0x1BB));
            AddItem(new Shoes(Utility.RandomNeutralHue()));
            AddItem(new Backpack());
        }

        public Patricus(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Patricus";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    [QuesterName("Belulah (Nujel'm)")] // On OSI it's "Belulah (Nu'Jelm)" (incorrect spelling)
    public class Belulah : BaseCreature
    {
        [Constructible]
        public Belulah() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the scorned";
            Female = true;
            Body = 401;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new FancyShirt(Utility.RandomBlueHue()));
            AddItem(new LongPants(Utility.RandomNondyedHue()));
            AddItem(new Boots());
        }

        public Belulah(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Belulah";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            /*
             * 1074205 - Oh great adventurer, would you please assist a weak soul in need of aid?
             * 1074206 - Excuse me please traveler, might I have a little of your time?
             */
            MLQuestSystem.Tell(this, pm, Utility.Random(1074205, 2));
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
