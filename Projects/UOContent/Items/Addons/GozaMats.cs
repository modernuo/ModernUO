using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class GozaMatEastAddon : BaseAddon
    {
        [Constructible]
        public GozaMatEastAddon(int hue = 0)
        {
            AddComponent(new LocalizedAddonComponent(0x28a4, 1030688), 1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x28a5, 1030688), 0, 0, 0);
            Hue = hue;
        }

        public override BaseAddonDeed Deed => new GozaMatEastDeed();

        public override bool RetainDeedHue => true;
    }

    [SerializationGenerator(0, false)]
    public partial class GozaMatEastDeed : BaseAddonDeed
    {
        [Constructible]
        public GozaMatEastDeed()
        {
        }

        public override BaseAddon Addon => new GozaMatEastAddon(Hue);
        public override int LabelNumber => 1030404; // goza (east)
    }

    [SerializationGenerator(0, false)]
    public partial class GozaMatSouthAddon : BaseAddon
    {
        [Constructible]
        public GozaMatSouthAddon(int hue = 0)
        {
            AddComponent(new LocalizedAddonComponent(0x28a6, 1030688), 0, 1, 0);
            AddComponent(new LocalizedAddonComponent(0x28a7, 1030688), 0, 0, 0);
            Hue = hue;
        }

        public override BaseAddonDeed Deed => new GozaMatSouthDeed();

        public override bool RetainDeedHue => true;
    }

    [SerializationGenerator(0, false)]
    public partial class GozaMatSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public GozaMatSouthDeed()
        {
        }

        public override BaseAddon Addon => new GozaMatSouthAddon(Hue);
        public override int LabelNumber => 1030405; // goza (south)
    }

    [SerializationGenerator(0, false)]
    public partial class SquareGozaMatEastAddon : BaseAddon
    {
        [Constructible]
        public SquareGozaMatEastAddon(int hue = 0)
        {
            AddComponent(new LocalizedAddonComponent(0x28a8, 1030688), 0, 0, 0);
            Hue = hue;
        }

        public override BaseAddonDeed Deed => new SquareGozaMatEastDeed();
        public override int LabelNumber => 1030688; // goza mat

        public override bool RetainDeedHue => true;
    }

    [SerializationGenerator(0, false)]
    public partial class SquareGozaMatEastDeed : BaseAddonDeed
    {
        [Constructible]
        public SquareGozaMatEastDeed()
        {
        }

        public override BaseAddon Addon => new SquareGozaMatEastAddon(Hue);
        public override int LabelNumber => 1030407; // square goza (east)
    }

    [SerializationGenerator(0, false)]
    public partial class SquareGozaMatSouthAddon : BaseAddon
    {
        [Constructible]
        public SquareGozaMatSouthAddon(int hue = 0)
        {
            AddComponent(new LocalizedAddonComponent(0x28a9, 1030688), 0, 0, 0);
            Hue = hue;
        }

        public override BaseAddonDeed Deed => new SquareGozaMatSouthDeed();

        public override bool RetainDeedHue => true;
    }

    [SerializationGenerator(0, false)]
    public partial class SquareGozaMatSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public SquareGozaMatSouthDeed()
        {
        }

        public override BaseAddon Addon => new SquareGozaMatSouthAddon(Hue);
        public override int LabelNumber => 1030406; // square goza (south)
    }

    [SerializationGenerator(0, false)]
    public partial class BrocadeGozaMatEastAddon : BaseAddon
    {
        [Constructible]
        public BrocadeGozaMatEastAddon(int hue = 0)
        {
            AddComponent(new LocalizedAddonComponent(0x28AB, 1030688), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x28AA, 1030688), 1, 0, 0);
            Hue = hue;
        }

        public override BaseAddonDeed Deed => new BrocadeGozaMatEastDeed();

        public override bool RetainDeedHue => true;
    }

    [SerializationGenerator(0, false)]
    public partial class BrocadeGozaMatEastDeed : BaseAddonDeed
    {
        [Constructible]
        public BrocadeGozaMatEastDeed()
        {
        }

        public override BaseAddon Addon => new BrocadeGozaMatEastAddon(Hue);
        public override int LabelNumber => 1030408; // brocade goza (east)
    }

    [SerializationGenerator(0, false)]
    public partial class BrocadeGozaMatSouthAddon : BaseAddon
    {
        [Constructible]
        public BrocadeGozaMatSouthAddon(int hue = 0)
        {
            AddComponent(new LocalizedAddonComponent(0x28AD, 1030688), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x28AC, 1030688), 0, 1, 0);
            Hue = hue;
        }

        public override BaseAddonDeed Deed => new BrocadeGozaMatSouthDeed();

        public override bool RetainDeedHue => true;
    }

    [SerializationGenerator(0, false)]
    public partial class BrocadeGozaMatSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public BrocadeGozaMatSouthDeed()
        {
        }

        public override BaseAddon Addon => new BrocadeGozaMatSouthAddon(Hue);
        public override int LabelNumber => 1030409; // brocade goza (south)
    }

    [SerializationGenerator(0, false)]
    public partial class BrocadeSquareGozaMatEastAddon : BaseAddon
    {
        [Constructible]
        public BrocadeSquareGozaMatEastAddon(int hue = 0)
        {
            AddComponent(new LocalizedAddonComponent(0x28AE, 1030688), 0, 0, 0);
            Hue = hue;
        }

        public override BaseAddonDeed Deed => new BrocadeSquareGozaMatEastDeed();

        public override bool RetainDeedHue => true;
    }

    [SerializationGenerator(0, false)]
    public partial class BrocadeSquareGozaMatEastDeed : BaseAddonDeed
    {
        [Constructible]
        public BrocadeSquareGozaMatEastDeed()
        {
        }

        public override BaseAddon Addon => new BrocadeSquareGozaMatEastAddon(Hue);
        public override int LabelNumber => 1030411; // brocade square goza (east)
    }

    [SerializationGenerator(0, false)]
    public partial class BrocadeSquareGozaMatSouthAddon : BaseAddon
    {
        [Constructible]
        public BrocadeSquareGozaMatSouthAddon(int hue = 0)
        {
            AddComponent(new LocalizedAddonComponent(0x28AF, 1030688), 0, 0, 0);
            Hue = hue;
        }

        public override BaseAddonDeed Deed => new BrocadeSquareGozaMatSouthDeed();

        public override bool RetainDeedHue => true;
    }

    [SerializationGenerator(0, false)]
    public partial class BrocadeSquareGozaMatSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public BrocadeSquareGozaMatSouthDeed()
        {
        }

        public override BaseAddon Addon => new BrocadeSquareGozaMatSouthAddon(Hue);
        public override int LabelNumber => 1030410; // brocade square goza (south)
    }
}
