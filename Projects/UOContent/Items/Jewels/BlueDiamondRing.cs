using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0, false )]
public partial class BlueDiamondRing : GoldRing
{
    [Constructible]
    public BlueDiamondRing()
    {
        Weight = 1.0;

        BaseRunicTool.ApplyAttributesTo( this, true, 0, Utility.RandomMinMax( 2, 4 ), 0, 100 );

        switch ( Utility.Random( 4 ) )
        {
            case 0:
                Attributes.LowerManaCost += 10;
                break;
            case 1:
                Attributes.CastSpeed += 1;
                break;
            case 2:
                Attributes.CastRecovery += 2;
                break;
            case 3:
                Attributes.SpellDamage += 5;
                break;
        }
    }

    public override int LabelNumber => 1073458; // blue diamond ring
}
