using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class WaterTroughEastAddon : BaseAddon, IWaterSource
    {
        [Constructible]
        public WaterTroughEastAddon()
        {
            AddComponent(new AddonComponent(0xB41), 0, 0, 0);
            AddComponent(new AddonComponent(0xB42), 0, 1, 0);
        }

        public override BaseAddonDeed Deed => new WaterTroughEastDeed();

        int IHasQuantity.Quantity
        {
            get => 500;
            set { }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class WaterTroughEastDeed : BaseAddonDeed
    {
        [Constructible]
        public WaterTroughEastDeed()
        {
        }

        public override BaseAddon Addon => new WaterTroughEastAddon();
        public override int LabelNumber => 1044349; // water trough (east)
    }
}
