namespace Server.Items
{
    public class TableWithPurpleClothAddon : BaseAddon
    {
        [Constructible]
        public TableWithPurpleClothAddon()
        {
            AddComponent(new LocalizedAddonComponent(0x118B, 1076275), 0, 0, 0);
        }

        public TableWithPurpleClothAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new TableWithPurpleClothDeed();

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

    public class TableWithPurpleClothDeed : BaseAddonDeed
    {
        [Constructible]
        public TableWithPurpleClothDeed() => LootType = LootType.Blessed;

        public TableWithPurpleClothDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new TableWithPurpleClothAddon();
        public override int LabelNumber => 1076275; // Table With A Purple Tablecloth

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
