namespace Server.Items
{
    public class GamanHorns : Item
    {
        [Constructible]
        public GamanHorns(int amount = 1) : base(0x1084)
        {
            LootType = LootType.Blessed;
            Stackable = true;
            Amount = amount;
            Hue = 0x395;
        }

        public GamanHorns(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074557; // Gaman Horns

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
