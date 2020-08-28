namespace Server.Items
{
    public class FullMoonFish : BaseFish
    {
        [Constructible]
        public FullMoonFish() : base(0x3B15)
        {
        }

        public FullMoonFish(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074597; // A Full Moon Fish

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
