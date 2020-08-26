namespace Server.Items
{
    [Flippable(0x1EA3, 0x1EA4)]
    public class SmallFishingNetComponent : AddonComponent
    {
        public SmallFishingNetComponent() : base(0x1EA3)
        {
        }

        public SmallFishingNetComponent(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1076286; // Small Fish Net

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

    public class SmallFishingNetAddon : BaseAddon
    {
        [Constructible]
        public SmallFishingNetAddon()
        {
            AddComponent(new SmallFishingNetComponent(), 0, 0, 0);
        }

        public SmallFishingNetAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new SmallFishingNetDeed();

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

    public class SmallFishingNetDeed : BaseAddonDeed
    {
        [Constructible]
        public SmallFishingNetDeed() => LootType = LootType.Blessed;

        public SmallFishingNetDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new SmallFishingNetAddon();
        public override int LabelNumber => 1076286; // Small Fish Net

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
