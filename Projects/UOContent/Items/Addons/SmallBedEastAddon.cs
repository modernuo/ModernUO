namespace Server.Items
{
    public class SmallBedEastAddon : BaseAddon
    {
        [Constructible]
        public SmallBedEastAddon()
        {
            AddComponent(new AddonComponent(0xA5D), 0, 0, 0);
            AddComponent(new AddonComponent(0xA62), 1, 0, 0);
        }

        public SmallBedEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new SmallBedEastDeed();

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

    public class SmallBedEastDeed : BaseAddonDeed
    {
        [Constructible]
        public SmallBedEastDeed()
        {
        }

        public SmallBedEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new SmallBedEastAddon();
        public override int LabelNumber => 1044322; // small bed (east)

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
