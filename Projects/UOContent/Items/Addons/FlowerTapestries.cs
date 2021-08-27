namespace Server.Items
{
    [Serializable(0, false)]
    public partial class LightFlowerTapestryEastAddon : BaseAddon
    {
        [Constructible]
        public LightFlowerTapestryEastAddon()
        {
            AddComponent(new AddonComponent(0xFDC), 0, 0, 0);
            AddComponent(new AddonComponent(0xFDB), 0, 1, 0);
        }

        public override BaseAddonDeed Deed => new LightFlowerTapestryEastDeed();
    }

    [Serializable(0, false)]
    public partial class LightFlowerTapestryEastDeed : BaseAddonDeed
    {
        [Constructible]
        public LightFlowerTapestryEastDeed()
        {
        }

        public override BaseAddon Addon => new LightFlowerTapestryEastAddon();
        public override int LabelNumber => 1049393; // a flower tapestry deed facing east
    }

    [Serializable(0, false)]
    public partial class LightFlowerTapestrySouthAddon : BaseAddon
    {
        [Constructible]
        public LightFlowerTapestrySouthAddon()
        {
            AddComponent(new AddonComponent(0xFD9), 0, 0, 0);
            AddComponent(new AddonComponent(0xFDA), 1, 0, 0);
        }

        public override BaseAddonDeed Deed => new LightFlowerTapestrySouthDeed();
    }

    [Serializable(0, false)]
    public partial class LightFlowerTapestrySouthDeed : BaseAddonDeed
    {
        [Constructible]
        public LightFlowerTapestrySouthDeed()
        {
        }

        public override BaseAddon Addon => new LightFlowerTapestrySouthAddon();
        public override int LabelNumber => 1049394; // a flower tapestry deed facing south
    }

    [Serializable(0, false)]
    public partial class DarkFlowerTapestryEastAddon : BaseAddon
    {
        [Constructible]
        public DarkFlowerTapestryEastAddon()
        {
            AddComponent(new AddonComponent(0xFE0), 0, 0, 0);
            AddComponent(new AddonComponent(0xFDF), 0, 1, 0);
        }

        public override BaseAddonDeed Deed => new DarkFlowerTapestryEastDeed();
    }

    [Serializable(0, false)]
    public partial class DarkFlowerTapestryEastDeed : BaseAddonDeed
    {
        [Constructible]
        public DarkFlowerTapestryEastDeed()
        {
        }

        public override BaseAddon Addon => new DarkFlowerTapestryEastAddon();
        public override int LabelNumber => 1049395; // a dark flower tapestry deed facing east
    }

    [Serializable(0, false)]
    public partial class DarkFlowerTapestrySouthAddon : BaseAddon
    {
        [Constructible]
        public DarkFlowerTapestrySouthAddon()
        {
            AddComponent(new AddonComponent(0xFDD), 0, 0, 0);
            AddComponent(new AddonComponent(0xFDE), 1, 0, 0);
        }

        public override BaseAddonDeed Deed => new DarkFlowerTapestrySouthDeed();
    }

    [Serializable(0, false)]
    public partial class DarkFlowerTapestrySouthDeed : BaseAddonDeed
    {
        [Constructible]
        public DarkFlowerTapestrySouthDeed()
        {
        }

        public override BaseAddon Addon => new DarkFlowerTapestrySouthAddon();
        public override int LabelNumber => 1049396; // a dark flower tapestry deed facing south
    }
}
