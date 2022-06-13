namespace Server.Items
{
    public class ReginasRing : SilverRing
    {
        [Constructible]
        public ReginasRing() => LootType = LootType.Blessed;

        public ReginasRing(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075305; // Regina's Ring

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
