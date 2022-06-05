namespace Server.Items
{
    public class DreadSpiderSilk : Item
    {
        [Constructible]
        public DreadSpiderSilk() : base(0xDF8)
        {
            LootType = LootType.Blessed;
            Hue = 0x481;
        }

        public DreadSpiderSilk(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075319; // Dread Spider Silk

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
