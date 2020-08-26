namespace Server.Items
{
    internal class Tub : BaseWaterContainer
    {
        private static readonly int vItemID = 0xe83;
        private static readonly int fItemID = 0xe7b;

        [Constructible]
        public Tub(bool filled = false)
            : base(filled ? fItemID : vItemID, filled)
        {
        }

        public Tub(Serial serial)
            : base(serial)
        {
        }

        public override int voidItem_ID => vItemID;
        public override int fullItem_ID => fItemID;
        public override int MaxQuantity => 50;

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
