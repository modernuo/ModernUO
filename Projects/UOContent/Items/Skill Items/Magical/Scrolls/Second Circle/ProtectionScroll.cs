namespace Server.Items
{
    public class ProtectionScroll : SpellScroll
    {
        [Constructible]
        public ProtectionScroll(int amount = 1) : base(14, 0x1F3B, amount)
        {
        }

        public ProtectionScroll(Serial serial) : base(serial)
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

            var version = reader.ReadInt();
        }
    }
}
