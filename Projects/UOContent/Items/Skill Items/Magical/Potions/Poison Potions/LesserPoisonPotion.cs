using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LesserPoisonPotion : BasePoisonPotion
{
    [Constructible]
    public LesserPoisonPotion() : base(PotionEffect.PoisonLesser)
    {
    }

    public override Poison Poison => Poison.Lesser;

    public override double MinPoisoningSkill => 0.0;
    public override double MaxPoisoningSkill => 60.0;
}
