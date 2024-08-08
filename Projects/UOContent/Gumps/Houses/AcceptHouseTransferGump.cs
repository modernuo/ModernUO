using Server.Multis;
using Server.Network;

namespace Server.Gumps;

public class AcceptHouseTransferGump : StaticGump<AcceptHouseTransferGump>
{
    private readonly Mobile _transferor;
    private readonly BaseHouse _house;

    public override bool Singleton => true;

    public AcceptHouseTransferGump(Mobile transferor, BaseHouse house) : base(110, 100)
    {
        _transferor = transferor;
        _house = house;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.SetNoClose();

        builder.AddPage();

        builder.AddBackground(0, 0, 420, 280, 5054);

        builder.AddImageTiled(10, 10, 400, 20, 2624);
        builder.AddAlphaRegion(10, 10, 400, 20);

        builder.AddHtmlLocalized(10, 10, 400, 20, 1060635, 30720); // <CENTER>WARNING</CENTER>

        builder.AddImageTiled(10, 40, 400, 200, 2624);
        builder.AddAlphaRegion(10, 40, 400, 200);

        /*
         * Another player is attempting to initiate a house trade with you.
         * In order for you to see this window, both you and the other person are standing within two paces of the house to be traded.
         * If you click OKAY below, a house trade scroll will appear in your trade window and you can complete the transaction.
         * This scroll is a distinctive blue color and will show the name of the house, the name of the owner of that house, and the sextant coordinates of the center of the house when you hover your mouse over it.
         * In order for the transaction to be successful, you both must accept the trade and you both must remain within two paces of the house sign.
         * <BR><BR>Accepting this house in trade will <a href = "?ForceTopic97">condemn</a> any and all of your other houses that you may have.
         * All of your houses on <U>all shards</U> will be affected.
         * <BR><BR>In addition, you will not be able to place another house or have one transferred to you for one (1) real-life week.<BR><BR>
         * Once you accept these terms, these effects cannot be reversed.
         * Re-deeding or transferring your new house will <U>not</U> uncondemn your other house(s) nor will the one week timer be removed.<BR><BR>
         * If you are absolutely certain you wish to proceed, click the button next to OKAY below.
         * If you do not wish to trade for this house, click CANCEL.
         */
        builder.AddHtmlLocalized(10, 40, 400, 200, 1062086, 32512, false, true);

        builder.AddImageTiled(10, 250, 400, 20, 2624);
        builder.AddAlphaRegion(10, 250, 400, 20);

        builder.AddButton(10, 250, 4005, 4007, 1);
        builder.AddHtmlLocalized(40, 250, 170, 20, 1011036, 32767); // OKAY

        builder.AddButton(210, 250, 4005, 4007, 0);
        builder.AddHtmlLocalized(240, 250, 170, 20, 1011012, 32767); // CANCEL
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 1 && !_house.Deleted)
        {
            _house.EndConfirmTransfer(_transferor, state.Mobile);
        }
    }
}
