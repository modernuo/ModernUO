namespace Server.Items
{
    public class LargeStoneTableSouthAddon : BaseAddon
    {
        [Constructible]
        public LargeStoneTableSouthAddon(int hue = 0)
        {
            AddComponent(new AddonComponent(0x1205), 0, 0, 0);
            AddComponent(new AddonComponent(0x1206), 1, 0, 0);
            AddComponent(new AddonComponent(0x1204), 2, 0, 0);
            Hue = hue;
        }

        public LargeStoneTableSouthAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new LargeStoneTableSouthDeed();

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

    public class LargeStoneTableSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public LargeStoneTableSouthDeed()
        {
        }

        public LargeStoneTableSouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new LargeStoneTableSouthAddon(Hue);
        public override int LabelNumber => 1044512; // large stone table (South)

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
