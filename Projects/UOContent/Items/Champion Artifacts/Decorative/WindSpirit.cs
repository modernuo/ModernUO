namespace Server.Items
{
    public class WindSpirit : Item
    {
        [Constructible]
        public WindSpirit() : base(0x1F1F)
        {
        }

        public WindSpirit(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094925; // Wind Spirit [Replica]

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
