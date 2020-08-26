namespace Server.Items
{
    public class ElvenStoveSouthAddon : BaseAddon
    {
        [Constructible]
        public ElvenStoveSouthAddon()
        {
            AddComponent(new AddonComponent(0x2DDC), 0, 0, 0);
        }

        public ElvenStoveSouthAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new ElvenStoveSouthDeed();

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

    public class ElvenStoveSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenStoveSouthDeed()
        {
        }

        public ElvenStoveSouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new ElvenStoveSouthAddon();
        public override int LabelNumber => 1073394; // elven oven (south)

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
