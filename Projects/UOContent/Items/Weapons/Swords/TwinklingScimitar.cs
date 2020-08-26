namespace Server.Items
{
    public class TwinklingScimitar : RadiantScimitar
    {
        [Constructible]
        public TwinklingScimitar() => Attributes.DefendChance = 6;

        public TwinklingScimitar(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073544; // twinkling scimitar

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
