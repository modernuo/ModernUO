namespace Server.Items
{
    public class VampiricEmbraceScroll : SpellScroll
    {
        [Constructible]
        public VampiricEmbraceScroll(int amount = 1) : base(112, 0x226C, amount)
        {
        }

        public VampiricEmbraceScroll(Serial serial) : base(serial)
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
