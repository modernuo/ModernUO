using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ParrotPerchAddon : BaseAddon
    {
        [Constructible]
        public ParrotPerchAddon()
        {
            AddComponent(new AddonComponent(0x2FF4), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new ParrotPerchDeed();
    }

    [SerializationGenerator(0)]
    public partial class ParrotPerchDeed : BaseAddonDeed
    {
        [Constructible]
        public ParrotPerchDeed()
        {
        }

        public override BaseAddon Addon => new ParrotPerchAddon();
        public override int LabelNumber => 1072617; // parrot perch
    }
}
