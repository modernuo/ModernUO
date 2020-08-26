namespace Server.Items
{
    public class SummonFireElementalScroll : SpellScroll
    {
        [Constructible]
        public SummonFireElementalScroll(int amount = 1) : base(62, 0x1F6B, amount)
        {
        }

        public SummonFireElementalScroll(Serial serial) : base(serial)
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
