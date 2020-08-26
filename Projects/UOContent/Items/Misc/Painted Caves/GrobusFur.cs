namespace Server.Items
{
    public class GrobusFur : Item
    {
        [Constructible]
        public GrobusFur() : base(0x11F4)
        {
            LootType = LootType.Blessed;
            Hue = 0x455;
        }

        public GrobusFur(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074676; // Grobu's Fur

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
