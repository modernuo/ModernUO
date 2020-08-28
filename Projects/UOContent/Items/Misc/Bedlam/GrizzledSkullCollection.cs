namespace Server.Items
{
    public class GrizzledSkullCollection : Item
    {
        [Constructible]
        public GrizzledSkullCollection() : base(0x21FC)
        {
        }

        public GrizzledSkullCollection(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072116; // Grizzled Skull collection

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
