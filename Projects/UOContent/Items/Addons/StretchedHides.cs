using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SmallStretchedHideEastAddon : BaseAddon
    {
        [Constructible]
        public SmallStretchedHideEastAddon()
        {
            AddComponent(new AddonComponent(0x1069), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new SmallStretchedHideEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class SmallStretchedHideEastDeed : BaseAddonDeed
    {
        [Constructible]
        public SmallStretchedHideEastDeed()
        {
        }

        public override BaseAddon Addon => new SmallStretchedHideEastAddon();
        public override int LabelNumber => 1049401; // a small stretched hide deed facing east
    }

    [SerializationGenerator(0, false)]
    public partial class SmallStretchedHideSouthAddon : BaseAddon
    {
        [Constructible]
        public SmallStretchedHideSouthAddon()
        {
            AddComponent(new AddonComponent(0x107A), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new SmallStretchedHideSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class SmallStretchedHideSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public SmallStretchedHideSouthDeed()
        {
        }

        public override BaseAddon Addon => new SmallStretchedHideSouthAddon();
        public override int LabelNumber => 1049402; // a small stretched hide deed facing south
    }

    [SerializationGenerator(0, false)]
    public partial class MediumStretchedHideEastAddon : BaseAddon
    {
        [Constructible]
        public MediumStretchedHideEastAddon()
        {
            AddComponent(new AddonComponent(0x106B), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new MediumStretchedHideEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class MediumStretchedHideEastDeed : BaseAddonDeed
    {
        [Constructible]
        public MediumStretchedHideEastDeed()
        {
        }

        public override BaseAddon Addon => new MediumStretchedHideEastAddon();
        public override int LabelNumber => 1049403; // a medium stretched hide deed facing east
    }

    [SerializationGenerator(0, false)]
    public partial class MediumStretchedHideSouthAddon : BaseAddon
    {
        [Constructible]
        public MediumStretchedHideSouthAddon()
        {
            AddComponent(new AddonComponent(0x107C), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new MediumStretchedHideSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class MediumStretchedHideSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public MediumStretchedHideSouthDeed()
        {
        }

        public override BaseAddon Addon => new MediumStretchedHideSouthAddon();
        public override int LabelNumber => 1049404; // a medium stretched hide deed facing south
    }
}
