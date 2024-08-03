using System;
using Server.Items;
using Server.Network;

namespace Server.Gumps;

public class ConfirmPartyGump : Gump
{
    private readonly MasterKey _Key;

    public ConfirmPartyGump( MasterKey key )
        : base( 50, 50 )
    {
        _Key = key;
        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

        AddPage( 0 );
        AddBackground( 0, 0, 245, 145, 9250 );
        AddHtmlLocalized(
            21,
            20,
            203,
            70,
            1072525
        ); // <CENTER>Are you sure you want to teleport <BR>your party to an unknown area?</CENTER>
        AddButton( 157, 101, 247, 248, 1 );
        AddButton( 81, 100, 241, 248, 0 );
    }

    public override void OnResponse( NetState sender, in RelayInfo info )
    {
        var from = sender.Mobile;

        switch ( info.ButtonID )
        {
            case 0:
                {
                    break;
                }
            case 1:
                {
                    if ( _Key?.Altar == null )
                    {
                        return;
                    }

                    _Key.Altar.SendConfirmations( from );
                    _Key.Delete();

                    break;
                }
        }
    }
}

public class ConfirmEntranceGump : Gump
{
    private readonly PeerlessAltar _Altar;

    public ConfirmEntranceGump( PeerlessAltar altar, Mobile from )
        : base( 50, 50 )
    {
        from.CloseGump<ConfirmEntranceGump>();

        _Altar = altar;

        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

        AddPage( 0 );
        AddBackground( 0, 0, 245, 145, 9250 );
        AddHtmlLocalized(
            21,
            20,
            203,
            70,
            1072526
        ); // <CENTER>Your party is teleporting to an unknown area.<BR>Do you wish to go?</CENTER>
        AddButton( 157, 101, 247, 248, 1 );
        AddButton( 81, 100, 241, 248, 0 );

        Timer accept = new AcceptConfirmPeerlessPartyTimer( from );
        accept.Start();
    }

    public override void OnResponse( NetState sender, in RelayInfo info )
    {
        var from = sender.Mobile;

        if ( _Altar == null )
        {
            return;
        }

        var button = info.ButtonID;

        switch ( button )
        {
            case 0:
                {
                    break;
                }
            case 1:
                {
                    _Altar.Enter( from );

                    break;
                }
        }
    }
}

public class AcceptConfirmPeerlessPartyTimer( Mobile from ) : Timer(
    TimeSpan.FromSeconds( 60.0 ),
    TimeSpan.FromSeconds( 60.0 ),
    1
)
{
    protected override void OnTick()
    {
        from.CloseGump<ConfirmEntranceGump>();
        Stop();
    }
}

public class ConfirmExitGump( object altar ) : BaseConfirmGump
{
    public override int LabelNumber => 1075026; // Are you sure you wish to teleport?

    public override void Confirm( Mobile from )
    {
        if ( altar == null )
        {
            return;
        }

        if ( altar is PeerlessAltar )
        {
            ( ( PeerlessAltar )altar ).Exit( from );
        }
    }
}
