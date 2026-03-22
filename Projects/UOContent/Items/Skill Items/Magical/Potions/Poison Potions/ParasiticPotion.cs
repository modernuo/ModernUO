using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ParasiticPotion : BasePoisonPotion
{
    [Constructible]
    public ParasiticPotion() : base(PotionEffect.Parasitic) => Hue = 0x17C;

    public override Poison Poison => Poison.DeadlyParasitic;

    public override double MinPoisoningSkill => 95.0;
    public override double MaxPoisoningSkill => 100.0;

    public override int LabelNumber => 1072848; // Parasitic Poison
}
