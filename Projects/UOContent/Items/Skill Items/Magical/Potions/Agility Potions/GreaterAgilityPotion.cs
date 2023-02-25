using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GreaterAgilityPotion : BaseAgilityPotion
{
    [Constructible]
    public GreaterAgilityPotion() : base(PotionEffect.AgilityGreater)
    {
    }

    public override int DexOffset => 20;
    public override TimeSpan Duration => TimeSpan.FromMinutes(2.0);
}
