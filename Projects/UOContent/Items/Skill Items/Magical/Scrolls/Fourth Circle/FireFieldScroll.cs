namespace Server.Items
{
    public class FireFieldScroll : SpellScroll
    {
        [Constructible]
        public FireFieldScroll(int amount = 1) : base(27, 0x1F48, amount)
        {
        }

        public FireFieldScroll(Serial serial) : base(serial)
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
