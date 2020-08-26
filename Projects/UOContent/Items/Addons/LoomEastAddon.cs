namespace Server.Items
{
    public interface ILoom
    {
        int Phase { get; set; }
    }

    public class LoomEastAddon : BaseAddon, ILoom
    {
        [Constructible]
        public LoomEastAddon()
        {
            AddComponent(new AddonComponent(0x1060), 0, 0, 0);
            AddComponent(new AddonComponent(0x105F), 0, 1, 0);
        }

        public LoomEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new LoomEastDeed();

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

    public class LoomEastDeed : BaseAddonDeed
    {
        [Constructible]
        public LoomEastDeed()
        {
        }

        public LoomEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new LoomEastAddon();
        public override int LabelNumber => 1044343; // loom (east)

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
