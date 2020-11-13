namespace Server.Items
{
    [Furniture, Flippable(0xB2D, 0xB2C)]
    public class WoodenBench : Item
    {
        [Constructible]
        public WoodenBench() : base(0xB2D) => Weight = 6;

        public WoodenBench(Serial serial) : base(serial)
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
        }
    }
}
