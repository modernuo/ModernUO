namespace Server.Items
{
    public class Jellyfish : BaseFish
    {
        [Constructible]
        public Jellyfish() : base(0x3B0E)
        {
        }

        public Jellyfish(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074593; // Jellyfish

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
