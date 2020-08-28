namespace Server.Items
{
    public class ParrotPerchAddon : BaseAddon
    {
        [Constructible]
        public ParrotPerchAddon()
        {
            AddComponent(new AddonComponent(0x2FF4), 0, 0, 0);
        }

        public ParrotPerchAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new ParrotPerchDeed();

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

    public class ParrotPerchDeed : BaseAddonDeed
    {
        [Constructible]
        public ParrotPerchDeed()
        {
        }

        public ParrotPerchDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new ParrotPerchAddon();
        public override int LabelNumber => 1072617; // parrot perch

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
