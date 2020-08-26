namespace Server.Items
{
    public class LightweightShortbow : MagicalShortbow
    {
        [Constructible]
        public LightweightShortbow() => Balanced = true;

        public LightweightShortbow(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073510; // lightweight shortbow

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
