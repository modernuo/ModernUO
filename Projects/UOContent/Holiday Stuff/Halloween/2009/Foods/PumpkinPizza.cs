namespace Server.Items
{
    /*
    first seen halloween 2009.  subsequently in 2010,
    2011 and 2012. GM Beggar-only Semi-Rare Treats
    */

    public class PumpkinPizza : CheesePizza
    {
        [Constructible]
        public PumpkinPizza() => Hue = 0xF3;

        public PumpkinPizza(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Pumpkin Pizza";

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
