namespace Server.Items
{
    public class ElvenForgeAddon : BaseAddon
    {
        [Constructible]
        public ElvenForgeAddon()
        {
            AddComponent(new AddonComponent(0x2DD8), 0, 0, 0);
        }

        public ElvenForgeAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new ElvenForgeDeed();

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

    public class ElvenForgeDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenForgeDeed()
        {
        }

        public ElvenForgeDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new ElvenForgeAddon();
        public override int LabelNumber => 1072875; // squirrel statue (east)

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
