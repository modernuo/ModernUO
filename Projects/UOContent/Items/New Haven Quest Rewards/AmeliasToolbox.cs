using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class AmeliasToolbox : TinkerTools
{
    [Constructible]
    public AmeliasToolbox() : base(500)
    {
        LootType = LootType.Blessed;
        Hue = 1895; // TODO check
    }

    public override int LabelNumber => 1077749; // Amelias Toolbox
}
