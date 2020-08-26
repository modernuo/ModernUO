namespace Server.Items
{
    [Flippable(0x1F03, 0x1F04)]
    public class RobeOfTheEquinox : BaseOuterTorso
    {
        [Constructible]
        public RobeOfTheEquinox() : base(0x1F04, 0xD6)
        {
            Weight = 3.0;

            Attributes.Luck = 95;

            // TODO: Supports arcane?
            // TODO: Elves Only
        }

        public RobeOfTheEquinox(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075042; // Robe of the Equinox

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
