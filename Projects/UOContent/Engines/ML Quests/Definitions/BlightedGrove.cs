using System;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions
{
    public class VilePoison : MLQuest
    {
        public VilePoison()
        {
            Activated = true;
            Title = 1074950; // Vile Poison
            Description =
                1074956; // Heya!  I'm sure glad to see you.  Listen I'm in a bit of a bind here.  I'm supposed to be gathering poisoned water at the base of that corrupted tree there, but I can't get in under the roots to get a good sample.  The branches and brush are so tainted that they can't be cut, burned or even magically passed.  It's put my work at a real standstill.  If you help me out, I'll help you get in there too.  Whadda ya say?
            RefusalMessage = 1074964; // Okay.  If you change your mind, I'll probably still be stuck here trying to get in.
            InProgressMessage = 1074968; // My friend, Iosep, is a weaponsmith in Jhelom.  If anyone can help us, he can!
            CompletionMessage =
                1074991; // Greetings.  What have you there?  Ah, a sample from a poisonous tree, you say?  My friend Jamal sent you?  Well, let me see that then, and we'll get to work.

            Objectives.Add(new DeliverObjective(typeof(TaintedTreeSample), 1, "tainted tree sample", typeof(Iosep)));

            Rewards.Add(new DummyReward(1074962)); // A step closer to entering Blighted Grove.
        }

        public override Type NextQuest => typeof(ARockAndAHardPlace);
    }

    public class ARockAndAHardPlace : MLQuest
    {
        public ARockAndAHardPlace()
        {
            Activated = true;
            Title = 1074951; // A Rock and a Hard Place
            Description =
                1074957; // This is some nasty stuff, that's for certain.  I don't even want to think about what sort of blight caused this venomous reaction from that old tree.  Let's get to work … we'll need to try something really hard but still workable as our base material.  Nothing's harder than stone and diamond.  Let's try them first.
            RefusalMessage = 1074965; // Sure, no problem.  I thought you were interested in figuring this out.
            InProgressMessage =
                1074969; // If you're a miner, you should have no trouble getting that stuff.  If not, you can probably buy some samples from a miner?
            CompletionMessage =
                1074992; // Have you got the granite and diamonds?  Great, let me see them and we'll see what effect this venom has upon them.

            // Any type of granite works
            Objectives.Add(new CollectObjective(4, typeof(BaseGranite), 1026009)); // rock
            Objectives.Add(new CollectObjective(2, typeof(BlueDiamond), 1032696)); // Blue Diamond

            Rewards.Add(new DummyReward(1074962)); // A step closer to entering Blighted Grove.
        }

        public override Type NextQuest => typeof(SympatheticMagic);
        public override bool IsChainTriggered => true;
    }

    public class SympatheticMagic : MLQuest
    {
        public SympatheticMagic()
        {
            Activated = true;
            Title = 1074952; // Sympathetic Magic
            Description =
                1074958; // Hmm, I've never even heard of something that can damage diamond like that.  I guess we'll have to go with plan B.  Let's try something similar.  Sometimes there's a natural immunity to be found when you use a substance that's like the one you're trying to cut.  A sort of "sympathetic" thing.  Y'know?
            RefusalMessage = 1074965; // Sure, no problem.  I thought you were interested in figuring this out.
            InProgressMessage = 1074970; // I think a lumberjack can help supply bark.
            CompletionMessage = 1074993; // You're back with the bark already?  Terrific!  I bet this will do the trick.

            Objectives.Add(new CollectObjective(10, typeof(BarkFragment), 1032687)); // Bark Fragment

            Rewards.Add(new DummyReward(1074962)); // A step closer to entering Blighted Grove.
        }

        public override Type NextQuest => typeof(AlreadyDead);
        public override bool IsChainTriggered => true;
    }

    public class AlreadyDead : MLQuest
    {
        public AlreadyDead()
        {
            Activated = true;
            Title = 1074953; // Already Dead
            Description =
                1074959; // Amazing!  The bark was reduced to ash in seconds.  Whatever this taint is, it plays havok with living things.  And of course, it took the edge off both diamonds and granite even faster.  What we need is something workable but dead; something that can hold an edge without melting.  See what you can come up with, please.
            RefusalMessage = 1074965; // Sure, no problem.  I thought you were interested in figuring this out.
            InProgressMessage =
                1074971; // I'm thinking we need something fairly brittle or it won't hold an edge.  And, it can't be alive, of course.
            CompletionMessage = 1074994; // Great thought!  Bone might just do the trick.

            Objectives.Add(new InternalObjective());

            Rewards.Add(new DummyReward(1074962)); // A step closer to entering Blighted Grove.
        }

        public override Type NextQuest => typeof(Eureka);
        public override bool IsChainTriggered => true;

        private class InternalObjective : CollectObjective
        {
            public InternalObjective()
                : base(10, typeof(Bone), 1074963) // (10) workable samples
            {
            }

            public override bool ShowDetailed => false;
        }
    }

    public class Eureka : MLQuest
    {
        public Eureka()
        {
            Activated = true;
            Title = 1074954; // Eureka!
            Description =
                1074960; // We're in business!  I've put together the instructions for chopping sort of sword, in the style of one of those new-fangled elven machetes.  Take those back to Jamal for me, if you would.
            RefusalMessage = 1074966; // Well, okay.  I guess I thought you'd want to see this through.
            InProgressMessage =
                1074972; // I'm sure Jamal is eager to get this information.  He's probably still hanging around near that big old blighted tree.
            CompletionMessage = 1074995; // Heya!  You're back.  Was Iosep able to help?  Let me see what he's sent.

            Objectives.Add(new DeliverObjective(typeof(SealedNotesForJamal), 1, "sealed note for Jamal", typeof(Jamal)));

            Rewards.Add(new DummyReward(1074962)); // A step closer to entering Blighted Grove.
        }

        public override Type NextQuest => typeof(SubContracting);
        public override bool IsChainTriggered => true;

        public override void GetRewards(MLQuestInstance instance)
        {
            var pm = instance.Player;

            if (!pm.HasRecipe(32))
            {
                // The ability is awarded regardless of blacksmithy skill
                pm.AcquireRecipe(32);

                if (pm.Skills.Blacksmith.Base < 45.0) // TODO: Verify threshold
                {
                    // You observe carefully but you can't grasp the complexities of smithing a bone handled machete.
                    pm.SendLocalizedMessage(1075005);
                }
                else
                {
                    pm.SendLocalizedMessage(1075006); // You have learned how to smith a bone handled machete!
                }
            }

            base.GetRewards(instance);
        }
    }

    public class SubContracting : MLQuest
    {
        public SubContracting()
        {
            Activated = true;
            Title = 1074955; // Sub Contracting
            Description =
                1074961; // Wonderful!  Now we can both get in there!  Let me show you these instructions for making this machete.  If you're not skilled in smithing, I'm not sure how much sense it will make though.  Listen, if you're heading in there anyway … maybe you'd do me one more favor?  I'm ah ... buried in work out here ... so if you'd go in and get me a few water samples, I'd be obliged.
            RefusalMessage = 1074967; // Oh.  Right, I guess you're really ... ah ... busy too.
            InProgressMessage =
                1074973; // Once you're inside, look for places where the water has twisted and warped the natural creatures.
            CompletionMessage =
                1074996; // I hear sloshing ... that must mean you've got my water samples.  Whew, I'm so glad you braved the dangers in there ... I mean, I would have but I'm so busy out here.  Here's your reward!

            // On OSI the label is "#1074999"
            Objectives.Add(new CollectObjective(3, typeof(SamplesOfCorruptedWater), "samples of corrupted water"));
            // TODO: "Return to" should say "Jamal (near Blighted Grove)"
            // Maybe every quest NPC has directions as a property?

            Rewards.Add(ItemReward.LargeBagOfTreasure);
        }

        public override bool IsChainTriggered => true;
    }

    [QuesterName("Jamal (near Blighted Grove)")]
    public class Jamal : BaseCreature
    {
        [Constructible]
        public Jamal()
            : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Fisherman";
            Body = 400;
            Hue = Race.RandomSkinHue();
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);
            Utility.AssignRandomFacialHair(this, HairHue);

            AddItem(new Shirt(0x1BB));
            AddItem(new ShortPants(Utility.RandomNeutralHue()));
            AddItem(new ThighBoots(Utility.RandomAnimalHue()));
            AddItem(new Backpack());
        }

        public Jamal(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Jamal";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadInt();
        }
    }

    [QuesterName("Iosep (Jhelom)")]
    public class Iosep : BaseCreature
    {
        [Constructible]
        public Iosep()
            : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Exporter";
            Body = 400;
            Hue = Race.RandomSkinHue();
            InitStats(100, 100, 25);

            AddItem(new FancyShirt(Utility.RandomBlueHue()));
            AddItem(new LongPants(0x1BB));
            AddItem(new Shoes(Utility.RandomNeutralHue()));
            AddItem(new Backpack());
        }

        public Iosep(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override string DefaultName => "Iosep";

        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(
                this,
                pm,
                Utility.RandomList(
                    1074209, // Hey, could you help me out with something?
                    1074215  // Don’t test my patience you sniveling worm!
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

            reader.ReadInt();
        }
    }
}
