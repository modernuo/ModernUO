namespace Server.Items
{
    public class FestiveCactus : Item
    {
        [Constructible]
        public FestiveCactus() : base(0x2376)
        {
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public FestiveCactus(Serial serial) : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, 1070880); // Winter 2004
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1070880); // Winter 2004
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
