namespace Server.Items
{
    public class SapOfSosaria : Item
    {
        [Constructible]
        public SapOfSosaria(int amount = 1) : base(0x1848)
        {
            LootType = LootType.Blessed;
            Stackable = true;
            Amount = amount;
        }

        public SapOfSosaria(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074178; // Sap of Sosaria

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
