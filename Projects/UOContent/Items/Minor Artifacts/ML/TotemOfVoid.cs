using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class TotemOfVoid : BaseTalisman
{
    [Constructible]
    public TotemOfVoid() : base(0x2F5B)
    {
        Hue = 0x2D0;
        MaxChargeTime = 1800;

        Blessed = GetRandomBlessed();
        Protection = GetRandomProtection(false);

        Attributes.RegenHits = 2;
        Attributes.LowerManaCost = 10;
    }

    public override int LabelNumber => 1075035; // Totem of the Void
    public override bool ForceShowName => true;

    public override Type GetSummoner() => Utility.RandomBool() ? typeof(SummonedSkeletalKnight) : typeof(SummonedSheep);
}
