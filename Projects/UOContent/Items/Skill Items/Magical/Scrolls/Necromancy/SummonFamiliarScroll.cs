namespace Server.Items
{
    public class SummonFamiliarScroll : SpellScroll
    {
        [Constructible]
        public SummonFamiliarScroll(int amount = 1) : base(111, 0x226B, amount)
        {
        }

        public SummonFamiliarScroll(Serial serial) : base(serial)
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
