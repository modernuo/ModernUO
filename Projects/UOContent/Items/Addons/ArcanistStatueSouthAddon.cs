namespace Server.Items
{
    public class ArcanistStatueSouthAddon : BaseAddon
    {
        [Constructible]
        public ArcanistStatueSouthAddon()
        {
            AddComponent(new AddonComponent(0x2D0F), 0, 0, 0);
        }

        public ArcanistStatueSouthAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new ArcanistStatueSouthDeed();

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

    public class ArcanistStatueSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public ArcanistStatueSouthDeed()
        {
        }

        public ArcanistStatueSouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new ArcanistStatueSouthAddon();
        public override int LabelNumber => 1072885; // arcanist statue (south)

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
