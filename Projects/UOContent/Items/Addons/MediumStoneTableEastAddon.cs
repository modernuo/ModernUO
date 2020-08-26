namespace Server.Items
{
    public class MediumStoneTableEastAddon : BaseAddon
    {
        [Constructible]
        public MediumStoneTableEastAddon(int hue = 0)
        {
            AddComponent(new AddonComponent(0x1202), 0, 0, 0);
            AddComponent(new AddonComponent(0x1201), 0, 1, 0);
            Hue = hue;
        }

        public MediumStoneTableEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new MediumStoneTableEastDeed();

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

    public class MediumStoneTableEastDeed : BaseAddonDeed
    {
        [Constructible]
        public MediumStoneTableEastDeed()
        {
        }

        public MediumStoneTableEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new MediumStoneTableEastAddon(Hue);
        public override int LabelNumber => 1044508; // stone table (east)

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
