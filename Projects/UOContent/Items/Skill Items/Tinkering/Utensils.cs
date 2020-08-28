namespace Server.Items
{
    [Flippable(0x9F4, 0x9F5, 0x9A3, 0x9A4)]
    public class Fork : Item
    {
        [Constructible]
        public Fork() : base(0x9F4) => Weight = 1.0;

        public Fork(Serial serial) : base(serial)
        {
        }

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

    public class ForkLeft : Item
    {
        [Constructible]
        public ForkLeft() : base(0x9F4) => Weight = 1.0;

        public ForkLeft(Serial serial) : base(serial)
        {
        }

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

    public class ForkRight : Item
    {
        [Constructible]
        public ForkRight() : base(0x9F5) => Weight = 1.0;

        public ForkRight(Serial serial) : base(serial)
        {
        }

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

    [Flippable(0x9F8, 0x9F9, 0x9C2, 0x9C3)]
    public class Spoon : Item
    {
        [Constructible]
        public Spoon() : base(0x9F8) => Weight = 1.0;

        public Spoon(Serial serial) : base(serial)
        {
        }

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

    public class SpoonLeft : Item
    {
        [Constructible]
        public SpoonLeft() : base(0x9F8) => Weight = 1.0;

        public SpoonLeft(Serial serial) : base(serial)
        {
        }

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

    public class SpoonRight : Item
    {
        [Constructible]
        public SpoonRight() : base(0x9F9) => Weight = 1.0;

        public SpoonRight(Serial serial) : base(serial)
        {
        }

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

    [Flippable(0x9F6, 0x9F7, 0x9A5, 0x9A6)]
    public class Knife : Item
    {
        [Constructible]
        public Knife() : base(0x9F6) => Weight = 1.0;

        public Knife(Serial serial) : base(serial)
        {
        }

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

    public class KnifeLeft : Item
    {
        [Constructible]
        public KnifeLeft() : base(0x9F6) => Weight = 1.0;

        public KnifeLeft(Serial serial) : base(serial)
        {
        }

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

    public class KnifeRight : Item
    {
        [Constructible]
        public KnifeRight() : base(0x9F7) => Weight = 1.0;

        public KnifeRight(Serial serial) : base(serial)
        {
        }

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

    public class Plate : Item
    {
        [Constructible]
        public Plate() : base(0x9D7) => Weight = 1.0;

        public Plate(Serial serial) : base(serial)
        {
        }

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
