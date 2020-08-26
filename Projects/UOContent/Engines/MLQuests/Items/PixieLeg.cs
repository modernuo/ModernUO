namespace Server.Items
{
    public class PixieLeg : ChickenLeg
    {
        [Constructible]
        public PixieLeg(int amount = 1) : base(amount)
        {
            LootType = LootType.Blessed;
            Hue = 0x1C2;
        }

        public PixieLeg(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074613; // Pixie Leg

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
