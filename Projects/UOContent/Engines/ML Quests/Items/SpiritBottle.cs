namespace Server.Items
{
    public class SpiritBottle : Item
    {
        [Constructible]
        public SpiritBottle() : base(0xEFB) => LootType = LootType.Blessed;

        public SpiritBottle(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075283; // Spirit bottle

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
