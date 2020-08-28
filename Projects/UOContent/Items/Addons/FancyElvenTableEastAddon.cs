namespace Server.Items
{
    public class FancyElvenTableEastAddon : BaseAddon
    {
        [Constructible]
        public FancyElvenTableEastAddon()
        {
            AddComponent(new AddonComponent(0x3094), -1, 0, 0);
            AddComponent(new AddonComponent(0x3093), 0, 0, 0);
            AddComponent(new AddonComponent(0x3092), 1, 0, 0);
        }

        public FancyElvenTableEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new FancyElvenTableEastDeed();

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

    public class FancyElvenTableEastDeed : BaseAddonDeed
    {
        [Constructible]
        public FancyElvenTableEastDeed()
        {
        }

        public FancyElvenTableEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new FancyElvenTableEastAddon();
        public override int LabelNumber => 1073386; // hardwood table (east)

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
