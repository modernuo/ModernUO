namespace Server.Items
{
    public class Shrimp : BaseFish
    {
        [Constructible]
        public Shrimp() : base(0x3B14)
        {
        }

        public Shrimp(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074596; // Shrimp

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
