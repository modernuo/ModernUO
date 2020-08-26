namespace Server.Items
{
    public class LuckyNecklace : BaseJewel
    {
        [Constructible]
        public LuckyNecklace()
            : base(0x1088, Layer.Neck)
        {
            Attributes.Luck = 200;
            LootType = LootType.Blessed;
        }

        public LuckyNecklace(Serial serial) : base(serial)
        {
        }

        public override int Hue => 1150;
        public override int LabelNumber => 1075239; // Lucky Necklace	1075239

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadInt(); /* int version = reader.ReadInt(); Why? Just to have an unused var? */
        }
    }
}
