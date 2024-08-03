using System.Linq;
using ModernUO.Serialization;
using Server.Engines.PartySystem;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator( 0, false )]
public partial class MasterKey : PeerlessKey
{
    [SerializableField( 0 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private PeerlessAltar _altar;

    [Constructible]
    public MasterKey( int itemID ) : base( itemID ) => LootType = LootType.Blessed;

    public override int LabelNumber => 1074348; // master key

    public override void OnDoubleClick( Mobile from )
    {
        if ( !IsChildOf( from.Backpack ) )
        {
            from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
        }
        else
        {
            if ( !CanOfferConfirmation( from ) && Altar == null )
            {
                return;
            }

            if ( Altar.Peerless == null )
            {
                from.CloseGump<ConfirmPartyGump>();
                from.SendGump( new ConfirmPartyGump( this ) );
            }
            else
            {
                var p = Party.Get( from );

                if ( p != null )
                {
                    foreach ( var m in p.Members.Select( x => x.Mobile ).Where( m => m.InRange( from.Location, 25 ) ) )
                    {
                        m.CloseGump<ConfirmEntranceGump>();
                        m.SendGump( new ConfirmEntranceGump( Altar, m ) );
                    }
                }
                else
                {
                    from.CloseGump<ConfirmEntranceGump>();
                    from.SendGump( new ConfirmEntranceGump( Altar, from ) );
                }
            }
        }
    }

    public override void OnAfterDelete()
    {
        if ( Altar == null )
        {
            return;
        }

        Altar.MasterKeys.Remove( this );

        if ( Altar.MasterKeys.Count == 0 && Altar.Fighters.Count == 0 )
        {
            Altar.FinishSequence();
        }
    }

    public virtual bool CanOfferConfirmation( Mobile from )
    {
        if ( Altar != null && Altar.Fighters.Contains( from ) )
        {
            from.SendLocalizedMessage( 1063296 ); // You may not use that teleporter at this time.
            return false;
        }

        return true;
    }
}
