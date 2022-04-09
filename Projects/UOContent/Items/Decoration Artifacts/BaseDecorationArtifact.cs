using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public abstract partial class BaseDecorationArtifact : Item
{
    public BaseDecorationArtifact(int itemID) : base(itemID) => Weight = 10.0;

    public abstract int ArtifactRarity { get; }

    public override bool ForceShowProperties => true;

    public override void GetProperties(ObjectPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1061078, ArtifactRarity.ToString()); // artifact rarity ~1_val~
    }
}

[SerializationGenerator(0)]
public abstract partial class BaseDecorationContainerArtifact : BaseContainer
{
    public BaseDecorationContainerArtifact(int itemID) : base(itemID) => Weight = 10.0;

    public abstract int ArtifactRarity { get; }

    public override bool ForceShowProperties => true;

    public override void AddNameProperties(ObjectPropertyList list)
    {
        base.AddNameProperties(list);

        list.Add(1061078, ArtifactRarity.ToString()); // artifact rarity ~1_val~
    }
}
