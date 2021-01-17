namespace Server.Items
{
    public class PeppercornFishsteak : FishSteak
    {
        [Constructible]
        public PeppercornFishsteak() => Hue = 0x222;

        public PeppercornFishsteak(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075557; // peppercorn fishsteak

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // Version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
