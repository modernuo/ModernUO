using Server.Items;

namespace Server.Mobiles
{
    public class Actor : BaseCreature
    {
        [Constructible]
        public Actor() : base(AIType.AI_Animal, FightMode.None)
        {
            InitStats(31, 41, 51);

            SpeechHue = Utility.RandomDyedHue();

            Hue = Race.Human.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
                AddItem(new FancyDress(Utility.RandomDyedHue()));
                Title = "the actress";
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
                AddItem(new LongPants(Utility.RandomNeutralHue()));
                AddItem(new FancyShirt(Utility.RandomDyedHue()));
                Title = "the actor";
            }

            AddItem(new Boots(Utility.RandomNeutralHue()));

            Utility.AssignRandomHair(this);

            Container pack = new Backpack();

            pack.DropItem(new Gold(250, 300));

            pack.Movable = false;

            AddItem(pack);
        }

        public Actor(Serial serial) : base(serial)
        {
        }

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
