namespace Server.Items
{
    public class RisingColossusScroll : SpellScroll
    {
        [Constructible]
        public RisingColossusScroll(int amount = 1)
            : base(692, 0x2DAD, amount)
        {
        }

        public RisingColossusScroll(Serial serial)
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
