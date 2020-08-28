namespace Server.Items
{
    public class SeaHorseFish : BaseFish
    {
        [Constructible]
        public SeaHorseFish() : base(0x3B10)
        {
        }

        public SeaHorseFish(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074414; // A sea horse

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
