namespace Server.Items
{
    [Furniture]
    public class ElegantLowTable : Item
    {
        [Constructible]
        public ElegantLowTable() : base(0x2819) => Weight = 1.0;

        public ElegantLowTable(Serial serial) : base(serial)
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

    [Furniture]
    public class PlainLowTable : Item
    {
        [Constructible]
        public PlainLowTable() : base(0x281A) => Weight = 1.0;

        public PlainLowTable(Serial serial) : base(serial)
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

    [Furniture, Flippable(0xB90, 0xB7D)]
    public class LargeTable : Item
    {
        [Constructible]
        public LargeTable() : base(0xB90) => Weight = 1.0;

        public LargeTable(Serial serial) : base(serial)
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

    [Furniture, Flippable(0xB35, 0xB34)]
    public class Nightstand : Item
    {
        [Constructible]
        public Nightstand() : base(0xB35) => Weight = 1.0;

        public Nightstand(Serial serial) : base(serial)
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

    [Furniture, Flippable(0xB8F, 0xB7C)]
    public class YewWoodTable : Item
    {
        [Constructible]
        public YewWoodTable() : base(0xB8F) => Weight = 1.0;

        public YewWoodTable(Serial serial) : base(serial)
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
