using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LoomSouthAddon : BaseAddon, ILoom
    {
        [SerializableField(0)]
        private int _phase;

        [Constructible]
        public LoomSouthAddon()
        {
            AddComponent(new AddonComponent(0x1061), 0, 0, 0);
            AddComponent(new AddonComponent(0x1062), 1, 0, 0);
        }

        public override BaseAddonDeed Deed => new LoomSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class LoomSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public LoomSouthDeed()
        {
        }

        public override BaseAddon Addon => new LoomSouthAddon();
        public override int LabelNumber => 1044344; // loom (south)
    }
}
