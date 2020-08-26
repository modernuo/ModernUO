namespace Server.Items
{
    public class SquirrelStatueSouthAddon : BaseAddon
    {
        [Constructible]
        public SquirrelStatueSouthAddon()
        {
            AddComponent(new AddonComponent(0x2D11), 0, 0, 0);
        }

        public SquirrelStatueSouthAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new SquirrelStatueSouthDeed();

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

    public class SquirrelStatueSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public SquirrelStatueSouthDeed()
        {
        }

        public SquirrelStatueSouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new SquirrelStatueSouthAddon();
        public override int LabelNumber => 1072884; // squirrel statue (south)

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
