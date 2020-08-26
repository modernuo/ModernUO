namespace Server.Items
{
    public class RunedPrism : Item
    {
        [Constructible]
        public RunedPrism() : base(0x2F57) => Weight = 1.0;

        public RunedPrism(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073465; // runed prism

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
