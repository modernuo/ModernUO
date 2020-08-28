namespace Server.Items
{
    public class ArcaneBookshelfEastAddon : BaseAddon
    {
        [Constructible]
        public ArcaneBookshelfEastAddon()
        {
            AddComponent(new AddonComponent(0x3084), 0, 0, 0);
            AddComponent(new AddonComponent(0x3085), -1, 0, 0);
        }

        public ArcaneBookshelfEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new ArcaneBookshelfEastDeed();

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

    public class ArcaneBookshelfEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ArcaneBookshelfEastDeed()
        {
        }

        public ArcaneBookshelfEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new ArcaneBookshelfEastAddon();
        public override int LabelNumber => 1073371; // arcane bookshelf (east)

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
