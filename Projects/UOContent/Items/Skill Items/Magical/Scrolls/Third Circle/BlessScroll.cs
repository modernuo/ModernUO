namespace Server.Items
{
    public class BlessScroll : SpellScroll
    {
        [Constructible]
        public BlessScroll(int amount = 1) : base(16, 0x1F3D, amount)
        {
        }

        public BlessScroll(Serial serial) : base(serial)
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
