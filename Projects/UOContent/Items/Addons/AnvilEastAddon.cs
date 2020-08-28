namespace Server.Items
{
    public class AnvilEastAddon : BaseAddon
    {
        [Constructible]
        public AnvilEastAddon()
        {
            AddComponent(new AnvilComponent(0xFAF), 0, 0, 0);
        }

        public AnvilEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new AnvilEastDeed();

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

    public class AnvilEastDeed : BaseAddonDeed
    {
        [Constructible]
        public AnvilEastDeed()
        {
        }

        public AnvilEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new AnvilEastAddon();
        public override int LabelNumber => 1044333; // anvil (east)

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
