using System;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions
{
    public class TheAncientWorld : MLQuest
    {
        public TheAncientWorld()
        {
            Activated = true;
            Title = 1074534; // The Ancient World
            Description =
                1074535; // The lore of my people mentions Mondain many times. In one tale, it is revealed that he created and enslaved a race -- a sort of man bull, known as a 'minotaur'. The tales speak of mighty warriors who charged with blood-soaked horns into the heat of battle.  But, alas, the fate of the bull-men is unknown after the rupture.  Will you seek information about their civilization?
            RefusalMessage = 1074538; // I am disappointed, but I respect your decision.
            InProgressMessage =
                1074539; // A traveler has told me that worshippers of Mondain still exist and wander the land.  Perhaps their lore speaks of whether the bull-men survived.  I do not think they share their secrets gladly.  You may need to be 'persuasive'.
            CompletionMessage = 1074542; // What have you found?

            Objectives.Add(new CollectObjective(1, typeof(FragmentOfAMap), "fragment of a map"));

            Rewards.Add(new DummyReward(1074876)); // Knowledge of the legendary minotaur.
        }

        public override Type NextQuest => typeof(TheGoldenHorn);
    }

    public class TheGoldenHorn : MLQuest
    {
        public TheGoldenHorn()
        {
            Activated = true;
            Title = 1074543; // The Golden Horn
            Description =
                1074545; // Ah ha!  You see here ... and over here ... The map fragment places the city of the bull-men, Labyrinth, on that piece of Sosaria that was thrown into the sky. Hmmm, I would have you go there and find any artifacts that remain that help tell the story.  But, legend speaks of a mighty barrier to prevent invasion of the city. Take this map to Braen and explain the problem. Perhaps he can devise a solution.
            RefusalMessage = 1074538; // I am disappointed, but I respect your decision.
            InProgressMessage = 1074547; // Braen is nearby, run and speak with him.
            CompletionMessage = 1074549; // Yes?  What do you want?  I'm very busy.

            Objectives.Add(new DeliverObjective(typeof(FragmentOfAMapDelivery), 1, "fragment of a map", typeof(Braen)));

            Rewards.Add(new DummyReward(1074876)); // Knowledge of the legendary minotaur.
        }

        public override Type NextQuest => typeof(Bullish);
        public override bool IsChainTriggered => true;
    }

    public class Bullish : MLQuest
    {
        public Bullish()
        {
            Activated = true;
            Title = 1074550; // Bullish
            Description =
                1074552; // Oh, I see. I will need some materials to infuse you with the essence of a bull-man, so you can fool their defenses.  The most similar beast to the original Baratarian bull that the minotaur were bred from is undoubtedly the mighty Gaman, native to the Lands of the Feudal Lords.  I need horns, in great quantity to undertake this magic.
            RefusalMessage = 1074554; // Oh come now, don't be afraid.  The magic won't harm you.
            InProgressMessage =
                1074555; // I cannot grant you the ability to pass through the bull-men's defenses without the gaman horns.
            CompletionMessage =
                1074556; // You've returned at last!  Give me just a moment to examine what you've brought and I can perform the magic that will allow you enter the Labyrinth.

            Objectives.Add(new CollectObjective(20, typeof(GamanHorns), "gaman horns"));

            Rewards.Add(new DummyReward(1074876)); // Knowledge of the legendary minotaur.
        }

        public override Type NextQuest => typeof(LostCivilization);
        public override bool IsChainTriggered => true;
    }

    public class LostCivilization : MLQuest
    {
        public LostCivilization()
        {
            Activated = true;
            Title = 1074823; // Lost Civilization
            Description =
                1074825; // *whew*  It is done!  The fierce essence of the bull has been infused into your aura.  You are able now to breach the ancient defenses of the city.  Go forth and seek the minotaur -- and then return with wonderous tales and evidence of your visit to the Labyrinth.
            RefusalMessage =
                1074827; // As you wish.  I can't understand why you'd pass up such a remarkable opportunity.  Think of the adventures you would have.
            InProgressMessage =
                1074828; // You won't reach the minotaur city by loitering around here!  What are you waiting for?  You need to get to Malas and find the access point for the island.  You'll be renowned for your discovery!
            CompletionMessage =
                1074829; // Oh! You've returned at last!  I can't wait to hear the tales ... but first, let me see those artifacts.  You've certainly earned this reward.

            Objectives.Add(new CollectObjective(3, typeof(MinotaurArtifact), "minotaur artifacts"));

            Rewards.Add(ItemReward.Strongbox);
        }

        public override bool IsChainTriggered => true;
    }

    [QuesterName("Broolol (The Heartwood)")]
    public class LorekeeperBroolol : BaseCreature
    {
        [Constructible]
        public LorekeeperBroolol() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the keeper of tradition";
            Race = Race.Elf;
            Body = 0x25E;
            Female = true;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            SetSkill(SkillName.Meditation, 60.0, 80.0);
            SetSkill(SkillName.Focus, 60.0, 80.0);

            AddItem(new ElvenBoots(0x70D));
            AddItem(new FemaleElvenRobe(0x3A));
            AddItem(new WildStaff());
        }

        public LorekeeperBroolol(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Lorekeeper Broolol";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1074200); // Thank goodness you are here, there's no time to lose.
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
