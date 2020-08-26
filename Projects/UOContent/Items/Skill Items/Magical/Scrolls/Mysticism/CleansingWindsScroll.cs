namespace Server.Items
{
    public class CleansingWindsScroll : SpellScroll
    {
        [Constructible]
        public CleansingWindsScroll(int amount = 1)
            : base(687, 0x2DA8, amount)
        {
        }

        public CleansingWindsScroll(Serial serial)
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
