namespace Server.Items
{
    [Serializable(0)]
    public partial class ElvenForgeAddon : BaseAddon
    {
        [Constructible]
        public ElvenForgeAddon()
        {
            AddComponent(new AddonComponent(0x2DD8), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new ElvenForgeDeed();
    }

    [Serializable(0)]
    public partial class ElvenForgeDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenForgeDeed()
        {
        }

        public override BaseAddon Addon => new ElvenForgeAddon();
        public override int LabelNumber => 1072875; // squirrel statue (east)
    }
}
