namespace Server.Items
{
    public class GiftForArielle : BaseContainer
    {
        [Constructible]
        public GiftForArielle() : base(0x1882) => Hue = 0x2C4;

        public GiftForArielle(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074356; // gift for arielle
        public override int DefaultGumpID => 0x41;

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
