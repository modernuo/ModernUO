namespace Server.Items
{
    public class MirrorOfPurification : Item
    {
        [Constructible]
        public MirrorOfPurification() : base(0x1008)
        {
            LootType = LootType.Blessed;
            Hue = 0x530;
        }

        public MirrorOfPurification(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075304; // Mirror of Purification

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
