namespace Server.Items
{
    public class TaintedTreeSample : Item // On OSI the base class is Kindling, and it's ignitable...
    {
        [Constructible]
        public TaintedTreeSample() : base(0xDE2)
        {
            LootType = LootType.Blessed;
            Hue = 0x9D;
        }

        public TaintedTreeSample(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074997; // Tainted Tree Sample

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
