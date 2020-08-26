namespace Server.Items
{
    [Flippable(0x1f81, 0x1f82, 0x1f83, 0x1f84)]
    public class Glass : Item
    {
        [Constructible]
        public Glass() : base(0x1f81) => Weight = 0.1;

        public Glass(Serial serial) : base(serial)
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

    public class GlassBottle : Item
    {
        [Constructible]
        public GlassBottle() : base(0xe2b) => Weight = 0.3;

        public GlassBottle(Serial serial) : base(serial)
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
