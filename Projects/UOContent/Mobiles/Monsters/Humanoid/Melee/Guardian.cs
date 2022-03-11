using Server.Items;

namespace Server.Mobiles
{
    public class Guardian : BaseCreature
    {
        [Constructible]
        public Guardian() : base(AIType.AI_Archer, FightMode.Aggressor)
        {
            InitStats(100, 125, 25);
            Title = "the guardian";

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

            new ForestOstard().Rider = this;

            var chest = new PlateChest();
            chest.Hue = 0x966;
            AddItem(chest);
            var arms = new PlateArms();
            arms.Hue = 0x966;
            AddItem(arms);
            var gloves = new PlateGloves();
            gloves.Hue = 0x966;
            AddItem(gloves);
            var gorget = new PlateGorget();
            gorget.Hue = 0x966;
            AddItem(gorget);
            var legs = new PlateLegs();
            legs.Hue = 0x966;
            AddItem(legs);
            var helm = new PlateHelm();
            helm.Hue = 0x966;
            AddItem(helm);

            var bow = new Bow();

            bow.Movable = false;
            bow.Crafter = this;
            bow.Quality = WeaponQuality.Exceptional;

            AddItem(bow);

            PackItem(new Arrow(250));
            PackGold(250, 500);

            Skills.Anatomy.Base = 120.0;
            Skills.Tactics.Base = 120.0;
            Skills.Archery.Base = 120.0;
            Skills.MagicResist.Base = 120.0;
            Skills.DetectHidden.Base = 100.0;
        }

        public Guardian(Serial serial) : base(serial)
        {
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
