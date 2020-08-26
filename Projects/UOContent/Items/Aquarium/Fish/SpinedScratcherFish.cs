namespace Server.Items
{
    public class SpinedScratcherFish : BaseFish
    {
        [Constructible]
        public SpinedScratcherFish() : base(0x3B05)
        {
        }

        public SpinedScratcherFish(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073832; // A Spined Scratcher Fish

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
