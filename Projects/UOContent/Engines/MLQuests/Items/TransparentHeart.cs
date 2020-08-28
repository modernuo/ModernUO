namespace Server.Items
{
    public class TransparentHeart : GoldEarrings
    {
        [Constructible]
        public TransparentHeart()
        {
            LootType = LootType.Blessed;
            Hue = 0x4AB;
        }

        public TransparentHeart(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075400; // Transparent Heart

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
