namespace Server.Items
{
    public class EagleStrikeScroll : SpellScroll
    {
        [Constructible]
        public EagleStrikeScroll(int amount = 1)
            : base(682, 0x2DA3, amount)
        {
        }

        public EagleStrikeScroll(Serial serial)
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
