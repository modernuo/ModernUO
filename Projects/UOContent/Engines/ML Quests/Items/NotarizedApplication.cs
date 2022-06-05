namespace Server.Items
{
    public class NotarizedApplication : Item
    {
        [Constructible]
        public NotarizedApplication() : base(0x14EF) => LootType = LootType.Blessed;

        public NotarizedApplication(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073135; // Notarized Application

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
