using Server.Mobiles;
using Server.Network;

namespace Server.Gumps;

public class ConfirmReleaseGump : StaticGump<ConfirmReleaseGump>
{
    private readonly Mobile _from;
    private readonly BaseCreature _pet;

    public override bool Singleton => true;

    public ConfirmReleaseGump(Mobile from, BaseCreature pet) : base(50, 50)
    {
        _from = from;
        _pet = pet;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 270, 120, 5054);
        builder.AddBackground(10, 10, 250, 100, 3000);

        // Are you sure you want to release your pet?
        builder.AddHtmlLocalized(20, 15, 230, 60, 1046257, true, true);

        builder.AddButton(20, 80, 4005, 4007, 2);
        builder.AddHtmlLocalized(55, 80, 75, 20, 1011011); // CONTINUE

        builder.AddButton(135, 80, 4005, 4007, 1);
        builder.AddHtmlLocalized(170, 80, 75, 20, 1011012); // CANCEL
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID != 2 || _pet.Deleted ||
            !(_pet.Controlled && _from == _pet.ControlMaster &&
              _from.CheckAlive() && _pet.Map == _from.Map && _pet.InRange(_from, 14)))
        {
            return;
        }

        _pet.ControlTarget = null;
        _pet.ControlOrder = OrderType.Release;
    }
}
