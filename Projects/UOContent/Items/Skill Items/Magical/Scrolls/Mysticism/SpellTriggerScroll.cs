namespace Server.Items
{
    public class SpellTriggerScroll : SpellScroll
    {
        [Constructible]
        public SpellTriggerScroll(int amount = 1)
            : base(685, 0x2DA6, amount)
        {
        }

        public SpellTriggerScroll(Serial serial)
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
