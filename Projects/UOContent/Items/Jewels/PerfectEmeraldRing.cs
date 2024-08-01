using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0, false )]
public partial class PerfectEmeraldRing : GoldRing
{
    [Constructible]
    public PerfectEmeraldRing()
    {
        Weight = 1.0;

        BaseRunicTool.ApplyAttributesTo( this, true, 0, Utility.RandomMinMax( 2, 4 ), 0, 100 );

        if ( Utility.RandomBool() )
        {
            Resistances.Poison += 10;
        }
        else
        {
            Attributes.SpellDamage += 5;
        }
    }

    public override int LabelNumber => 1073459; // perfect emerald ring
}
