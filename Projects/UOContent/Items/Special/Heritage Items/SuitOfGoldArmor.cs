namespace Server.Items
{
    [Flippable(0x3DAA, 0x3DA9)]
    public class SuitOfGoldArmorComponent : AddonComponent
    {
        public SuitOfGoldArmorComponent() : base(0x3DAA)
        {
        }

        public SuitOfGoldArmorComponent(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1076265; // Suit of Gold Armor

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

    public class SuitOfGoldArmorAddon : BaseAddon
    {
        [Constructible]
        public SuitOfGoldArmorAddon()
        {
            AddComponent(new SuitOfGoldArmorComponent(), 0, 0, 0);
        }

        public SuitOfGoldArmorAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new SuitOfGoldArmorDeed();

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

    public class SuitOfGoldArmorDeed : BaseAddonDeed
    {
        [Constructible]
        public SuitOfGoldArmorDeed() => LootType = LootType.Blessed;

        public SuitOfGoldArmorDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new SuitOfGoldArmorAddon();
        public override int LabelNumber => 1076265; // Suit of Gold Armor

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
