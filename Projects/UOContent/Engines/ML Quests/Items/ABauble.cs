using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ABauble : Item
{
    [Constructible]
    public ABauble() : base(0x23B) => LootType = LootType.Blessed;

    public override int LabelNumber => 1073137; // A bauble
}
