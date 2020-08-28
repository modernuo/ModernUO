namespace Server.Items
{
    public class BlueDecorativeRugAddon : BaseAddon
    {
        [Constructible]
        public BlueDecorativeRugAddon()
        {
            AddComponent(new LocalizedAddonComponent(0xAD2, 1076589), 1, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xAD3, 1076589), -1, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xAD4, 1076589), -1, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xAD5, 1076589), 1, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xAD6, 1076589), -1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xAD7, 1076589), 0, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xAD8, 1076589), 1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xAD9, 1076589), 0, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xAD1, 1076589), 0, 0, 0);
        }

        public BlueDecorativeRugAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new BlueDecorativeRugDeed();

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

    public class BlueDecorativeRugDeed : BaseAddonDeed
    {
        [Constructible]
        public BlueDecorativeRugDeed() => LootType = LootType.Blessed;

        public BlueDecorativeRugDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new BlueDecorativeRugAddon();
        public override int LabelNumber => 1076589; // Blue decorative rug

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
