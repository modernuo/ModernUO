namespace Server.Items
{
    public class HailStormScroll : SpellScroll
    {
        [Constructible]
        public HailStormScroll(int amount = 1)
            : base(690, 0x2DAB, amount)
        {
        }

        public HailStormScroll(Serial serial)
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
