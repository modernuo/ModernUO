using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MinotaurArtifact : Item
{
    [Constructible]
    public MinotaurArtifact() : base(Utility.RandomList(0xB46, 0xB48, 0x9ED))
    {
        if (ItemID == 0x9ED)
        {
            Weight = 30;
        }

        LootType = LootType.Blessed;
        Hue = 0x100;
    }

    public override int LabelNumber => 1074826; // Minotaur Artifact
    public override double DefaultWeight => 5.0;
}
