using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GreaterPoisonPotion : BasePoisonPotion
{
    [Constructible]
    public GreaterPoisonPotion() : base(PotionEffect.PoisonGreater)
    {
    }

    public override Poison Poison => Poison.Greater;

    public override double MinPoisoningSkill => 60.0;
    public override double MaxPoisoningSkill => 100.0;
}
