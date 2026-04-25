using Server.Gumps;
using Server.Network;

namespace Server.Engines.VeteranRewards;

public class RewardNoticeGump : StaticGump<RewardNoticeGump>
{
    private readonly Mobile _from;

    public override bool Singleton => true;

    private RewardNoticeGump(Mobile from) : base(0, 0) => _from = from;

    public static void DisplayTo(Mobile from)
    {
        if (from?.NetState == null)
        {
            return;
        }

        from.SendGump(new RewardNoticeGump(from));
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(10, 10, 500, 135, 2600);

        /* You have reward items available.
         * Click 'ok' below to get the selection menu or 'cancel' to be prompted upon your next login.
         */
        builder.AddHtmlLocalized(52, 35, 420, 55, 1006046, true, true);

        builder.AddButton(60, 95, 4005, 4007, 1);
        builder.AddHtmlLocalized(95, 96, 150, 35, 1006044); // Ok

        builder.AddButton(285, 95, 4017, 4019, 0);
        builder.AddHtmlLocalized(320, 96, 150, 35, 1006045); // Cancel
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            RewardChoiceGump.DisplayTo(_from);
        }
    }
}
