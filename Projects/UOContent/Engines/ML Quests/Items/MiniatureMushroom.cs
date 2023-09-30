using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MiniatureMushroom : Food
{
    [Constructible]
    public MiniatureMushroom() : base(0xD16) => LootType = LootType.Blessed;

    public override int LabelNumber => 1073138; // Miniature mushroom
}
