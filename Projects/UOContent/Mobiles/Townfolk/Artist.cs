using Server.Items;

namespace Server.Mobiles
{
    public class Artist : BaseCreature
    {
        [Constructible]
        public Artist()
            : base(AIType.AI_Animal, FightMode.None)
        {
            InitStats(31, 41, 51);

            SetSkill(SkillName.Healing, 36, 68);

            SpeechHue = Utility.RandomDyedHue();
            Title = "the artist";
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

            AddItem(new Doublet(Utility.RandomDyedHue()));
            AddItem(new Sandals(Utility.RandomNeutralHue()));
            AddItem(new ShortPants(Utility.RandomNeutralHue()));
            AddItem(new HalfApron(Utility.RandomDyedHue()));

            Utility.AssignRandomHair(this);

            Container pack = new Backpack();

            pack.DropItem(new Gold(250, 300));

            pack.Movable = false;

            AddItem(pack);
        }

        public Artist(Serial serial)
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
