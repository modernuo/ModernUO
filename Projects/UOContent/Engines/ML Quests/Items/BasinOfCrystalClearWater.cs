namespace Server.Items
{
    public class BasinOfCrystalClearWater : Item
    {
        [Constructible]
        public BasinOfCrystalClearWater() : base(0x1008) => LootType = LootType.Blessed;

        public BasinOfCrystalClearWater(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075303; // Basin of Crystal-Clear Water

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
