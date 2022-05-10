using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class StoneFireplaceSouthAddon : BaseAddon
    {
        [Constructible]
        public StoneFireplaceSouthAddon()
        {
            AddComponent(new AddonComponent(0x967), -1, 0, 0);
            AddComponent(new AddonComponent(0x961), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new StoneFireplaceSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class StoneFireplaceSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public StoneFireplaceSouthDeed()
        {
        }

        public override BaseAddon Addon => new StoneFireplaceSouthAddon();
        public override int LabelNumber => 1061849; // stone fireplace (south)
    }
}
