namespace Server.Items
{
    [Furniture, Flippable(0xB32, 0xB33)]
    public class Throne : Item
    {
        [Constructible]
        public Throne() : base(0xB33) => Weight = 1.0;

        public Throne(Serial serial) : base(serial)
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

            if (Weight == 6.0)
            {
                Weight = 1.0;
            }
        }
    }

    [Furniture, Flippable(0xB2E, 0xB2F, 0xB31, 0xB30)]
    public class WoodenThrone : Item
    {
        [Constructible]
        public WoodenThrone() : base(0xB2E) => Weight = 15.0;

        public WoodenThrone(Serial serial) : base(serial)
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

            if (Weight == 6.0)
            {
                Weight = 15.0;
            }
        }
    }
}
