namespace Server.Items
{
    public class ElvenBedEastAddon : BaseAddon
    {
        [Constructible]
        public ElvenBedEastAddon()
        {
            AddComponent(new AddonComponent(0x304D), 0, 0, 0);
            AddComponent(new AddonComponent(0x304C), 1, 0, 0);
        }

        public ElvenBedEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new ElvenBedEastDeed();

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

    public class ElvenBedEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenBedEastDeed()
        {
        }

        public ElvenBedEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new ElvenBedEastAddon();
        public override int LabelNumber => 1072861; // elven bed (east)

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
