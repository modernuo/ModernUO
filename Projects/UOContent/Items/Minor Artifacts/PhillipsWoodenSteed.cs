using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PhillipsWoodenSteed : MonsterStatuette
{
    [Constructible]
    public PhillipsWoodenSteed() : base(MonsterStatuetteType.PhillipsWoodenSteed) => LootType = LootType.Regular;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}
