namespace Server.Items
{
    public class LoomSouthAddon : BaseAddon, ILoom
    {
        [Constructible]
        public LoomSouthAddon()
        {
            AddComponent(new AddonComponent(0x1061), 0, 0, 0);
            AddComponent(new AddonComponent(0x1062), 1, 0, 0);
        }

        public LoomSouthAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new LoomSouthDeed();

        public int Phase { get; set; }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(Phase);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        Phase = reader.ReadInt();
                        break;
                    }
            }
        }
    }

    public class LoomSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public LoomSouthDeed()
        {
        }

        public LoomSouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new LoomSouthAddon();
        public override int LabelNumber => 1044344; // loom (south)

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
