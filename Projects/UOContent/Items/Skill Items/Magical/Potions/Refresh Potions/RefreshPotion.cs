using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RefreshPotion : BaseRefreshPotion
{
    [Constructible]
    public RefreshPotion() : base(PotionEffect.Refresh)
    {
    }

    public override double Refresh => 0.25;
}
