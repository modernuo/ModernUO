namespace Server.Items
{
    public class WarriorStatueSouthAddon : BaseAddon
    {
        [Constructible]
        public WarriorStatueSouthAddon()
        {
            AddComponent(new AddonComponent(0x2D13), 0, 0, 0);
        }

        public WarriorStatueSouthAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new WarriorStatueSouthDeed();


    }

    public class WarriorStatueSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public WarriorStatueSouthDeed()
        {
        }

        public WarriorStatueSouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new WarriorStatueSouthAddon();
        public override int LabelNumber => 1072887; // warrior statue (south)


    }
}
