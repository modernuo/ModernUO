namespace Server.Items
{
    public class RedDartFish : BaseFish
    {
        [Constructible]
        public RedDartFish() : base(0x3B00)
        {
        }

        public RedDartFish(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073834; // A Red Dart Fish

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
