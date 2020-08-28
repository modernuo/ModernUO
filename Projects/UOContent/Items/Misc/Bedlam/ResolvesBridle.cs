namespace Server.Items
{
    public class ResolvesBridle : Item
    {
        [Constructible]
        public ResolvesBridle() : base(0x1374)
        {
        }

        public ResolvesBridle(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074761; // Resolve's Bridle

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
