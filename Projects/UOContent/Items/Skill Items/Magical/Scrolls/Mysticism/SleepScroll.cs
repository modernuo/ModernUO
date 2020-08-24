namespace Server.Items
{
    public class SleepScroll : SpellScroll
    {
        [Constructible]
        public SleepScroll(int amount = 1)
            : base(681, 0x2DA2, amount)
        {
        }

        public SleepScroll(Serial serial)
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
