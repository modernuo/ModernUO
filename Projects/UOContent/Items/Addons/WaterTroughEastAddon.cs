namespace Server.Items
{
    public class WaterTroughEastAddon : BaseAddon, IWaterSource
    {
        [Constructible]
        public WaterTroughEastAddon()
        {
            AddComponent(new AddonComponent(0xB41), 0, 0, 0);
            AddComponent(new AddonComponent(0xB42), 0, 1, 0);
        }

        public WaterTroughEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new WaterTroughEastDeed();

        int IHasQuantity.Quantity
        {
            get => 500;
            set { }
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

    public class WaterTroughEastDeed : BaseAddonDeed
    {
        [Constructible]
        public WaterTroughEastDeed()
        {
        }

        public WaterTroughEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new WaterTroughEastAddon();
        public override int LabelNumber => 1044349; // water trough (east)

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
