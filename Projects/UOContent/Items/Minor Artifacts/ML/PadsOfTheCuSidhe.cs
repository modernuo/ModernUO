namespace Server.Items
{
    public class PadsOfTheCuSidhe : FurBoots
    {
        [Constructible]
        public PadsOfTheCuSidhe() : base(0x47E)
        {
        }

        public PadsOfTheCuSidhe(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075048; // Pads of the Cu Sidhe

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
