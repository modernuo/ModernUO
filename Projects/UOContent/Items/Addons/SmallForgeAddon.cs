namespace Server.Items
{
    [Serializable(0, false)]
    public partial class SmallForgeAddon : BaseAddon
    {
        [Constructible]
        public SmallForgeAddon()
        {
            AddComponent(new ForgeComponent(0xFB1), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new SmallForgeDeed();
    }

    [Serializable(0, false)]
    public partial class SmallForgeDeed : BaseAddonDeed
    {
        [Constructible]
        public SmallForgeDeed()
        {
        }

        public override BaseAddon Addon => new SmallForgeAddon();
        public override int LabelNumber => 1044330; // small forge
    }
}
