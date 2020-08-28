namespace Server.Items
{
    public class LuckyDagger : Item
    {
        [Constructible]
        public LuckyDagger() : base(0xF52) => Hue = 0x8A5;

        public LuckyDagger(Serial serial) : base(serial)
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
