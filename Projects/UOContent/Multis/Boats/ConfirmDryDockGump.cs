using Server.Gumps;
using Server.Network;

namespace Server.Multis;

public class ConfirmDryDockGump : StaticGump<ConfirmDryDockGump>
{
    private readonly BaseBoat _boat;

    public ConfirmDryDockGump(BaseBoat boat) : base(150, 200)
    {
        _boat = boat;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 220, 170, 5054);
        builder.AddBackground(10, 10, 200, 150, 3000);

        builder.AddHtmlLocalized(20, 20, 180, 80, 1018319, true); // Do you wish to dry dock this boat?

        builder.AddHtmlLocalized(55, 100, 140, 25, 1011011); // CONTINUE
        builder.AddButton(20, 100, 4005, 4007, 2);

        builder.AddHtmlLocalized(55, 125, 140, 25, 1011012); // CANCEL
        builder.AddButton(20, 125, 4005, 4007, 1);
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 2)
        {
            _boat.EndDryDock(state.Mobile);
        }
    }
}
