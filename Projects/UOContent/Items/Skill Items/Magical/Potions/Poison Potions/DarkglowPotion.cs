using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DarkglowPotion : BasePoisonPotion
{
    [Constructible]
    public DarkglowPotion() : base(PotionEffect.Darkglow) => Hue = 0x96;

    public override Poison Poison => Poison.GreaterDarkglow;

    public override double MinPoisoningSkill => 95.0;
    public override double MaxPoisoningSkill => 100.0;

    public override int LabelNumber => 1072849; // Darkglow Poison
}
