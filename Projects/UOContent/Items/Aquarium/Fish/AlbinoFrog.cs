namespace Server.Items
{
    public class AlbinoFrog : BaseFish
    {
        [Constructible]
        public AlbinoFrog() : base(0x3B0D) => Hue = 0x47E;

        public AlbinoFrog(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073824; // An Albino Frog

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
