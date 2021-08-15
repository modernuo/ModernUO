namespace Server.Items
{
    public class WarriorStatueEastAddon : BaseAddon
    {
        [Constructible]
        public WarriorStatueEastAddon()
        {
            AddComponent(new AddonComponent(0x2D12), 0, 0, 0);
        }

        public WarriorStatueEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new WarriorStatueEastDeed();


    }

    public class WarriorStatueEastDeed : BaseAddonDeed
    {
        [Constructible]
        public WarriorStatueEastDeed()
        {
        }

        public WarriorStatueEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new WarriorStatueEastAddon();
        public override int LabelNumber => 1072888; // warrior statue (east)


    }
}
