using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DeadlyPoisonPotion : BasePoisonPotion
{
    [Constructible]
    public DeadlyPoisonPotion() : base(PotionEffect.PoisonDeadly)
    {
    }

    public override Poison Poison => Poison.Deadly;

    public override double MinPoisoningSkill => 95.0;
    public override double MaxPoisoningSkill => 100.0;
}
