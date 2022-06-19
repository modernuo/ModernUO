using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions
{
    public class SplitEnds : MLQuest
    {
        public SplitEnds()
        {
            Activated = true;
            HasRestartDelay = true;
            Title = 1075506; // Split Ends
            Description =
                1075507; // *sighs* I think bowcrafting is a might beyond my talents. Say there, you look a bit more confident with tools. Can I persuade thee to make a few arrows? You could have my satchel in return... 'tis useless to me! You'll need a fletching kit to start, some feathers, and a few arrow shafts. Just use the fletching kit while you have the other things, and I'm sure you'll figure out the rest.
            RefusalMessage = 1075508; // Oh. Well. I'll just keep trying alone, I suppose...
            InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!
            CompletionMessage = 1072272; // Thanks for helping me out.  Here's the reward I promised you.

            Objectives.Add(new CollectObjective(20, typeof(Arrow), 1023902)); // arrow

            Rewards.Add(new ItemReward(1074282, typeof(AndricSatchel))); // Craftsmans's Satchel
        }
    }

    public class IShotAnArrowIntoTheAir : MLQuest
    {
        public IShotAnArrowIntoTheAir()
        {
            Activated = true;
            Title = 1075486; // I Shot an Arrow Into the Air...
            Description =
                1075482; // Truth be told, the only way to get a feel for the bow is to shoot one and there's no better practice target than a sheep. If ye can shoot ten of them I think ye will have proven yer abilities. Just grab a bow and make sure to take enough ammunition. Bows tend to use arrows and crossbows use bolts. Ye can buy 'em or have someone craft 'em. How about it then? Come back here when ye are done.
            RefusalMessage = 1075483; // Fair enough, the bow isn't for everyone. Good day then.
            InProgressMessage = 1075484; // Return once ye have killed ten sheep with a bow and not a moment before.

            Objectives.Add(new KillObjective(10, new[] { typeof(Sheep) }, 1018270)); // sheep

            Rewards.Add(ItemReward.BagOfTrinkets);
        }
    }

    public class BakersDozen : MLQuest
    {
        public BakersDozen()
        {
            Activated = true;
            HasRestartDelay = true;
            Title = 1075478; // Baker's Dozen
            Description =
                1075479; // You there! Do you know much about the ways of cooking? If you help me out, I'll show you a thing or two about how it's done. Bring me some cookie mix, about 5 batches will do it, and I will reward you. Although, I don't think you can buy it, you can make some in a snap! First get a rolling pin or frying pan or even a flour sifter. Then you mix one pinch of flour with some water and you've got some dough! Take that dough and add one dollop of honey and you've got sweet dough. add one more drop of honey and you've got cookie mix. See? Nothing to it! Now get to work!
            RefusalMessage =
                1075480; // Argh, I absolutely must have more of these 'cookies!' Come back if you change your mind.
            InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!
            CompletionMessage = 1075481; // Thank you! I haven't been this excited about food in months!

            Objectives.Add(new CollectObjective(5, typeof(CookieMix), 1024159)); // cookie mix

            Rewards.Add(new ItemReward(1074282, typeof(AsandosSatchel))); // Craftsmans's Satchel
        }
    }

    public class AStitchInTime : MLQuest
    {
        public AStitchInTime()
        {
            Activated = true;
            HasRestartDelay = true;
            Title = 1075523; // A Stitch in Time
            Description =
                1075522; // Oh how I wish I had a fancy dress like the noble ladies of Castle British! I don't have much... but I have a few trinkets I might trade for it. It would mean the world to me to go to a fancy ball and dance the night away. Oh, and I could tell you how to make one! You just need to use your sewing kit on enough cut cloth, that's all.
            RefusalMessage = 1075526; // Won't you reconsider? It'd mean the world to me, it would!
            InProgressMessage =
                1075527; // Hello again! Do you need anything? You may want to visit the tailor's shop for cloth and a sewing kit, if you don't already have them.
            CompletionMessage =
                1075528; // It's gorgeous! I only have a few things to give you in return, but I can't thank you enough! Maybe I'll even catch Uzeraan's eye at the, er, *blushes* I mean, I can't wait to wear it to the next town dance!

            Objectives.Add(new CollectObjective(1, typeof(FancyDress), 1027935)); // fancy dress

            Rewards.Add(new ItemReward(1075524, typeof(AnOldRing)));     // an old ring
            Rewards.Add(new ItemReward(1075525, typeof(AnOldNecklace))); // an old necklace
        }
    }

    public class BatteredBucklers : MLQuest
    {
        public BatteredBucklers()
        {
            Activated = true;
            HasRestartDelay = true;
            Title = 1075511; // Battered Bucklers
            Description =
                1075512; // Hey there! Yeah... you! Ya' any good with a hammer? Tell ya what, if yer thinking about tryin' some metal work, and have a bit of skill, I can show ya how to bend it into shape. Just get some of those ingots there, and grab a hammer and use it over here at this forge. I need a few more bucklers hammered out to fill this here order with...  hmmm about ten more. that'll give some taste of how to work the metal.
            RefusalMessage =
                1075514; // Not enough muscle on yer bones to use it? hmph, probably afraid of the sparks markin' up yer loverly skin... to good for some honest labor... ha!... off with ya!
            InProgressMessage = 1075515; // Come On! Whats that... a bucket? We need ten bucklers... not spitoons.
            CompletionMessage = 1075516; // Thanks for the help. Here's something for ya to remember me by.

            Objectives.Add(new CollectObjective(10, typeof(Buckler), 1027027)); // buckler

            Rewards.Add(new ItemReward(1074282, typeof(GervisSatchel))); // Craftsmans's Satchel
        }
    }

    public class MoreOrePlease : MLQuest
    {
        public MoreOrePlease()
        {
            Activated = true;
            HasRestartDelay = true;
            Title = 1075530; // More Ore Please
            Description =
                1075529; // Have a pickaxe? My supplier is late and I need some iron ore so I can complete a bulk order for another merchant. If you can get me some soon I'll pay you double what it's worth on the market. Just find a cave or mountainside and try to use your pickaxe there, maybe you'll strike a good vein! 5 large pieces should do it.
            RefusalMessage =
                1075531; // Not feeling strong enough today? Its alright, I didn't need a bucket of rocks anyway.
            InProgressMessage = 1075532; // Hmmm' we need some more Ore. Try finding a mountain or cave, and give it a whack.
            CompletionMessage =
                1075533; // I see you found a good vien! Great!  This will help get this order out on time. Good work!

            Objectives.Add(new InternalObjective());

            Rewards.Add(new ItemReward(1074282, typeof(MuggSatchel))); // Craftsmans's Satchel
        }

        private class InternalObjective : CollectObjective
        {
            // Any type of ore is allowed
            public InternalObjective()
                : base(5, typeof(BaseOre), 1026585) // ore
            {
            }

            public override bool CheckItem(Item item) => item.ItemID == 6585;
        }
    }

    public class ComfortableSeating : MLQuest
    {
        public ComfortableSeating()
        {
            Activated = true;
            HasRestartDelay = true;
            Title = 1075517; // Comfortable Seating
            Description =
                1075518; // Hail friend, hast thou a moment? A mishap with a saw hath left me in a sorry state, for it shall be a while before I canst return to carpentry. In the meantime, I need a comfortable chair that I may rest. Could thou craft a straw chair?  Only a tool, such as a dovetail saw, a few boards, and some skill as a carpenter is needed. Remember, this is a piece of furniture, so please pay attention to detail.
            RefusalMessage = 1072687; // I quite understand your reluctance.  If you reconsider, I'll be here.
            InProgressMessage = 1075509; // Is all going well? I look forward to the simple comforts in my very own home.
            CompletionMessage = 1074720; // This is perfect!

            Objectives.Add(new CollectObjective(1, typeof(BambooChair), "straw chair"));

            Rewards.Add(new ItemReward(1074282, typeof(LowelSatchel))); // Craftsmans's Satchel
        }
    }

    public class ThePenIsMightier : MLQuest
    {
        public ThePenIsMightier()
        {
            Activated = true;
            HasRestartDelay = true;
            Title = 1075542; // The Pen is Mightier
            Description =
                1075543; // Do you know anything about 'Inscription?' I've been trying to get my hands on some hand crafted Recall scrolls for a while now, and I could really use some help. I don't have a scribe's pen, let alone a spellbook with Recall in it, or blank scrolls, so there's no way I can do it on my own. How about you though? I could trade you one of my old leather bound books for some.
            RefusalMessage =
                1075546; // Hmm, thought I had your interest there for a moment. It's not everyday you see a book made from real daemon skin, after all!
            InProgressMessage =
                1075547; // Inscribing... yes, you'll need a scribe's pen, some reagents, some blank scroll, and of course your own magery book. You might want to visit the magery shop if you're lacking some materials.
            CompletionMessage =
                1075548; // Ha! Finally! I've had a rune to the waterfalls near Justice Isle that I've been wanting to use for the longest time, and now I can visit at last. Here's that book I promised you... glad to be rid of it, to be honest.

            Objectives.Add(new CollectObjective(5, typeof(RecallScroll), "recall scroll"));

            Rewards.Add(new ItemReward(1075545, typeof(RedLeatherBook))); // a book bound in red leather
        }
    }

    public class AClockworkPuzzle : MLQuest
    {
        public AClockworkPuzzle()
        {
            Activated = true;
            HasRestartDelay = true;
            Title = 1075535; // A clockwork puzzle
            Description =
                1075534; // 'Tis a riddle, you see! "What kind of clock is only right twice per day? A broken one!" *laughs heartily* Ah, yes *wipes eye*, that's one of my favorites! Ah... to business. Could you fashion me some clock parts? I wish my own clocks to be right all the day long! You'll need some tinker's tools and some iron ingots, I think, but from there it should be just a matter of working the metal.
            RefusalMessage = 1072981; // Or perhaps you'd rather not.
            InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!
            CompletionMessage = 1075536; // Wonderful! Tick tock, tick tock, soon all shall be well with grandfather's clock!

            Objectives.Add(new CollectObjective(5, typeof(ClockParts), 1024175)); // clock parts

            Rewards.Add(new ItemReward(1074282, typeof(NibbetSatchel))); // Craftsmans's Satchel
        }
    }

    public class DeliciousFishes : MLQuest
    {
        public DeliciousFishes()
        {
            Activated = true;
            HasRestartDelay = true;
            Title = 1075555; // Delicious Fishes
            Description =
                1075556; // Ello there, looking for a good place on the dock to fish? I like the southeast corner meself. What's that? Oh, no, *sighs* me pole is broken and in for fixin'. My grandpappy gave me that pole, means a lot you see. Miss the taste of fish though... Oh say, since you're here, could you catch me a few fish? I can cook a mean fish steak, and I'll split 'em with you! But make sure it's one of the green kind, they're the best for seasoning!
            RefusalMessage =
                1075558; // Ah, you're missin' out my friend, you're missing out. My peppercorn fishsteaks are famous on this little isle of ours!
            InProgressMessage =
                1075559; // Eh? Find yerself a pole and get close to some water. Just toss the line on in and hopefully you won't snag someone's old boots! Remember, that's twenty of them green fish we'll be needin', so come back when you've got em, 'aight?
            CompletionMessage =
                1075560; // Just a moment my friend, just a moment! *rummages in his pack* Here we are! My secret blend of peppers always does the trick, never fails, no not once. These'll fill you up much faster than that tripe they sell in the market!

            Objectives.Add(new CollectObjective(5, typeof(Fish), 1022508)); // fish

            Rewards.Add(new ItemReward(1075557, typeof(PeppercornFishsteak), 3)); // peppercorn fishsteak
        }
    }

    public class FleeAndFatigue : MLQuest
    {
        public FleeAndFatigue()
        {
            Activated = true;
            HasRestartDelay = true;
            Title = 1075487; // Flee and Fatigue
            Description =
                1075488; // I was just *coughs* ambushed near the moongate. *wheeze* Why do I pay my taxes? Where were the guards? You then, you an Alchemist? If you can make me a few Refresh potions, I will be back on my feet and can give those lizards the what for! Find a mortar and pestle, a good amount of black pearl, and ten empty bottles to store the finished potions in. Just use the mortar and pestle and the rest will surely come to you. When you return, the favor will be repaid.
            RefusalMessage =
                1075489; // Fine fine, off with *cough* thee then! The next time you see a lizardman though, give him a whallop for me, eh?
            InProgressMessage =
                1075490; // Just remember you need to use your mortar and pestle while you have empty bottles and some black pearl. Refresh potions are what I need.
            CompletionMessage =
                1075491; // *glug* *glug* Ahh... Yes! Yes! That feels great! Those lizardmen will never know what hit 'em! Here, take this, I can get more from the lizards.

            Objectives.Add(new CollectObjective(10, typeof(RefreshPotion), "refresh potions"));

            Rewards.Add(new ItemReward(1074282, typeof(SadrahSatchel))); // Craftsmans's Satchel
        }
    }

    public class ChopChopOnTheDouble : MLQuest
    {
        public ChopChopOnTheDouble()
        {
            Activated = true;
            HasRestartDelay = true;
            Title = 1075537; // Chop Chop, On The Double!
            Description =
                1075538; // That's right, move it! I need sixty logs on the double, and they need to be freshly cut! If you can get them to me fast I'll have your payment in your hands before you have the scent of pine out from beneath your nostrils. Just get a sharp axe and hack away at some of the trees in the land and your lumberjacking skill will rise in no time.
            RefusalMessage = 1072981; // Or perhaps you'd rather not.
            InProgressMessage = 1072271; // You're not quite done yet.  Get back to work!
            CompletionMessage =
                1075539; // Ahhh! The smell of fresh cut lumber. And look at you, all strong and proud, as if you had done an honest days work!

            Objectives.Add(new CollectObjective(60, typeof(Log), 1027133)); // log

            Rewards.Add(new ItemReward(1074282, typeof(HargroveSatchel))); // Craftsmans's Satchel
        }
    }

    public class Andric : BaseCreature
    {
        [Constructible]
        public Andric() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the archer trainer";
            Race = Race.Human;
            Body = 0x190;
            Female = false;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            SetSkill(SkillName.Archery, 60.0, 80.0);

            AddItem(new Backpack());

            Item item;

            item = new LeatherChest();
            item.Hue = 0x1BB;
            AddItem(item);

            item = new LeatherLegs();
            item.Hue = 0x6AD;
            AddItem(item);

            item = new LeatherArms();
            item.Hue = 0x6AD;
            AddItem(item);

            item = new LeatherGloves();
            item.Hue = 0x1BB;
            AddItem(item);

            AddItem(new Boots(0x1BB));
            AddItem(new CompositeBow());
        }

        public Andric(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Andric";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(
                this,
                pm,
                Utility.RandomList(
                    1074205, // Oh great adventurer, would you please assist a weak soul in need of aid?
                    1074213  // Hey buddy.  Looking for work?
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

    public class Kashiel : BaseCreature
    {
        [Constructible]
        public Kashiel() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the archer";
            Race = Race.Human;
            Body = 0x191;
            Female = true;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());

            Item item;

            item = new LeatherChest();
            item.Hue = 0x1BB;
            AddItem(item);

            item = new LeatherLegs();
            item.Hue = 0x901;
            AddItem(item);

            item = new LeatherArms();
            item.Hue = 0x901;
            AddItem(item);

            item = new LeatherGloves();
            item.Hue = 0x1BB;
            AddItem(item);

            AddItem(new Boots(0x1BB));
            AddItem(new CompositeBow());
        }

        public Kashiel(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Kashiel";
        public override bool IsInvulnerable => true;

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

    public class Asandos : BaseCreature
    {
        [Constructible]
        public Asandos() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the chef";
            Race = Race.Human;
            Body = 0x190;
            Female = false;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new Boots(0x901));
            AddItem(new ShortPants());
            AddItem(new Shirt());
            AddItem(new Cap());
            AddItem(new HalfApron(0x28));
        }

        public Asandos(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Asandos";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(
                this,
                pm,
                Utility.RandomList(
                    1074205, // Oh great adventurer, would you please assist a weak soul in need of aid?
                    1074213  // Hey buddy.  Looking for work?
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

    // [QuesterName( "Clarisse" )] // On OSI the gumps refer to her as this, different from actual name
    public class Clairesse : BaseCreature
    {
        [Constructible]
        public Clairesse() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the servant";
            Race = Race.Human;
            Body = 0x191;
            Female = true;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new Shoes(Utility.RandomNeutralHue()));
            AddItem(new PlainDress(0x3C9));
        }

        public Clairesse(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Clairesse";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(
                this,
                pm,
                Utility.RandomList(
                    1074205, // Oh great adventurer, would you please assist a weak soul in need of aid?
                    1074213  // Hey buddy.  Looking for work?
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

    public class Gervis : BaseCreature
    {
        [Constructible]
        public Gervis() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the blacksmith trainer";
            Race = Race.Human;
            Body = 0x190;
            Female = false;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            SetSkill(SkillName.Blacksmith, 60.0, 80.0);

            AddItem(new Backpack());
            AddItem(new Boots(0x3B3));
            AddItem(new ShortPants(0x1BB));
            AddItem(new Doublet(0x652));
            AddItem(new SmithHammer());

            Item item;

            item = new LeatherGloves();
            item.Hue = 0x3B2;
            AddItem(item);
        }

        public Gervis(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Gervis";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(
                this,
                pm,
                Utility.RandomList(
                    1074205, // Oh great adventurer, would you please assist a weak soul in need of aid?
                    1074213, // Hey buddy.  Looking for work?
                    1074211  // I could use some help.
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

    public class Mugg : BaseCreature
    {
        [Constructible]
        public Mugg() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the miner";
            Race = Race.Human;
            Body = 0x190;
            Female = false;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new Boots(0x901));
            AddItem(new ShortPants(0x3B3));
            AddItem(new Shirt(0x22B));
            AddItem(new HalfApron(0x5F1));
            AddItem(new SkullCap(0x177));
            AddItem(new Pickaxe());
        }

        public Mugg(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Mugg";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1074211); // I could use some help.
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

    public class Lowel : BaseCreature
    {
        [Constructible]
        public Lowel() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the carpenter";
            Race = Race.Human;
            Body = 0x190;
            Female = false;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new Boots(0x543));
            AddItem(new ShortPants(0x758));
            AddItem(new FancyShirt(0x53A));
            AddItem(new HalfApron(0x6D2));
        }

        public Lowel(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Lowel";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(
                this,
                pm,
                Utility.RandomList(
                    1074205, // Oh great adventurer, would you please assist a weak soul in need of aid?
                    1074213  // Hey buddy.  Looking for work?
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

    public class Lyle : BaseCreature
    {
        [Constructible]
        public Lyle() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the mage";
            Race = Race.Human;
            Body = 0x190;
            Female = false;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new Robe(0x2FD));
            AddItem(new ThighBoots());
        }

        public Lyle(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Lyle";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(
                this,
                pm,
                Utility.RandomList(
                    1074205, // Oh great adventurer, would you please assist a weak soul in need of aid?
                    1074213  // Hey buddy.  Looking for work?
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

    public class Nibbet : BaseCreature
    {
        [Constructible]
        public Nibbet() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the tinker";
            Race = Race.Human;
            Body = 0x190;
            Female = false;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new Boots(0x591));
            AddItem(new ShortPants(0xF8));
            AddItem(new Shirt(0x2D));
            AddItem(new FullApron(0x288));
        }

        public Nibbet(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Nibbet";
        public override bool IsInvulnerable => true;

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

    public class Norton : BaseCreature
    {
        [Constructible]
        public Norton() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the fisher";
            Race = Race.Human;
            Body = 0x190;
            Female = false;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new ThighBoots());
            AddItem(new LongPants(0x6C2));
            AddItem(new Shirt(0x11D));
        }

        public Norton(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Norton";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(
                this,
                pm,
                Utility.RandomList(
                    1074205, // Oh great adventurer, would you please assist a weak soul in need of aid?
                    1074213, // Hey buddy.  Looking for work?
                    1074211  // I could use some help.
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

    public class Sadrah : BaseCreature
    {
        [Constructible]
        public Sadrah() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the courier";
            Race = Race.Human;
            Body = 0x191;
            Female = true;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new Boots(0x901));
            AddItem(new Skirt(0x52));
            AddItem(new Shirt(0x127));
            AddItem(new Cloak(0x65));
            AddItem(new Longsword());
        }

        public Sadrah(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Sadrah";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(
                this,
                pm,
                Utility.RandomList(
                    1074205, // Oh great adventurer, would you please assist a weak soul in need of aid?
                    1074213, // Hey buddy.  Looking for work?
                    1074211  // I could use some help.
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

    public class Hargrove : BaseCreature
    {
        [Constructible]
        public Hargrove() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Lumberjack";
            Race = Race.Human;
            Body = 0x190;
            Female = false;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new Boots(0x901));
            AddItem(new StuddedLegs());
            AddItem(new Shirt(0x288));
            AddItem(new Bandana(0x20));
            AddItem(new BattleAxe());

            Item item;

            item = new PlateGloves();
            item.Hue = 0x21E;
            AddItem(item);
        }

        public Hargrove(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Hargrove";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(
                this,
                pm,
                Utility.RandomList(
                    1074213, // Hey buddy.  Looking for work?
                    1074211  // I could use some help.
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
}
