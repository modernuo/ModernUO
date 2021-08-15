namespace Server.Items
{
    public class SquirrelStatueEastAddon : BaseAddon
    {
        [Constructible]
        public SquirrelStatueEastAddon()
        {
            AddComponent(new AddonComponent(0x2D10), 0, 0, 0);
        }

        public SquirrelStatueEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new SquirrelStatueEastDeed();


    }

    public class SquirrelStatueEastDeed : BaseAddonDeed
    {
        [Constructible]
        public SquirrelStatueEastDeed()
        {
        }

        public SquirrelStatueEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new SquirrelStatueEastAddon();
        public override int LabelNumber => 1073398; // squirrel statue (east)


    }
}
