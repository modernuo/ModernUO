using Server.Items;

namespace Server.Mobiles
{
    public class Merchant : BaseEscortable
    {
        [Constructible]
        public Merchant()
        {
            Title = "the merchant";
            SetSkill(SkillName.ItemID, 55.0, 78.0);
            SetSkill(SkillName.ArmsLore, 55, 78);
        }

        public Merchant(Serial serial)
            : base(serial)
        {
        }

        public override bool CanTeach => true;
        public override bool ClickTitle => false; // Do not display 'the merchant' when single-clicking

        private static int GetRandomHue()
        {
            return Utility.Random(6) switch
            {
                0 => 0,
                1 => Utility.RandomBlueHue(),
                2 => Utility.RandomGreenHue(),
                3 => Utility.RandomRedHue(),
                4 => Utility.RandomYellowHue(),
                5 => Utility.RandomNeutralHue(),
                _ => 0
            };
        }

        public override void InitOutfit()
        {
            if (Female)
            {
                AddItem(new PlainDress());
            }
            else
            {
                AddItem(new Shirt(GetRandomHue()));
            }

            var lowHue = GetRandomHue();

            AddItem(new ThighBoots());

            if (Female)
            {
                AddItem(new FancyDress(lowHue));
            }
            else
            {
                AddItem(new FancyShirt(lowHue));
            }

            AddItem(new LongPants(lowHue));

            if (!Female)
            {
                AddItem(new BodySash(lowHue));
            }

            // if (!Female)
            // AddItem( new Longsword() );

            Utility.AssignRandomHair(this);

            PackGold(200, 250);
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
