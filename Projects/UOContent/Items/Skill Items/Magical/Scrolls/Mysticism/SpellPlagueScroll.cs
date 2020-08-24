namespace Server.Items
{
    public class SpellPlagueScroll : SpellScroll
    {
        [Constructible]
        public SpellPlagueScroll(int amount = 1)
            : base(689, 0x2DAA, amount)
        {
        }

        public SpellPlagueScroll(Serial serial)
            : base(serial)
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

            /*int version = */
            reader.ReadInt();
        }
    }
}
