using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class BrownBearRugEastAddon : BaseAddon
    {
        [Constructible]
        public BrownBearRugEastAddon()
        {
            AddComponent(new AddonComponent(0x1E40), 1, 1, 0);
            AddComponent(new AddonComponent(0x1E41), 1, 0, 0);
            AddComponent(new AddonComponent(0x1E42), 1, -1, 0);
            AddComponent(new AddonComponent(0x1E43), 0, -1, 0);
            AddComponent(new AddonComponent(0x1E44), 0, 0, 0);
            AddComponent(new AddonComponent(0x1E45), 0, 1, 0);
            AddComponent(new AddonComponent(0x1E46), -1, 1, 0);
            AddComponent(new AddonComponent(0x1E47), -1, 0, 0);
            AddComponent(new AddonComponent(0x1E48), -1, -1, 0);
        }

        public override BaseAddonDeed Deed => new BrownBearRugEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class BrownBearRugEastDeed : BaseAddonDeed
    {
        [Constructible]
        public BrownBearRugEastDeed()
        {
        }

        public override BaseAddon Addon => new BrownBearRugEastAddon();
        public override int LabelNumber => 1049397; // a brown bear rug deed facing east
    }

    [SerializationGenerator(0, false)]
    public partial class BrownBearRugSouthAddon : BaseAddon
    {
        [Constructible]
        public BrownBearRugSouthAddon()
        {
            AddComponent(new AddonComponent(0x1E36), 1, 1, 0);
            AddComponent(new AddonComponent(0x1E37), 0, 1, 0);
            AddComponent(new AddonComponent(0x1E38), -1, 1, 0);
            AddComponent(new AddonComponent(0x1E39), -1, 0, 0);
            AddComponent(new AddonComponent(0x1E3A), 0, 0, 0);
            AddComponent(new AddonComponent(0x1E3B), 1, 0, 0);
            AddComponent(new AddonComponent(0x1E3C), 1, -1, 0);
            AddComponent(new AddonComponent(0x1E3D), 0, -1, 0);
            AddComponent(new AddonComponent(0x1E3E), -1, -1, 0);
        }

        public override BaseAddonDeed Deed => new BrownBearRugSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class BrownBearRugSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public BrownBearRugSouthDeed()
        {
        }

        public override BaseAddon Addon => new BrownBearRugSouthAddon();
        public override int LabelNumber => 1049398; // a brown bear rug deed facing south
    }

    [SerializationGenerator(0, false)]
    public partial class PolarBearRugEastAddon : BaseAddon
    {
        [Constructible]
        public PolarBearRugEastAddon()
        {
            AddComponent(new AddonComponent(0x1E53), 1, 1, 0);
            AddComponent(new AddonComponent(0x1E54), 1, 0, 0);
            AddComponent(new AddonComponent(0x1E55), 1, -1, 0);
            AddComponent(new AddonComponent(0x1E56), 0, -1, 0);
            AddComponent(new AddonComponent(0x1E57), 0, 0, 0);
            AddComponent(new AddonComponent(0x1E58), 0, 1, 0);
            AddComponent(new AddonComponent(0x1E59), -1, 1, 0);
            AddComponent(new AddonComponent(0x1E5A), -1, 0, 0);
            AddComponent(new AddonComponent(0x1E5B), -1, -1, 0);
        }

        public override BaseAddonDeed Deed => new PolarBearRugEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class PolarBearRugEastDeed : BaseAddonDeed
    {
        [Constructible]
        public PolarBearRugEastDeed()
        {
        }

        public override BaseAddon Addon => new PolarBearRugEastAddon();
        public override int LabelNumber => 1049399; // a polar bear rug deed facing east
    }

    [SerializationGenerator(0, false)]
    public partial class PolarBearRugSouthAddon : BaseAddon
    {
        [Constructible]
        public PolarBearRugSouthAddon()
        {
            AddComponent(new AddonComponent(0x1E49), 1, 1, 0);
            AddComponent(new AddonComponent(0x1E4A), 0, 1, 0);
            AddComponent(new AddonComponent(0x1E4B), -1, 1, 0);
            AddComponent(new AddonComponent(0x1E4C), -1, 0, 0);
            AddComponent(new AddonComponent(0x1E4D), 0, 0, 0);
            AddComponent(new AddonComponent(0x1E4E), 1, 0, 0);
            AddComponent(new AddonComponent(0x1E4F), 1, -1, 0);
            AddComponent(new AddonComponent(0x1E50), 0, -1, 0);
            AddComponent(new AddonComponent(0x1E51), -1, -1, 0);
        }

        public override BaseAddonDeed Deed => new PolarBearRugSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class PolarBearRugSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public PolarBearRugSouthDeed()
        {
        }

        public override BaseAddon Addon => new PolarBearRugSouthAddon();
        public override int LabelNumber => 1049400; // a polar bear rug deed facing south
    }
}
