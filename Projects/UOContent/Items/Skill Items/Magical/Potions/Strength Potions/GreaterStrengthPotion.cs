using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GreaterStrengthPotion : BaseStrengthPotion
{
    [Constructible]
    public GreaterStrengthPotion() : base(PotionEffect.StrengthGreater)
    {
    }

    public override int StrOffset => 20;
    public override TimeSpan Duration => TimeSpan.FromMinutes(2.0);
}
