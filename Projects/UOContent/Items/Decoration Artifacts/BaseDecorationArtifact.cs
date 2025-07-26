using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public abstract partial class BaseDecorationArtifact : Item
{
    public BaseDecorationArtifact(int itemID) : base(itemID)
    {
    }

    public override double DefaultWeight => 10.0;

    public abstract int ArtifactRarity { get; }

    public override bool ForceShowProperties => true;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1061078, ArtifactRarity); // artifact rarity ~1_val~
    }
}

[SerializationGenerator(0)]
public abstract partial class BaseDecorationContainerArtifact : BaseContainer
{
    public BaseDecorationContainerArtifact(int itemID) : base(itemID)
    {
    }

    public override double DefaultWeight => 10.0;

    public abstract int ArtifactRarity { get; }

    public override bool ForceShowProperties => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);

        list.Add(1061078, ArtifactRarity); // artifact rarity ~1_val~
    }
}
