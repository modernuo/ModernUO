namespace Server.Items
{
    public class KillerFrog : BaseFish
    {
        [Constructible]
        public KillerFrog() : base(0x3B0D)
        {
        }

        public KillerFrog(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073825; // A Killer Frog

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
