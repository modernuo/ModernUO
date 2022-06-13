namespace Server.Items
{
    public class BridesLetter : Item
    {
        [Constructible]
        public BridesLetter() : base(0x14ED) => LootType = LootType.Blessed;

        public BridesLetter(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075301; // Bride's Letter

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
