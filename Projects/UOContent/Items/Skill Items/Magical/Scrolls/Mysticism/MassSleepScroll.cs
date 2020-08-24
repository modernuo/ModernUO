namespace Server.Items
{
    public class MassSleepScroll : SpellScroll
    {
        [Constructible]
        public MassSleepScroll(int amount = 1)
            : base(686, 0x2DA7, amount)
        {
        }

        public MassSleepScroll(Serial serial)
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
