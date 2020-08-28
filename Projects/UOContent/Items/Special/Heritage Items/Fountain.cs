namespace Server.Items
{
    public class FountainAddon : StoneFountainAddon
    {
        [Constructible]
        public FountainAddon()
        {
        }

        public FountainAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new FountainDeed();

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

    public class FountainDeed : BaseAddonDeed
    {
        [Constructible]
        public FountainDeed() => LootType = LootType.Blessed;

        public FountainDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new FountainAddon();
        public override int LabelNumber => 1076283; // Fountain

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
