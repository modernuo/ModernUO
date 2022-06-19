using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class WaterTroughSouthAddon : BaseAddon, IWaterSource
    {
        [Constructible]
        public WaterTroughSouthAddon()
        {
            AddComponent(new AddonComponent(0xB43), 0, 0, 0);
            AddComponent(new AddonComponent(0xB44), 1, 0, 0);
        }

        public override BaseAddonDeed Deed => new WaterTroughSouthDeed();

        int IHasQuantity.Quantity
        {
            get => 500;
            set { }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class WaterTroughSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public WaterTroughSouthDeed()
        {
        }

        public override BaseAddon Addon => new WaterTroughSouthAddon();
        public override int LabelNumber => 1044350; // water trough (south)
    }
}
