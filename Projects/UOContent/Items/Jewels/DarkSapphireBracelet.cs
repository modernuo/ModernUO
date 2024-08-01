using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0, false )]
public partial class DarkSapphireBracelet : GoldBracelet
{
    [Constructible]
    public DarkSapphireBracelet()
    {
        Weight = 1.0;

        BaseRunicTool.ApplyAttributesTo( this, true, 0, Utility.RandomMinMax( 1, 4 ), 0, 100 );

        if ( Utility.Random( 100 ) < 10 )
        {
            Attributes.RegenMana += 2;
        }
        else
        {
            Resistances.Cold += 10;
        }
    }

    public override int LabelNumber => 1073455; // dark sapphire bracelet
}
