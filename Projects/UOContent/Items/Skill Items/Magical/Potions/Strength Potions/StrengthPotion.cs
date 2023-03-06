using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StrengthPotion : BaseStrengthPotion
{
    [Constructible]
    public StrengthPotion() : base(PotionEffect.Strength)
    {
    }

    public override int StrOffset => 10;
    public override TimeSpan Duration => TimeSpan.FromMinutes(2.0);
}
