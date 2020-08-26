namespace Server.Items
{
    public class ElvenLoveseatEastAddon : BaseAddon
    {
        [Constructible]
        public ElvenLoveseatEastAddon()
        {
            AddComponent(new AddonComponent(0x3089), 0, 0, 0);
            AddComponent(new AddonComponent(0x3088), 1, 0, 0);
        }

        public ElvenLoveseatEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new ElvenLoveseatEastDeed();

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

    public class ElvenLoveseatEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenLoveseatEastDeed()
        {
        }

        public ElvenLoveseatEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new ElvenLoveseatEastAddon();
        public override int LabelNumber => 1073372; // elven loveseat (east)

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
