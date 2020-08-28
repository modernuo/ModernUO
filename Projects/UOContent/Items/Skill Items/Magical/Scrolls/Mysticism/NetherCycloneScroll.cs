namespace Server.Items
{
    public class NetherCycloneScroll : SpellScroll
    {
        [Constructible]
        public NetherCycloneScroll(int amount = 1)
            : base(691, 0x2DAC, amount)
        {
        }

        public NetherCycloneScroll(Serial serial)
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
