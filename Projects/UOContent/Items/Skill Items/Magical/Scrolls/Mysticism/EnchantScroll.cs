namespace Server.Items
{
    public class EnchantScroll : SpellScroll
    {
        [Constructible]
        public EnchantScroll(int amount = 1)
            : base(680, 0x2DA1, amount)
        {
        }

        public EnchantScroll(Serial serial)
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
