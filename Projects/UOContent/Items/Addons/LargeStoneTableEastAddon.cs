namespace Server.Items
{
    public class LargeStoneTableEastAddon : BaseAddon
    {
        [Constructible]
        public LargeStoneTableEastAddon(int hue = 0)
        {
            AddComponent(new AddonComponent(0x1202), 0, 0, 0);
            AddComponent(new AddonComponent(0x1203), 0, 1, 0);
            AddComponent(new AddonComponent(0x1201), 0, 2, 0);
            Hue = hue;
        }

        public LargeStoneTableEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new LargeStoneTableEastDeed();

        public override bool RetainDeedHue => true;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class LargeStoneTableEastDeed : BaseAddonDeed
    {
        [Constructible]
        public LargeStoneTableEastDeed()
        {
        }

        public LargeStoneTableEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new LargeStoneTableEastAddon(Hue);
        public override int LabelNumber => 1044511; // large stone table (east)

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
