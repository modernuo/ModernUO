namespace Server.Items
{
    public class FeeblemindScroll : SpellScroll
    {
        [Constructible]
        public FeeblemindScroll(int amount = 1) : base(2, 0x1F30, amount)
        {
        }

        public FeeblemindScroll(Serial serial) : base(serial)
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
