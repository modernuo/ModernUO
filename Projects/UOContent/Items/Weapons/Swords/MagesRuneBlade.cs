namespace Server.Items
{
    public class MagesRuneBlade : RuneBlade
    {
        [Constructible]
        public MagesRuneBlade() => Attributes.CastSpeed = 1;

        public MagesRuneBlade(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073538; // mage's rune blade

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
