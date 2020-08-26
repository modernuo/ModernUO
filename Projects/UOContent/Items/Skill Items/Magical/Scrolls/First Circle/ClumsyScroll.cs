namespace Server.Items
{
    public class ClumsyScroll : SpellScroll
    {
        [Constructible]
        public ClumsyScroll(int amount = 1) : base(0, 0x1F2E, amount)
        {
        }

        public ClumsyScroll(Serial serial) : base(serial)
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
