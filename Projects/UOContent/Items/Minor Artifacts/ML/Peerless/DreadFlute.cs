using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0, false )]
[Flippable( 0x315C, 0x315D )]
public partial class DreadFlute : BaseInstrument
{
    [Constructible]
    public DreadFlute()
        : base( 0x315C, 0x58B, 0x58C ) // TODO check sounds
    {
        Weight = 1.0;
        ReplenishesCharges = true;
        Hue = 0x4F2;
    }

    public override int LabelNumber => 1075089; // Dread Flute
    public override int InitMinUses => 700;
    public override int InitMaxUses => 700;
    public override TimeSpan ChargeReplenishRate => TimeSpan.FromMinutes( 15.0 );
}
