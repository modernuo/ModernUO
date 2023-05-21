using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LesserExplosionPotion : BaseExplosionPotion
{
    [Constructible]
    public LesserExplosionPotion() : base(PotionEffect.ExplosionLesser)
    {
    }

    public override int MinDamage => 5;
    public override int MaxDamage => 10;
}
