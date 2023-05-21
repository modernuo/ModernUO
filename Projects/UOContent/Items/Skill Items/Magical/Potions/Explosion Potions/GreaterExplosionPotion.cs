using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GreaterExplosionPotion : BaseExplosionPotion
{
    [Constructible]
    public GreaterExplosionPotion() : base(PotionEffect.ExplosionGreater)
    {
    }

    public override int MinDamage => Core.AOS ? 20 : 15;
    public override int MaxDamage => Core.AOS ? 40 : 30;
}
