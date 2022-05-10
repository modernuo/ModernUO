using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LargeForgeSouthAddon : BaseAddon
    {
        [Constructible]
        public LargeForgeSouthAddon()
        {
            AddComponent(new ForgeComponent(0x197A), 0, 0, 0);
            AddComponent(new ForgeComponent(0x197E), 1, 0, 0);
            AddComponent(new ForgeComponent(0x19A2), 2, 0, 0);
            AddComponent(new ForgeComponent(0x199E), 3, 0, 0);
        }

        public override BaseAddonDeed Deed => new LargeForgeSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class LargeForgeSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public LargeForgeSouthDeed()
        {
        }

        public override BaseAddon Addon => new LargeForgeSouthAddon();
        public override int LabelNumber => 1044332; // large forge (south)
    }
}
