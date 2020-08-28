namespace Server.Items
{
    public class PurpleFrog : BaseFish
    {
        [Constructible]
        public PurpleFrog() : base(0x3B0D) => Hue = 0x4FA;

        public PurpleFrog(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073823; // A Purple Frog

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
