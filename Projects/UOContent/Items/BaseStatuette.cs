using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.Items;

[SerializationGenerator( 0 )]
public partial class BaseStatuette : Item
{
    [SerializedCommandProperty( AccessLevel.GameMaster )] [SerializableField( 0 )]
    private bool _turnedOn;

    [Constructible]
    public BaseStatuette( int itemID )
        : base( itemID ) =>
        LootType = LootType.Blessed;

    public override bool HandlesOnMovement => TurnedOn && IsLockedDown;
    public override double DefaultWeight => 1.0;

    public override void OnMovement( Mobile m, Point3D oldLocation )
    {
        if ( TurnedOn && IsLockedDown && ( !m.Hidden || m is PlayerMobile ) && Utility.InRange( m.Location, Location, 2 ) &&
             !Utility.InRange( oldLocation, Location, 2 ) )
        {
            PlaySound( m );
        }

        base.OnMovement( m, oldLocation );
    }

    public virtual void PlaySound( Mobile to )
    {
    }

    public override void GetProperties( IPropertyList list )
    {
        base.GetProperties( list );

        if ( TurnedOn )
        {
            list.Add( 502695 ); // turned on
        }
        else
        {
            list.Add( 502696 ); // turned off
        }
    }

    public bool IsOwner( Mobile mob )
    {
        var house = BaseHouse.FindHouseAt( this );

        return house != null && house.IsOwner( mob );
    }

    public override void OnDoubleClick( Mobile from )
    {
        if ( IsOwner( from ) )
        {
            var onOffGump = new OnOffGump( this );
            from.SendGump( onOffGump );
        }
        else
        {
            from.SendLocalizedMessage( 502691 ); // You must be the owner to use this.
        }
    }

    private class OnOffGump : Gump
    {
        private readonly BaseStatuette m_Statuette;

        public OnOffGump( BaseStatuette statuette )
            : base( 150, 200 )
        {
            m_Statuette = statuette;

            AddBackground( 0, 0, 300, 150, 0xA28 );

            AddHtmlLocalized( 45, 20, 300, 35, statuette.TurnedOn ? 1011035 : 1011034 ); // [De]Activate this item

            AddButton( 40, 53, 0xFA5, 0xFA7, 1 );
            AddHtmlLocalized( 80, 55, 65, 35, 1011036 ); // OKAY

            AddButton( 150, 53, 0xFA5, 0xFA7, 0 );
            AddHtmlLocalized( 190, 55, 100, 35, 1011012 ); // CANCEL
        }

        public override void OnResponse( NetState sender, in RelayInfo info )
        {
            var from = sender.Mobile;

            if ( info.ButtonID == 1 )
            {
                var newValue = !m_Statuette.TurnedOn;
                m_Statuette.TurnedOn = newValue;

                if ( newValue && !m_Statuette.IsLockedDown )
                {
                    from.SendLocalizedMessage( 502693 ); // Remember, this only works when locked down.
                }
            }
            else
            {
                from.SendLocalizedMessage( 502694 ); // Cancelled action.
            }
        }
    }
}
