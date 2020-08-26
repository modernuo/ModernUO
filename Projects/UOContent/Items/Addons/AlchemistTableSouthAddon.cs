namespace Server.Items
{
    public class AlchemistTableSouthAddon : BaseAddon
    {
        [Constructible]
        public AlchemistTableSouthAddon()
        {
            AddComponent(new AddonComponent(0x2DD4), 0, 0, 0);
        }

        public AlchemistTableSouthAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new AlchemistTableSouthDeed();

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

    public class AlchemistTableSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public AlchemistTableSouthDeed()
        {
        }

        public AlchemistTableSouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new AlchemistTableSouthAddon();
        public override int LabelNumber => 1073396; // alchemist table (south)

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
