namespace Server.Items
{
    public class SealingWaxOrderAddressedToPetrus : Item
    {
        [Constructible]
        public SealingWaxOrderAddressedToPetrus() : base(0xEBF) => LootType = LootType.Blessed;

        public SealingWaxOrderAddressedToPetrus(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073132; // Sealing Wax Order addressed to Petrus

        public override bool Nontransferable => true;

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);
            AddQuestItemProperty(list);
        }

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
