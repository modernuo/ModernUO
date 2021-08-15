namespace Server.Items
{
    public class OrnateElvenTableSouthAddon : BaseAddon
    {
        [Constructible]
        public OrnateElvenTableSouthAddon()
        {
            AddComponent(new AddonComponent(0x308F), 0, 1, 0);
            AddComponent(new AddonComponent(0x3090), 0, 0, 0);
            AddComponent(new AddonComponent(0x3091), 0, -1, 0);
        }

        public OrnateElvenTableSouthAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new OrnateElvenTableSouthDeed();


    }

    public class OrnateElvenTableSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public OrnateElvenTableSouthDeed()
        {
        }

        public OrnateElvenTableSouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new OrnateElvenTableSouthAddon();
        public override int LabelNumber => 1072869; // ornate table (south)


    }
}
