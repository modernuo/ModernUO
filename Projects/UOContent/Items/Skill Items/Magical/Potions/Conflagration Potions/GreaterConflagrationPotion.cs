using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GreaterConflagrationPotion : BaseConflagrationPotion
{
    [Constructible]
    public GreaterConflagrationPotion() : base(PotionEffect.ConflagrationGreater)
    {
    }

    public override int MinDamage => 4;
    public override int MaxDamage => 8;

    public override int LabelNumber => 1072098; // a Greater Conflagration potion
}
