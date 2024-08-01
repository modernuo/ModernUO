using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0, false )]
public partial class TurqouiseRing : GoldRing
{
    [Constructible]
    public TurqouiseRing()
    {
        Weight = 1.0;

        BaseRunicTool.ApplyAttributesTo( this, true, 0, Utility.RandomMinMax( 1, 3 ), 0, 100 );

        if ( Utility.Random( 100 ) < 10 )
        {
            Attributes.WeaponSpeed += 5;
        }
        else
        {
            Attributes.WeaponDamage += 15;
        }
    }

    public TurqouiseRing( Serial serial )
        : base( serial )
    {
    }

    public override int LabelNumber => 1073460; // turquoise ring
}
