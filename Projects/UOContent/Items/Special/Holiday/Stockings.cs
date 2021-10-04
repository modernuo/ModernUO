namespace Server.Items
{
    [Furniture]
    [Flippable(0x2bd9, 0x2bda)]
    public class GreenStocking : BaseContainer
    {
        [Constructible]
        public GreenStocking() : base(Utility.Random(0x2BD9, 2))
        {
        }

        public GreenStocking(Serial serial) : base(serial)
        {
        }

        public override int DefaultGumpID => 0x103;
        public override int DefaultDropSound => 0x42;

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

    [Furniture]
    [Flippable(0x2bdb, 0x2bdc)]
    public class RedStocking : BaseContainer
    {
        [Constructible]
        public RedStocking() : base(Utility.Random(0x2BDB, 2))
        {
        }

        public RedStocking(Serial serial) : base(serial)
        {
        }

        public override int DefaultGumpID => 0x103;
        public override int DefaultDropSound => 0x42;

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
