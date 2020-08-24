namespace Server.Items
{
    public class StoneFormScroll : SpellScroll
    {
        [Constructible]
        public StoneFormScroll(int amount = 1)
            : base(684, 0x2DA5, amount)
        {
        }

        public StoneFormScroll(Serial serial)
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
