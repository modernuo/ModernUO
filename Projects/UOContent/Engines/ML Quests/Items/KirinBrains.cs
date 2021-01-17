namespace Server.Items
{
    public class KirinBrains : Item
    {
        [Constructible]
        public KirinBrains() : base(0x1CF0)
        {
            LootType = LootType.Blessed;
            Hue = 0xD7;
        }

        public KirinBrains(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074612; // Ki-Rin Brains

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
