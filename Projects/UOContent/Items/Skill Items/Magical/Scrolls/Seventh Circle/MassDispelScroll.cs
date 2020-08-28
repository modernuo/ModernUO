namespace Server.Items
{
    public class MassDispelScroll : SpellScroll
    {
        [Constructible]
        public MassDispelScroll(int amount = 1) : base(53, 0x1F62, amount)
        {
        }

        public MassDispelScroll(Serial serial) : base(serial)
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
