namespace Server.Items
{
    public class TatteredAncientMummyWrapping : Item
    {
        [Constructible]
        public TatteredAncientMummyWrapping() : base(0xE21) => Hue = 0x909;

        public TatteredAncientMummyWrapping(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094912; // Tattered Ancient Mummy Wrapping [Replica]

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
