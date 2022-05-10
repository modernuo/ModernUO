using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ArcanistStatueSouthAddon : BaseAddon
    {
        [Constructible]
        public ArcanistStatueSouthAddon()
        {
            AddComponent(new AddonComponent(0x2D0F), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new ArcanistStatueSouthDeed();
    }

    [SerializationGenerator(0)]
    public partial class ArcanistStatueSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public ArcanistStatueSouthDeed()
        {
        }

        public override BaseAddon Addon => new ArcanistStatueSouthAddon();
        public override int LabelNumber => 1072885; // arcanist statue (south)
    }
}
