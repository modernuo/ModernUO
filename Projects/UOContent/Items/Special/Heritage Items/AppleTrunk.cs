namespace Server.Items
{
    public class AppleTrunkAddon : BaseAddon
    {
        [Constructible]
        public AppleTrunkAddon()
        {
            AddComponent(new LocalizedAddonComponent(0xD98, 1076785), 0, 0, 0);
        }

        public AppleTrunkAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new AppleTrunkDeed();

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

    public class AppleTrunkDeed : BaseAddonDeed
    {
        [Constructible]
        public AppleTrunkDeed() => LootType = LootType.Blessed;

        public AppleTrunkDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new AppleTrunkAddon();
        public override int LabelNumber => 1076785; // Apple Trunk

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
