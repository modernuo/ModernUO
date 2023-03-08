using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class FountainAddon : StoneFountainAddon
{
    [Constructible]
    public FountainAddon()
    {
    }

    public override BaseAddonDeed Deed => new FountainDeed();
}

[SerializationGenerator(0)]
public partial class FountainDeed : BaseAddonDeed
{
    [Constructible]
    public FountainDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new FountainAddon();
    public override int LabelNumber => 1076283; // Fountain
}
