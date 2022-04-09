using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class AbbatoirAddon : BaseAddon
    {
        [Constructible]
        public AbbatoirAddon()
        {
            AddComponent(new AddonComponent(0x120E), -1, -1, 0);
            AddComponent(new AddonComponent(0x120F), 0, -1, 0);
            AddComponent(new AddonComponent(0x1210), 1, -1, 0);
            AddComponent(new AddonComponent(0x1215), -1, 0, 0);
            AddComponent(new AddonComponent(0x1216), 0, 0, 0);
            AddComponent(new AddonComponent(0x1211), 1, 0, 0);
            AddComponent(new AddonComponent(0x1214), -1, 1, 0);
            AddComponent(new AddonComponent(0x1213), 0, 1, 0);
            AddComponent(new AddonComponent(0x1212), 1, 1, 0);
        }

        public override BaseAddonDeed Deed => new AbbatoirDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class AbbatoirDeed : BaseAddonDeed
    {
        [Constructible]
        public AbbatoirDeed()
        {
        }

        public override BaseAddon Addon => new AbbatoirAddon();
        public override int LabelNumber => 1044329; // abbatoir
    }
}
