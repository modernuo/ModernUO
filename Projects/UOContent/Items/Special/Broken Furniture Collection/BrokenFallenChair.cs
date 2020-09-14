namespace Server.Items
{
    [Flippable(0xC19, 0xC1A)]
    public class BrokenFallenChairComponent : AddonComponent
    {
        public BrokenFallenChairComponent() : base(0xC19)
        {
        }

        public BrokenFallenChairComponent(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1076264; // Broken Fallen Chair

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            if (version < 1 && ItemID == 0xC17)
            {
                ItemID = 0xC19;
            }
        }
    }

    public class BrokenFallenChairAddon : BaseAddon
    {
        [Constructible]
        public BrokenFallenChairAddon()
        {
            AddComponent(new BrokenFallenChairComponent(), 0, 0, 0);
        }

        public BrokenFallenChairAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new BrokenFallenChairDeed();

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

    public class BrokenFallenChairDeed : BaseAddonDeed
    {
        [Constructible]
        public BrokenFallenChairDeed() => LootType = LootType.Blessed;

        public BrokenFallenChairDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new BrokenFallenChairAddon();
        public override int LabelNumber => 1076264; // Broken Fallen Chair

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
