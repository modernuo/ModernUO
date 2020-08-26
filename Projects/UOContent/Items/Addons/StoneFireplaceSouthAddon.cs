namespace Server.Items
{
    public class StoneFireplaceSouthAddon : BaseAddon
    {
        [Constructible]
        public StoneFireplaceSouthAddon()
        {
            AddComponent(new AddonComponent(0x967), -1, 0, 0);
            AddComponent(new AddonComponent(0x961), 0, 0, 0);
        }

        public StoneFireplaceSouthAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new StoneFireplaceSouthDeed();

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

    public class StoneFireplaceSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public StoneFireplaceSouthDeed()
        {
        }

        public StoneFireplaceSouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new StoneFireplaceSouthAddon();
        public override int LabelNumber => 1061849; // stone fireplace (south)

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
