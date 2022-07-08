using ModernUO.Serialization;

namespace Server.Items
{
    public interface ILoom
    {
        int Phase { get; set; }
    }

    [SerializationGenerator(0, false)]
    public partial class LoomEastAddon : BaseAddon, ILoom
    {
        [SerializableField(0)]
        private int _phase;

        [Constructible]
        public LoomEastAddon()
        {
            AddComponent(new AddonComponent(0x1060), 0, 0, 0);
            AddComponent(new AddonComponent(0x105F), 0, 1, 0);
        }

        public override BaseAddonDeed Deed => new LoomEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class LoomEastDeed : BaseAddonDeed
    {
        [Constructible]
        public LoomEastDeed()
        {
        }

        public override BaseAddon Addon => new LoomEastAddon();
        public override int LabelNumber => 1044343; // loom (east)
    }
}
