namespace Server.Items
{
    public class EvilOmenScroll : SpellScroll
    {
        [Constructible]
        public EvilOmenScroll(int amount = 1) : base(104, 0x2264, amount)
        {
        }

        public EvilOmenScroll(Serial serial) : base(serial)
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
