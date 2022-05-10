using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class WarriorStatueEastAddon : BaseAddon
    {
        [Constructible]
        public WarriorStatueEastAddon()
        {
            AddComponent(new AddonComponent(0x2D12), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new WarriorStatueEastDeed();
    }

    [SerializationGenerator(0)]
    public partial class WarriorStatueEastDeed : BaseAddonDeed
    {
        [Constructible]
        public WarriorStatueEastDeed()
        {
        }

        public override BaseAddon Addon => new WarriorStatueEastAddon();
        public override int LabelNumber => 1072888; // warrior statue (east)
    }
}
