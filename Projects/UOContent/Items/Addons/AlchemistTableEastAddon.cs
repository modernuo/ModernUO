namespace Server.Items
{
    public class AlchemistTableEastAddon : BaseAddon
    {
        [Constructible]
        public AlchemistTableEastAddon()
        {
            AddComponent(new AddonComponent(0x2DD3), 0, 0, 0);
        }

        public AlchemistTableEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new AlchemistTableEastDeed();

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class AlchemistTableEastDeed : BaseAddonDeed
    {
        [Constructible]
        public AlchemistTableEastDeed()
        {
        }

        public AlchemistTableEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new AlchemistTableEastAddon();
        public override int LabelNumber => 1073397; // alchemist table (east)

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
