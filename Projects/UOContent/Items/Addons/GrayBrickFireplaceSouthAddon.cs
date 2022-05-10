using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class GrayBrickFireplaceSouthAddon : BaseAddon
    {
        [Constructible]
        public GrayBrickFireplaceSouthAddon()
        {
            AddComponent(new AddonComponent(0x94B), -1, 0, 0);
            AddComponent(new AddonComponent(0x945), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new GrayBrickFireplaceSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class GrayBrickFireplaceSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public GrayBrickFireplaceSouthDeed()
        {
        }

        public override BaseAddon Addon => new GrayBrickFireplaceSouthAddon();
        public override int LabelNumber => 1061847; // grey brick fireplace (south)
    }
}
