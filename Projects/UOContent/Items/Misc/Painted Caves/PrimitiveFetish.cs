namespace Server.Items
{
    public class PrimitiveFetish : Item
    {
        [Constructible]
        public PrimitiveFetish() : base(0x23F)
        {
            LootType = LootType.Blessed;
            Hue = 0x244;
        }

        public PrimitiveFetish(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074675; // Primitive Fetish

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
