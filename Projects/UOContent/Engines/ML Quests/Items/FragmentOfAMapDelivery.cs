namespace Server.Items
{
    public class FragmentOfAMapDelivery : Item
    {
        [Constructible]
        public FragmentOfAMapDelivery() : base(0x14ED) => LootType = LootType.Blessed;

        public FragmentOfAMapDelivery(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074533; // Fragment of a Map

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
