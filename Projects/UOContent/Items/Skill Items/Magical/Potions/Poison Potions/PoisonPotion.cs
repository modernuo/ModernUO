using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PoisonPotion : BasePoisonPotion
{
    [Constructible]
    public PoisonPotion() : base(PotionEffect.Poison)
    {
    }

    public override Poison Poison => Poison.Regular;

    public override double MinPoisoningSkill => 30.0;
    public override double MaxPoisoningSkill => 70.0;
}
