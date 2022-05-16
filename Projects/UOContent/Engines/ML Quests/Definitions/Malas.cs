using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions
{
    public class PointyEars : MLQuest
    {
        public PointyEars()
        {
            Activated = true;
            Title = 1074640; // Pointy Ears
            Description =
                1074641; // I've heard ... there's some that will pay a good bounty for pointed ears, much like we used to pay for each wolf skin.  I've got nothing personal against these elves.  It's just business.  You want in on this?  I'm not fussy who I work with.
            RefusalMessage = 1074642; // Suit yourself.
            InProgressMessage = 1074643; // I can't pay a bounty if you don't bring bag the ears.
            CompletionMessage = 1074644; // Here to collect on a bounty?

            Objectives.Add(new CollectObjective(20, typeof(SeveredElfEars), 1032590)); // severed elf ears

            Rewards.Add(ItemReward.BagOfTrinkets);
        }
    }

    [QuesterName("Drithen (Umbra)")]
    public class Drithen : BaseCreature
    {
        [Constructible]
        public Drithen() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Fierce";
            Race = Race.Human;
            Body = 0x190;
            Female = false;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            AddItem(new Backpack());
            AddItem(new ElvenBoots(Utility.RandomNeutralHue()));
            AddItem(new LongPants(Utility.RandomBlueHue()));
            AddItem(new Tunic(Utility.RandomNeutralHue()));
            AddItem(new Cloak(Utility.RandomBrightHue()));

            SetSkill(SkillName.Focus, 60.0, 80.0);
        }

        public Drithen(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Drithen";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1074188); // Weakling! You are not up to the task I have.
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
