using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0 )]
public partial class GrapesOfWrath : BaseMagicalFood
{
    //TODO
    //TextDefinition ICommodity.Description => LabelNumber;
    //bool ICommodity.IsDeedable => true;

    [Constructible]
    public GrapesOfWrath()
        : base( 0x2FD7 )
    {
        Weight = 1.0;
        Hue = 0x482;
        Stackable = true;
    }

    public override MagicalFood FoodID => MagicalFood.GrapesOfWrath;
    public override TimeSpan Cooldown => TimeSpan.FromMinutes( 2 );
    public override TimeSpan Duration => TimeSpan.FromSeconds( 20 );

    public override int EatMessage =>
        1074847; // The grapes of wrath invigorate you for a short time, allowing you to deal extra damage.

    public override bool Eat( Mobile from )
    {
        if ( base.Eat( from ) )
        {
            BuffInfo.AddBuff( from, new BuffInfo( BuffIcon.GrapesOfWrath, 1032247, 1153762, Duration, from, "15\t35" ) );
            return true;
        }

        return false;
    }
}
