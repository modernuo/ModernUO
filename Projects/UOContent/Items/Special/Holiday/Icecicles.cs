namespace Server.Items
{
    public class IcicleLargeSouth : Item
    {
        [Constructible]
        public IcicleLargeSouth()
            : base(0x4572)
        {
        }

        public IcicleLargeSouth(Serial serial)
            : base(serial)
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

    public class IcicleMedSouth : Item
    {
        [Constructible]
        public IcicleMedSouth()
            : base(0x4573)
        {
        }

        public IcicleMedSouth(Serial serial)
            : base(serial)
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

    public class IcicleSmallSouth : Item
    {
        [Constructible]
        public IcicleSmallSouth()
            : base(0x4574)
        {
        }

        public IcicleSmallSouth(Serial serial)
            : base(serial)
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

    public class IcicleLargeEast : Item
    {
        [Constructible]
        public IcicleLargeEast()
            : base(0x4575)
        {
        }

        public IcicleLargeEast(Serial serial)
            : base(serial)
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

    public class IcicleMedEast : Item
    {
        [Constructible]
        public IcicleMedEast()
            : base(0x4576)
        {
        }

        public IcicleMedEast(Serial serial)
            : base(serial)
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

    public class IcicleSmallEast : Item
    {
        [Constructible]
        public IcicleSmallEast()
            : base(0x4577)
        {
        }

        public IcicleSmallEast(Serial serial)
            : base(serial)
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
