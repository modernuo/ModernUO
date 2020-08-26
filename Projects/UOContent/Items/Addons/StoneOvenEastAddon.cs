namespace Server.Items
{
    public class StoneOvenEastAddon : BaseAddon
    {
        [Constructible]
        public StoneOvenEastAddon()
        {
            AddComponent(new AddonComponent(0x92C), 0, 0, 0);
            AddComponent(new AddonComponent(0x92B), 0, 1, 0);
        }

        public StoneOvenEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new StoneOvenEastDeed();

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

    public class StoneOvenEastDeed : BaseAddonDeed
    {
        [Constructible]
        public StoneOvenEastDeed()
        {
        }

        public StoneOvenEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new StoneOvenEastAddon();
        public override int LabelNumber => 1044345; // stone oven (east)

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
