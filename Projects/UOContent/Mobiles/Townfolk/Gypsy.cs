using Server.Items;

namespace Server.Mobiles
{
    public class Gypsy : BaseCreature
    {
        [Constructible]
        public Gypsy() : base(AIType.AI_Animal, FightMode.None)
        {
            InitStats(31, 41, 51);
            SetSkill(SkillName.Cooking, 65, 88);
            SetSkill(SkillName.Snooping, 65, 88);
            SetSkill(SkillName.Stealing, 65, 88);

            SetSpeed(0.2, 0.4);
            SpeechHue = Utility.RandomDyedHue();
            Hue = Race.Human.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
                AddItem(new Kilt(Utility.RandomDyedHue()));
                AddItem(new Shirt(Utility.RandomDyedHue()));
                AddItem(new ThighBoots());
                Title = "the gypsy";
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
                AddItem(new ShortPants(Utility.RandomNeutralHue()));
                AddItem(new Shirt(Utility.RandomDyedHue()));
                AddItem(new Sandals());
                Title = "the gypsy";
            }

            AddItem(new Bandana(Utility.RandomDyedHue()));
            AddItem(new Dagger());

            Utility.AssignRandomHair(this);

            Container pack = new Backpack();

            pack.DropItem(new Gold(250, 300));

            pack.Movable = false;

            AddItem(pack);
        }

        public Gypsy(Serial serial)
            : base(serial)
        {
        }

        public override bool CanTeach => true;
        public override bool ClickTitle => false;

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
