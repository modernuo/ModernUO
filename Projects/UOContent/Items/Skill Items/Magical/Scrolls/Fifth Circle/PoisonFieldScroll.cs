namespace Server.Items
{
    public class PoisonFieldScroll : SpellScroll
    {
        [Constructible]
        public PoisonFieldScroll(int amount = 1) : base(38, 0x1F53, amount)
        {
        }

        public PoisonFieldScroll(Serial serial) : base(serial)
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
