namespace Server.Items
{
    public abstract class BaseDecorationArtifact : Item
    {
        public BaseDecorationArtifact(int itemID) : base(itemID) => Weight = 10.0;

        public BaseDecorationArtifact(Serial serial) : base(serial)
        {
        }

        public abstract int ArtifactRarity { get; }

        public override bool ForceShowProperties => true;

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1061078, ArtifactRarity.ToString()); // artifact rarity ~1_val~
        }

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

    public abstract class BaseDecorationContainerArtifact : BaseContainer
    {
        public BaseDecorationContainerArtifact(int itemID) : base(itemID) => Weight = 10.0;

        public BaseDecorationContainerArtifact(Serial serial) : base(serial)
        {
        }

        public abstract int ArtifactRarity { get; }

        public override bool ForceShowProperties => true;

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1061078, ArtifactRarity.ToString()); // artifact rarity ~1_val~
        }

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
