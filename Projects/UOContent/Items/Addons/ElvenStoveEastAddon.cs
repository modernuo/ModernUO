namespace Server.Items
{
    public class ElvenStoveEastAddon : BaseAddon
    {
        [Constructible]
        public ElvenStoveEastAddon()
        {
            AddComponent(new AddonComponent(0x2DDB), 0, 0, 0);
        }

        public ElvenStoveEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new ElvenStoveEastDeed();

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

    public class ElvenStoveEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenStoveEastDeed()
        {
        }

        public ElvenStoveEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new ElvenStoveEastAddon();
        public override int LabelNumber => 1073395; // elven oven (east)

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
