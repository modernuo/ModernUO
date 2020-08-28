namespace Server.Items
{
    public class Coral : BaseFish
    {
        [Constructible]
        public Coral() : base(Utility.RandomList(0x3AF9, 0x3AFA, 0x3AFB))
        {
        }

        public Coral(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074588; // Coral

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
