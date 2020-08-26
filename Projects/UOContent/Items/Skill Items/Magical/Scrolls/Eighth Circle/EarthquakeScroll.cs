namespace Server.Items
{
    public class EarthquakeScroll : SpellScroll
    {
        [Constructible]
        public EarthquakeScroll(int amount = 1) : base(56, 0x1F65, amount)
        {
        }

        public EarthquakeScroll(Serial serial) : base(serial)
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
