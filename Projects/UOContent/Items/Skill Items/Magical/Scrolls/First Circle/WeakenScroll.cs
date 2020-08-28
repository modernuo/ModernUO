namespace Server.Items
{
    public class WeakenScroll : SpellScroll
    {
        [Constructible]
        public WeakenScroll(int amount = 1) : base(7, 0x1F34, amount)
        {
        }

        public WeakenScroll(Serial serial) : base(serial)
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
