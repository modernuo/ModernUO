namespace Server.Items
{
    public class SandstoneFireplaceSouthAddon : BaseAddon
    {
        [Constructible]
        public SandstoneFireplaceSouthAddon()
        {
            AddComponent(new AddonComponent(0x482), -1, 0, 0);
            AddComponent(new AddonComponent(0x47B), 0, 0, 0);
        }

        public SandstoneFireplaceSouthAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new SandstoneFireplaceSouthDeed();

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class SandstoneFireplaceSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public SandstoneFireplaceSouthDeed()
        {
        }

        public SandstoneFireplaceSouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new SandstoneFireplaceSouthAddon();
        public override int LabelNumber => 1061845; // sandstone fireplace (south)

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
