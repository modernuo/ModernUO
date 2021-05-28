namespace Server.Items
{
    /*
    first seen halloween 2009.  subsequently in 2010,
    2011 and 2012. GM Beggar-only Semi-Rare Treats
    */

    public class HarvestWine : BeverageBottle
    {
        [Constructible]
        public HarvestWine()
            : base(BeverageType.Wine) =>
            Hue = 0xe0;

        public HarvestWine(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Harvest Wine";
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
