namespace Server.Items
{
    [Flippable(0x1F03, 0x1F04)]
    public class RobeOfTheEclipse : BaseOuterTorso
    {
        [Constructible]
        public RobeOfTheEclipse() : base(0x1F03, 0x486)
        {
            Weight = 3.0;

            Attributes.Luck = 95;

            // TODO: Supports arcane?
        }

        public RobeOfTheEclipse(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075082; // Robe of the Eclipse

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
