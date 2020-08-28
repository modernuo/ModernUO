namespace Server.Items
{
    public class AbbatoirAddon : BaseAddon
    {
        [Constructible]
        public AbbatoirAddon()
        {
            AddComponent(new AddonComponent(0x120E), -1, -1, 0);
            AddComponent(new AddonComponent(0x120F), 0, -1, 0);
            AddComponent(new AddonComponent(0x1210), 1, -1, 0);
            AddComponent(new AddonComponent(0x1215), -1, 0, 0);
            AddComponent(new AddonComponent(0x1216), 0, 0, 0);
            AddComponent(new AddonComponent(0x1211), 1, 0, 0);
            AddComponent(new AddonComponent(0x1214), -1, 1, 0);
            AddComponent(new AddonComponent(0x1213), 0, 1, 0);
            AddComponent(new AddonComponent(0x1212), 1, 1, 0);
        }

        public AbbatoirAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new AbbatoirDeed();

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

    public class AbbatoirDeed : BaseAddonDeed
    {
        [Constructible]
        public AbbatoirDeed()
        {
        }

        public AbbatoirDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new AbbatoirAddon();
        public override int LabelNumber => 1044329; // abbatoir

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
