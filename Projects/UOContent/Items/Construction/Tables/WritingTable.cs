namespace Server.Items
{
    [Furniture, Flippable(0xB4A, 0xB49, 0xB4B, 0xB4C)]
    public class WritingTable : Item
    {
        [Constructible]
        public WritingTable() : base(0xB4A) => Weight = 1.0;

        public WritingTable(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Weight == 4.0)
            {
                Weight = 1.0;
            }
        }
    }
}
