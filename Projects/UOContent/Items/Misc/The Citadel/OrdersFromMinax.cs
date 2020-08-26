namespace Server.Items
{
    public class OrdersFromMinax : Item
    {
        [Constructible]
        public OrdersFromMinax() : base(0x2279) => LootType = LootType.Blessed;

        public OrdersFromMinax(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074639; // Orders from Minax

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
