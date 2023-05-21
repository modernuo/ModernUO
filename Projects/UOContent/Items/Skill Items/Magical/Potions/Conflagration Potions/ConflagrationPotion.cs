using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ConflagrationPotion : BaseConflagrationPotion
{
    [Constructible]
    public ConflagrationPotion() : base(PotionEffect.Conflagration)
    {
    }

    public override int MinDamage => 2;
    public override int MaxDamage => 4;

    public override int LabelNumber => 1072095; // a Conflagration potion
}
