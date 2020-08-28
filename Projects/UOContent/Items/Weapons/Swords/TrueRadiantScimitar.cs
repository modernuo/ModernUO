namespace Server.Items
{
    public class TrueRadiantScimitar : RadiantScimitar
    {
        [Constructible]
        public TrueRadiantScimitar() => Attributes.NightSight = 1;

        public TrueRadiantScimitar(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073541; // true radiant scimitar

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
