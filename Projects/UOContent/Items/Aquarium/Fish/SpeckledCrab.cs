namespace Server.Items
{
    public class SpeckledCrab : BaseFish
    {
        [Constructible]
        public SpeckledCrab() : base(0x3AFC)
        {
        }

        public SpeckledCrab(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073826; // A Speckled Crab

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
