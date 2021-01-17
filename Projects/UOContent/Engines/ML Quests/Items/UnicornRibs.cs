namespace Server.Items
{
    public class UnicornRibs : Item
    {
        [Constructible]
        public UnicornRibs(int amount = 1) : base(0x9F1)
        {
            LootType = LootType.Blessed;
            Hue = 0x14B;
            Stackable = true;
            Amount = amount;
        }

        public UnicornRibs(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074611; // Unicorn Ribs

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
