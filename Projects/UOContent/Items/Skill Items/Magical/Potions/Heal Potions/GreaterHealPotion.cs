using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GreaterHealPotion : BaseHealPotion
{
    [Constructible]
    public GreaterHealPotion() : base(PotionEffect.HealGreater)
    {
    }

    public override int MinHeal => Core.AOS ? 20 : 9;
    public override int MaxHeal => Core.AOS ? 25 : 30;
    public override double Delay => 10.0;
}
