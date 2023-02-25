using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HealPotion : BaseHealPotion
{
    [Constructible]
    public HealPotion() : base(PotionEffect.Heal)
    {
    }

    public override int MinHeal => Core.AOS ? 13 : 6;
    public override int MaxHeal => Core.AOS ? 16 : 20;
    public override double Delay => Core.AOS ? 8.0 : 10.0;
}
