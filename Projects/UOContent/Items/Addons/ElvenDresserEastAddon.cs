namespace Server.Items
{
    public class ElvenDresserEastAddon : BaseAddon
    {
        [Constructible]
        public ElvenDresserEastAddon()
        {
            AddComponent(new AddonComponent(0x30E4), 0, 0, 0);
            AddComponent(new AddonComponent(0x30E3), 0, -1, 0);
        }

        public ElvenDresserEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new ElvenDresserEastDeed();

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class ElvenDresserEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenDresserEastDeed()
        {
        }

        public ElvenDresserEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new ElvenDresserEastAddon();
        public override int LabelNumber => 1073388; // elven dresser (east)

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
