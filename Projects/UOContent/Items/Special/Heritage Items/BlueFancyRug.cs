namespace Server.Items
{
    public class BlueFancyRugAddon : BaseAddon
    {
        [Constructible]
        public BlueFancyRugAddon()
        {
            AddComponent(new LocalizedAddonComponent(0xAC2, 1076273), 1, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xAC3, 1076273), -1, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xAC4, 1076273), -1, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xAC5, 1076273), 1, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xAF6, 1076273), -1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xAF7, 1076273), 0, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xAF8, 1076273), 1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xAF9, 1076273), 0, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xAFA, 1076273), 0, 0, 0);
        }

        public BlueFancyRugAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new BlueFancyRugDeed();

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

    public class BlueFancyRugDeed : BaseAddonDeed
    {
        [Constructible]
        public BlueFancyRugDeed() => LootType = LootType.Blessed;

        public BlueFancyRugDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new BlueFancyRugAddon();
        public override int LabelNumber => 1076273; // Blue fancy rug

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
