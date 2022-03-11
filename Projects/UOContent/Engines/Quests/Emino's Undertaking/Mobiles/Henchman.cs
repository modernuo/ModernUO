using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Ninja
{
    public class Henchman : BaseCreature
    {
        [Constructible]
        public Henchman() : base(AIType.AI_Melee, FightMode.Aggressor)
        {
            InitStats(45, 30, 5);

            Hue = Race.Human.RandomSkinHue();
            Body = 0x190;

            Utility.AssignRandomHair(this);
            Utility.AssignRandomFacialHair(this);

            AddItem(new LeatherNinjaJacket());
            AddItem(new LeatherNinjaPants());
            AddItem(new NinjaTabi());

            if (Utility.RandomBool())
            {
                AddItem(new Kama());
            }
            else
            {
                AddItem(new Tessen());
            }

            SetSkill(SkillName.Swords, 50.0);
            SetSkill(SkillName.Tactics, 50.0);
        }

        public Henchman(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a henchman";

        public override bool AlwaysMurderer => true;

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
