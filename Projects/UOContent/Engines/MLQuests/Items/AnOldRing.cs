namespace Server.Items
{
    public class AnOldRing : GoldRing
    {
        [Constructible]
        public AnOldRing() => Hue = 0x222;

        public AnOldRing(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075524; // an old ring

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
