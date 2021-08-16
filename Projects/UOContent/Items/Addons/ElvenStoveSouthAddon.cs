namespace Server.Items
{
    [Serializable(0)]
    public partial class ElvenStoveSouthAddon : BaseAddon
    {
        [Constructible]
        public ElvenStoveSouthAddon()
        {
            AddComponent(new AddonComponent(0x2DDC), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new ElvenStoveSouthDeed();
    }

    public class ElvenStoveSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenStoveSouthDeed()
        {
        }

        public ElvenStoveSouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new ElvenStoveSouthAddon();
        public override int LabelNumber => 1073394; // elven oven (south)
    }
}
