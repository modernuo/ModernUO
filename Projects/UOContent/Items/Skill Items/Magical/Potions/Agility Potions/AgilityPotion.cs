using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AgilityPotion : BaseAgilityPotion
{
    [Constructible]
    public AgilityPotion() : base(PotionEffect.Agility)
    {
    }

    public override int DexOffset => 10;
    public override TimeSpan Duration => TimeSpan.FromMinutes(2.0);
}
