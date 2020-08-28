namespace Server.Items
{
    public class OrnateElvenTableEastAddon : BaseAddon
    {
        [Constructible]
        public OrnateElvenTableEastAddon()
        {
            AddComponent(new AddonComponent(0x308E), -1, 0, 0);
            AddComponent(new AddonComponent(0x308D), 0, 0, 0);
            AddComponent(new AddonComponent(0x308C), 1, 0, 0);
        }

        public OrnateElvenTableEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new OrnateElvenTableEastDeed();

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

    public class OrnateElvenTableEastDeed : BaseAddonDeed
    {
        [Constructible]
        public OrnateElvenTableEastDeed()
        {
        }

        public OrnateElvenTableEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new OrnateElvenTableEastAddon();
        public override int LabelNumber => 1073384; // ornate table (east)

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
