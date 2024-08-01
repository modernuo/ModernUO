using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0, false )]
public partial class WhitePearlBracelet : GoldBracelet
{
    [Constructible]
    public WhitePearlBracelet()
    {
        Weight = 1.0;

        Attributes.NightSight = 1;

        BaseRunicTool.ApplyAttributesTo( this, true, 0, Utility.RandomMinMax( 3, 5 ), 0, 100 );

        if ( Utility.Random( 100 ) < 50 )
        {
            switch ( Utility.Random( 3 ) )
            {
                case 0:
                    Attributes.CastSpeed += 1;
                    break;
                case 1:
                    Attributes.CastRecovery += 2;
                    break;
                case 2:
                    Attributes.LowerRegCost += 10;
                    break;
            }
        }
    }

    public override int LabelNumber => 1073456; // white pearl bracelet
}
