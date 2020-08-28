namespace Server.Items
{
    public class ManaVampireScroll : SpellScroll
    {
        [Constructible]
        public ManaVampireScroll(int amount = 1) : base(52, 0x1F61, amount)
        {
        }

        public ManaVampireScroll(Serial serial) : base(serial)
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
