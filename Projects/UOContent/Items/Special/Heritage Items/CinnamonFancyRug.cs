namespace Server.Items
{
    public class CinnamonFancyRugAddon : BaseAddon
    {
        [Constructible]
        public CinnamonFancyRugAddon()
        {
            AddComponent(new LocalizedAddonComponent(0xAE3, 1076587), 1, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xAE4, 1076587), -1, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xAE5, 1076587), -1, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xAE6, 1076587), 1, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xAE7, 1076587), -1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xAE8, 1076587), 0, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xAE9, 1076587), 1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xAEA, 1076587), 0, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xAEB, 1076587), 0, 0, 0);
        }

        public CinnamonFancyRugAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new CinnamonFancyRugDeed();

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

    public class CinnamonFancyRugDeed : BaseAddonDeed
    {
        [Constructible]
        public CinnamonFancyRugDeed() => LootType = LootType.Blessed;

        public CinnamonFancyRugDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new CinnamonFancyRugAddon();
        public override int LabelNumber => 1076587; // Cinnamon fancy rug

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
