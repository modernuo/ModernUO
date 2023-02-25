using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LesserHealPotion : BaseHealPotion
{
    [Constructible]
    public LesserHealPotion() : base(PotionEffect.HealLesser)
    {
    }

    public override int MinHeal => Core.AOS ? 6 : 3;
    public override int MaxHeal => Core.AOS ? 8 : 10;
    public override double Delay => Core.AOS ? 3.0 : 10.0;
}
