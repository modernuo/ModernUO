namespace Server.Items
{
    [Furniture]
    [Flippable(0x232A, 0x232B)]
    public class GiftBox : BaseContainer
    {
        [Constructible]
        public GiftBox() : this(Utility.RandomDyedHue())
        {
        }

        [Constructible]
        public GiftBox(int hue) : base(Utility.Random(0x232A, 2))
        {
            Weight = 2.0;
            Hue = hue;
        }

        public GiftBox(Serial serial) : base(serial)
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
