using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class PentagramAddon : BaseAddon
    {
        [Constructible]
        public PentagramAddon()
        {
            AddComponent(new AddonComponent(0xFE7), -1, -1, 0);
            AddComponent(new AddonComponent(0xFE8), 0, -1, 0);
            AddComponent(new AddonComponent(0xFEB), 1, -1, 0);
            AddComponent(new AddonComponent(0xFE6), -1, 0, 0);
            AddComponent(new AddonComponent(0xFEA), 0, 0, 0);
            AddComponent(new AddonComponent(0xFEE), 1, 0, 0);
            AddComponent(new AddonComponent(0xFE9), -1, 1, 0);
            AddComponent(new AddonComponent(0xFEC), 0, 1, 0);
            AddComponent(new AddonComponent(0xFED), 1, 1, 0);
        }

        public override BaseAddonDeed Deed => new PentagramDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class PentagramDeed : BaseAddonDeed
    {
        [Constructible]
        public PentagramDeed()
        {
        }

        public override BaseAddon Addon => new PentagramAddon();
        public override int LabelNumber => 1044328; // pentagram
    }
}
