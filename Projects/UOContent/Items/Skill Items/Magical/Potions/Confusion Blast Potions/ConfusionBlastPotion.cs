using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ConfusionBlastPotion : BaseConfusionBlastPotion
{
    [Constructible]
    public ConfusionBlastPotion() : base(PotionEffect.ConfusionBlast)
    {
    }

    public override int Radius => 5;

    public override int LabelNumber => 1072105; // a Confusion Blast potion
}
