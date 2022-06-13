using Server.Items;

namespace Server.Mobiles
{
    public class Ninja : BaseCreature
    {
        [Constructible]
        public Ninja() : base(AIType.AI_Melee, FightMode.Aggressor)
        {
            Title = "the ninja";
            InitStats(100, 100, 25);

            SetSkill(SkillName.Fencing, 64.0, 80.0);
            SetSkill(SkillName.Macing, 64.0, 80.0);
            SetSkill(SkillName.Ninjitsu, 60.0, 80.0);
            SetSkill(SkillName.Parry, 64.0, 80.0);
            SetSkill(SkillName.Tactics, 64.0, 85.0);
            SetSkill(SkillName.Swords, 64.0, 85.0);

            SetSpeed(0.2, 0.4);
            SpeechHue = Utility.RandomDyedHue();
            Hue = Race.Human.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }

            if (!Female)
            {
                AddItem(new LeatherNinjaHood());
            }

            AddItem(new LeatherNinjaPants());
            AddItem(new LeatherNinjaBelt());
            AddItem(new LeatherNinjaJacket());
            AddItem(new NinjaTabi());

            var hairHue = Utility.RandomNondyedHue();

            Utility.AssignRandomHair(this, hairHue);

            if (Utility.Random(7) != 0)
            {
                Utility.AssignRandomFacialHair(this, hairHue);
            }

            PackGold(250, 300);
        }

        public Ninja(Serial serial) : base(serial)
        {
        }

        public override bool CanTeach => true;
        public override bool ClickTitle => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
