namespace Server.Items
{
    /*
    first seen halloween 2009.  subsequently in 2010,
    2011 and 2012. GM Beggar-only Semi-Rare Treats
    */

    public class CreepyCake : Food
    {
        [Constructible]
        public CreepyCake() : base(0x9e9, 1) => Hue = 0x3E4;

        public CreepyCake(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Creepy Cake";

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
