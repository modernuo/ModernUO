namespace Server.Items
{
    public class FandancerFish : BaseFish
    {
        [Constructible]
        public FandancerFish() : base(0x3B02)
        {
        }

        public FandancerFish(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074591; // Fandancer Fish

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
