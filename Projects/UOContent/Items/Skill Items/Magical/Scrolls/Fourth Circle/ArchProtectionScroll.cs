namespace Server.Items
{
    public class ArchProtectionScroll : SpellScroll
    {
        [Constructible]
        public ArchProtectionScroll(int amount = 1) : base(25, 0x1F46, amount)
        {
        }

        public ArchProtectionScroll(Serial serial) : base(serial)
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
