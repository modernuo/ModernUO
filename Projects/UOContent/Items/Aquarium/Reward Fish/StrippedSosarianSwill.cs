namespace Server.Items
{
    public class StrippedSosarianSwill : BaseFish
    {
        [Constructible]
        public StrippedSosarianSwill() : base(0x3B0A)
        {
        }

        public StrippedSosarianSwill(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074594; // Stripped Sosarian Swill

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
