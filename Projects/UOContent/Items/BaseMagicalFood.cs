using System;
using System.Collections;
using ModernUO.Serialization;

namespace Server.Items;

public enum MagicalFood
{
    None = 0x0,
    GrapesOfWrath = 0x1,
    EnchantedApple = 0x2
}

[SerializationGenerator( 0 )]
public partial class BaseMagicalFood : Food
{
    private static Hashtable _table;
    private static Hashtable _cooldown;

    [Constructible]
    public BaseMagicalFood( int itemID )
        : base( itemID )
    {
        Weight = 1.0;
        FillFactor = 0;
        Stackable = false;
    }

    public virtual MagicalFood FoodID => MagicalFood.None;
    public virtual TimeSpan Cooldown => TimeSpan.Zero;
    public virtual TimeSpan Duration => TimeSpan.Zero;
    public virtual int EatMessage => 0;

    public static bool IsUnderInfluence( Mobile mob, MagicalFood id ) => _table?[mob] != null && ( ( int )_table[mob] & ( int )id ) > 0;

    public static bool CoolingDown( Mobile mob, MagicalFood id ) => _cooldown?[mob] != null && ( ( int )_cooldown[mob] & ( int )id ) > 0;

    public static void StartInfluence( Mobile mob, MagicalFood id, TimeSpan duration, TimeSpan cooldown )
    {
        _table ??= new Hashtable();

        _table[mob] ??= 0;

        _table[mob] = ( int )_table[mob] | ( int )id;

        Timer.DelayCall( duration, EndInfluence, new object[] { mob, id, cooldown } );
    }

    public static void EndInfluence( object obj )
    {
        if ( obj is object[] { Length: 3 } args )
        {
            if ( args[0] is Mobile && args[1] is MagicalFood && args[2] is TimeSpan )
            {
                EndInfluence( ( Mobile )args[0], ( MagicalFood )args[1], ( TimeSpan )args[2] );
            }
        }
    }

    public static void EndInfluence( Mobile mob, MagicalFood id, TimeSpan cooldown )
    {
        _table[mob] = ( int )_table[mob]! & ~( int )id;

        if ( cooldown != TimeSpan.Zero )
        {
            _cooldown ??= new Hashtable();
            _cooldown[mob] ??= 0;
            _cooldown[mob] = ( int )_cooldown[mob] | ( int )id;

            Timer.DelayCall( cooldown, EndCooldown, new object[] { mob, id } );
        }
    }

    public static void EndCooldown( object obj )
    {
        if ( obj is object[] { Length: 2 } args )
        {
            if ( args[0] is Mobile && args[1] is MagicalFood )
            {
                EndCooldown( ( Mobile )args[0], ( MagicalFood )args[1] );
            }
        }
    }

    public static void EndCooldown( Mobile mob, MagicalFood id )
    {
        _cooldown[mob] = ( int )_cooldown[mob]! & ~( int )id;
    }

    public override bool Eat( Mobile from )
    {
        if ( !IsUnderInfluence( from, FoodID ) )
        {
            if ( !CoolingDown( from, FoodID ) )
            {
                from.SendLocalizedMessage( EatMessage );

                StartInfluence( from, FoodID, Duration, Cooldown );
                Consume();

                return true;
            }

            from.SendLocalizedMessage( 1070772 ); // You must wait a few seconds before you can use that item.
        }

        return false;
    }
}
