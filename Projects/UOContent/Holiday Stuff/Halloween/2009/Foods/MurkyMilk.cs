namespace Server.Items
{
    /*
    first seen halloween 2009.  subsequently in 2010,
    2011 and 2012. GM Beggar-only Semi-Rare Treats
    */

    public class MurkyMilk : Pitcher
    {
        [Constructible]
        public MurkyMilk()
            : base(BeverageType.Milk)
        {
            Hue = 0x3e5;
            Quantity = MaxQuantity;
            ItemID = Utility.RandomBool() ? 0x09F0 : 0x09AD;
        }

        public MurkyMilk(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Murky Milk";
        public override int MaxQuantity => 5;
        public override double DefaultWeight => 1;

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
