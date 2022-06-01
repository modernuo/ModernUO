namespace Server.Items
{
    public class PortraitOfTheBride : Item
    {
        [Constructible]
        public PortraitOfTheBride() : base(0xE9F) => LootType = LootType.Blessed;

        public PortraitOfTheBride(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075300; // Portrait of the Bride

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
