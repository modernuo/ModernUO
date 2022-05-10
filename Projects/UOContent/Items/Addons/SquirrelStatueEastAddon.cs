using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class SquirrelStatueEastAddon : BaseAddon
    {
        [Constructible]
        public SquirrelStatueEastAddon()
        {
            AddComponent(new AddonComponent(0x2D10), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new SquirrelStatueEastDeed();
    }

    [SerializationGenerator(0)]
    public partial class SquirrelStatueEastDeed : BaseAddonDeed
    {
        [Constructible]
        public SquirrelStatueEastDeed()
        {
        }

        public override BaseAddon Addon => new SquirrelStatueEastAddon();
        public override int LabelNumber => 1073398; // squirrel statue (east)
    }
}
