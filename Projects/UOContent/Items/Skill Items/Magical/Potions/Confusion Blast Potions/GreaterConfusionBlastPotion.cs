using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GreaterConfusionBlastPotion : BaseConfusionBlastPotion
{
    [Constructible]
    public GreaterConfusionBlastPotion() : base(PotionEffect.ConfusionBlastGreater)
    {
    }

    public override int Radius => 7;

    public override int LabelNumber => 1072108; // a Greater Confusion Blast potion
}
