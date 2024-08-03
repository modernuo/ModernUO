using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator( 0, false )]
public partial class PeerlessTeleporter : Teleporter
{
    [SerializableField( 0 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    public PeerlessAltar _altar;

    [Constructible]
    public PeerlessTeleporter( PeerlessAltar altar) => Altar = altar;

    [Constructible]
    public PeerlessTeleporter()
        : this( null )
    {
    }

    public override bool OnMoveOver( Mobile m )
    {
        if ( m.Alive )
        {
            m.CloseGump<ConfirmExitGump>();
            m.SendGump( new ConfirmExitGump( Altar ) );
        }
        else if ( Altar != null )
        {
            Altar.Exit( m );
            return false;
        }

        return true;
    }
}
