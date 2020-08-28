namespace Server.Items
{
    public class SpottedBuccaneer : BaseFish
    {
        [Constructible]
        public SpottedBuccaneer() : base(0x3B09)
        {
        }

        public SpottedBuccaneer(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073833; // A Spotted Buccaneer

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
