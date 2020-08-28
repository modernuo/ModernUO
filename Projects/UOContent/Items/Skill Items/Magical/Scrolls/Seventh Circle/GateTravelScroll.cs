namespace Server.Items
{
    public class GateTravelScroll : SpellScroll
    {
        [Constructible]
        public GateTravelScroll(int amount = 1) : base(51, 0x1F60, amount)
        {
        }

        public GateTravelScroll(Serial serial) : base(serial)
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
