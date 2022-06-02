namespace Server.Items
{
    public class AlchemistsBandage : Item
    {
        [Constructible]
        public AlchemistsBandage() : base(0xE21)
        {
            LootType = LootType.Blessed;
            Hue = 0x482;
        }

        public AlchemistsBandage(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075452; // Alchemist's Bandage

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
