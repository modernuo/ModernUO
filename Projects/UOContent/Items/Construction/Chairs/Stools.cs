namespace Server.Items
{
    [Furniture]
    public class Stool : Item
    {
        [Constructible]
        public Stool() : base(0xA2A) => Weight = 10.0;

        public Stool(Serial serial) : base(serial)
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
                Weight = 10.0;
            }
        }
    }

    [Furniture]
    public class FootStool : Item
    {
        [Constructible]
        public FootStool() : base(0xB5E) => Weight = 6.0;

        public FootStool(Serial serial) : base(serial)
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
                Weight = 10.0;
            }
        }
    }
}
