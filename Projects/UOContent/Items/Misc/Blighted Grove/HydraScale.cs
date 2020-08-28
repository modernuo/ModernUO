namespace Server.Items
{
    public class HydraScale : Item
    {
        [Constructible]
        public HydraScale() : base(0x26B4)
        {
            LootType = LootType.Blessed;
            Hue = 0xC2; // TODO check
        }

        public HydraScale(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074760; // A hydra scale.

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
