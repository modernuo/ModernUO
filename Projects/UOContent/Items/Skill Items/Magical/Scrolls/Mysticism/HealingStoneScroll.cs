namespace Server.Items
{
    public class HealingStoneScroll : SpellScroll
    {
        [Constructible]
        public HealingStoneScroll(int amount = 1)
            : base(678, 0x2D9F, amount)
        {
        }

        public HealingStoneScroll(Serial serial)
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
