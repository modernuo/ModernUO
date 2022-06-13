using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class WarriorStatueSouthAddon : BaseAddon
    {
        [Constructible]
        public WarriorStatueSouthAddon()
        {
            AddComponent(new AddonComponent(0x2D13), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new WarriorStatueSouthDeed();
    }

    [SerializationGenerator(0)]
    public partial class WarriorStatueSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public WarriorStatueSouthDeed()
        {
        }

        public override BaseAddon Addon => new WarriorStatueSouthAddon();
        public override int LabelNumber => 1072887; // warrior statue (south)
    }
}
