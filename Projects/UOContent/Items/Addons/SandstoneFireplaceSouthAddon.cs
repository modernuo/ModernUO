using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SandstoneFireplaceSouthAddon : BaseAddon
    {
        [Constructible]
        public SandstoneFireplaceSouthAddon()
        {
            AddComponent(new AddonComponent(0x482), -1, 0, 0);
            AddComponent(new AddonComponent(0x47B), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new SandstoneFireplaceSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class SandstoneFireplaceSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public SandstoneFireplaceSouthDeed()
        {
        }

        public override BaseAddon Addon => new SandstoneFireplaceSouthAddon();
        public override int LabelNumber => 1061845; // sandstone fireplace (south)
    }
}
