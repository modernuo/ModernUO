namespace Server.Items
{
    internal class Bucket : BaseWaterContainer
    {
        private static readonly int vItemID = 0x14e0;
        private static readonly int fItemID = 0x2004;

        [Constructible]
        public Bucket(bool filled = false)
            : base(filled ? fItemID : vItemID, filled)
        {
        }

        public Bucket(Serial serial)
            : base(serial)
        {
        }

        public override int voidItem_ID => vItemID;
        public override int fullItem_ID => fItemID;
        public override int MaxQuantity => 25;

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
