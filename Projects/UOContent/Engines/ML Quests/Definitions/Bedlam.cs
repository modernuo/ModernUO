using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions
{
    public class Momento : MLQuest
    {
        public Momento()
        {
            Activated = true;
            Title = 1074750; // Momento!
            Description =
                1074751; // I was going to march right out there and get it myself, but no ... Master Gnosos won't let me.  But you see, that bridle means so much to me.  A momento of happier, less-dead ... well undead horseback riding.  Could you fetch it for me?  I think my horse, formerly known as 'Resolve', may still be wearing it.
            RefusalMessage = 1074752; // Hrmph.
            InProgressMessage =
                1074753; // The bridle would be hard to miss on him now ... since he's skeletal.  Please do what you need to do to retrieve it for me.
            CompletionMessage = 1074754; // I'd know that jingling sound anywhere!  You have recovered my bridle.  Thank you.

            Objectives.Add(new CollectObjective(1, typeof(ResolvesBridle), "Resolve's Bridle"));

            Rewards.Add(ItemReward.LargeBagOfTreasure);
        }
    }

    public class CulinaryCrisis : MLQuest
    {
        public CulinaryCrisis()
        {
            Activated = true;
            Title = 1074755; // Culinary Crisis
            Description =
                1074756; // You have NO idea how impossible this is.  Simply intolerable!  How can one expect an artiste' like me to create masterpieces of culinary delight without the best, fresh ingredients?  Ever since this whositwhatsit started this uproar, my thrice-daily produce deliveries have ended.  I can't survive another hour without produce!
            RefusalMessage = 1074757; // You have no artistry in your soul.
            InProgressMessage = 1074758; // I must have fresh produce and cheese at once!
            CompletionMessage =
                1074759; // Those dates look bruised!  Oh no, and you fetched a soft cheese.  *deep pained sigh*  Well, even I can only do so much with inferior ingredients.  BAM!

            Objectives.Add(new CollectObjective(20, typeof(Dates), 1025927));      // bunch of dates
            Objectives.Add(new CollectObjective(5, typeof(CheeseWheel), 1022430)); // wheel of cheese

            Rewards.Add(ItemReward.BagOfTreasure);
        }
    }

    public class GoneNative : MLQuest
    {
        public GoneNative()
        {
            Activated = true;
            Title = 1074855; // Gone Native
            Description =
                1074856; // Pathetic really.  I must say, a senior instructor going native -- forgetting about his students and peers and engaging in such disgraceful behavior!  I'm speaking, of course, of Theophilus.  Master Theophilus to you. He may have gone native but he still holds a Mastery Degree from Bedlam College!  But, well, that's neither here nor there.  I need you to take care of my colleague.  Convince him of the error of his ways.  He may resist.  In fact, assume he will and kill him.  We'll get him resurrected and be ready to cure his folly.  What do you say?
            RefusalMessage =
                1074857; // I understand.  A Master of Bedlam, even one entirely off his rocker, is too much for you to handle.
            InProgressMessage =
                1074858; // You had better get going.  Master Theophilus isn't likely to kill himself just to save me this embarrassment.
            CompletionMessage =
                1074859; // You look a bit worse for wear!  He put up a good fight did he?  Hah!  That's the spirit … a Master of Bedlam is a match for most.

            Objectives.Add(new KillObjective(1, new[] { typeof(MasterTheophilus) }, "Master Theophilus"));

            Rewards.Add(ItemReward.LargeBagOfTreasure);
        }
    }

    [QuesterName("Kia (Bedlam)")]
    public class Kia : BaseCreature
    {
        [Constructible]
        public Kia() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the student";
            Race = Race.Human;
            Body = 0x191;
            Female = true;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new Sandals(0x709));
            AddItem(new Robe(0x497));
        }

        public Kia(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Kia";

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

    [QuesterName("Emerillo (Bedlam)")]
    public class Emerillo : BaseCreature
    {
        [Constructible]
        public Emerillo() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the cook";
            Race = Race.Human;
            Body = 0x190;
            Female = false;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new Sandals(Utility.RandomNeutralHue()));
            AddItem(new ShortPants(Utility.RandomPinkHue()));
            AddItem(new Shirt());
            AddItem(new HalfApron(0x8FD));
        }

        public Emerillo(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Emerillo";

        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1074222); // Could I trouble you for some assistance?
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

    public class Nythalia : BaseCreature
    {
        [Constructible]
        public Nythalia() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the student";
            Race = Race.Human;
            Body = 0x191;
            Female = true;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new Shoes(Utility.RandomNeutralHue()));
            AddItem(new Robe(Utility.RandomBool() ? 0x497 : 0x498));
        }

        public Nythalia(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Mythalia";

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
