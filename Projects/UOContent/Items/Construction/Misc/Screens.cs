namespace Server.Items
{
    [Furniture, Flippable(0x24D0, 0x24D1, 0x24D2, 0x24D3, 0x24D4)]
    public class BambooScreen : Item
    {
        [Constructible]
        public BambooScreen() : base(0x24D0) => Weight = 20.0;

        public BambooScreen(Serial serial) : base(serial)
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

    [Furniture, Flippable(0x24CB, 0x24CC, 0x24CD, 0x24CE, 0x24CF)]
    public class ShojiScreen : Item
    {
        [Constructible]
        public ShojiScreen() : base(0x24CB) => Weight = 20.0;

        public ShojiScreen(Serial serial) : base(serial)
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
