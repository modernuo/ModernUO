namespace Server.Items
{
    public class YellowFinBluebelly : BaseFish
    {
        [Constructible]
        public YellowFinBluebelly() : base(0x3B07)
        {
        }

        public YellowFinBluebelly(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073831; // A Yellow Fin Bluebelly

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
