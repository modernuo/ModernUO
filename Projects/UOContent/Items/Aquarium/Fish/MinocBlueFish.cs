namespace Server.Items
{
    public class MinocBlueFish : BaseFish
    {
        [Constructible]
        public MinocBlueFish() : base(0x3AFE)
        {
        }

        public MinocBlueFish(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073829; // A Minoc Blue Fish

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
