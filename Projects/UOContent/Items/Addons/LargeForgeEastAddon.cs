namespace Server.Items
{
    public class LargeForgeEastAddon : BaseAddon
    {
        [Constructible]
        public LargeForgeEastAddon()
        {
            AddComponent(new ForgeComponent(0x1986), 0, 0, 0);
            AddComponent(new ForgeComponent(0x198A), 0, 1, 0);
            AddComponent(new ForgeComponent(0x1996), 0, 2, 0);
            AddComponent(new ForgeComponent(0x1992), 0, 3, 0);
        }

        public LargeForgeEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new LargeForgeEastDeed();

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class LargeForgeEastDeed : BaseAddonDeed
    {
        [Constructible]
        public LargeForgeEastDeed()
        {
        }

        public LargeForgeEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new LargeForgeEastAddon();
        public override int LabelNumber => 1044331; // large forge (east)

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
