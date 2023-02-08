using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ArcaneCircleAddon : BaseAddon
    {
        [Constructible]
        public ArcaneCircleAddon()
        {
            AddComponent(new AddonComponent(0x3083), -1, -1, 0);
            AddComponent(new AddonComponent(0x3080), -1, 0, 0);
            AddComponent(new AddonComponent(0x3082), 0, -1, 0);
            AddComponent(new AddonComponent(0x3081), 1, -1, 0);
            AddComponent(new AddonComponent(0x307D), -1, 1, 0);
            AddComponent(new AddonComponent(0x307F), 0, 0, 0);
            AddComponent(new AddonComponent(0x307E), 1, 0, 0);
            AddComponent(new AddonComponent(0x307C), 0, 1, 0);
            AddComponent(new AddonComponent(0x307B), 1, 1, 0);
        }

        public override BaseAddonDeed Deed => new ArcaneCircleDeed();
    }

    [SerializationGenerator(0)]
    public partial class ArcaneCircleDeed : BaseAddonDeed
    {
        [Constructible]
        public ArcaneCircleDeed()
        {
        }

        public override BaseAddon Addon => new ArcaneCircleAddon();
        public override int LabelNumber => 1072703; // arcane circle
    }
}
