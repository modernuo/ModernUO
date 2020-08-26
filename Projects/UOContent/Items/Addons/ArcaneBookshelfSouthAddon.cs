namespace Server.Items
{
    public class ArcaneBookshelfSouthAddon : BaseAddon
    {
        [Constructible]
        public ArcaneBookshelfSouthAddon()
        {
            AddComponent(new AddonComponent(0x3087), 0, 0, 0);
            AddComponent(new AddonComponent(0x3086), 0, 1, 0);
        }

        public ArcaneBookshelfSouthAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new ArcaneBookshelfSouthDeed();

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

    public class ArcaneBookshelfSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public ArcaneBookshelfSouthDeed()
        {
        }

        public ArcaneBookshelfSouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new ArcaneBookshelfSouthAddon();
        public override int LabelNumber => 1072871; // arcane bookshelf (south)

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
