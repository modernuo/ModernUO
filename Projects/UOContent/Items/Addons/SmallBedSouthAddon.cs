namespace Server.Items
{
    public class SmallBedSouthAddon : BaseAddon
    {
        [Constructible]
        public SmallBedSouthAddon()
        {
            AddComponent(new AddonComponent(0xA63), 0, 0, 0);
            AddComponent(new AddonComponent(0xA5C), 0, 1, 0);
        }

        public SmallBedSouthAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new SmallBedSouthDeed();

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

    public class SmallBedSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public SmallBedSouthDeed()
        {
        }

        public SmallBedSouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new SmallBedSouthAddon();
        public override int LabelNumber => 1044321; // small bed (south)

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
