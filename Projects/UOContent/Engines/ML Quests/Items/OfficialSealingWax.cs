namespace Server.Items
{
    public class OfficialSealingWax : Item
    {
        [Constructible]
        public OfficialSealingWax() : base(0x1426)
        {
            LootType = LootType.Blessed;
            Hue = 0x84;
        }

        public OfficialSealingWax(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072744; // Official Sealing Wax

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
