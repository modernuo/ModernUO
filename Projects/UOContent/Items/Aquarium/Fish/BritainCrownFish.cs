namespace Server.Items
{
    public class BritainCrownFish : BaseFish
    {
        [Constructible]
        public BritainCrownFish() : base(0x3AFF)
        {
        }

        public BritainCrownFish(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074589; // Britain Crown Fish

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
