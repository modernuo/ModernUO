namespace Server.Items
{
    public class SmallEmptyPot : Item
    {
        [Constructible]
        public SmallEmptyPot() : base(0x11C6) => Weight = 100;

        public SmallEmptyPot(Serial serial) : base(serial)
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

    public class LargeEmptyPot : Item
    {
        [Constructible]
        public LargeEmptyPot() : base(0x11C7) => Weight = 6;

        public LargeEmptyPot(Serial serial) : base(serial)
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
