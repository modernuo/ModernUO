namespace Server.Items
{
    public class ElvenWashBasinEastAddon : BaseAddon
    {
        [Constructible]
        public ElvenWashBasinEastAddon()
        {
            AddComponent(new AddonComponent(0x30DF), 0, 0, 0);
            AddComponent(new AddonComponent(0x30E0), 0, 1, 0);
        }

        public ElvenWashBasinEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new ElvenWashBasinEastDeed();

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

    public class ElvenWashBasinEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenWashBasinEastDeed()
        {
        }

        public ElvenWashBasinEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new ElvenWashBasinEastAddon();
        public override int LabelNumber => 1073387; // elven wash basin (east)

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
