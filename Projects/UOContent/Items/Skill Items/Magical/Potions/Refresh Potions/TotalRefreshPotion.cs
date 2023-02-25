using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TotalRefreshPotion : BaseRefreshPotion
{
    [Constructible]
    public TotalRefreshPotion() : base(PotionEffect.RefreshTotal)
    {
    }

    public override double Refresh => 1.0;
}
