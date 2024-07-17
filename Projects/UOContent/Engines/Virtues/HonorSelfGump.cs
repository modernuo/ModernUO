using Server.Engines.Virtues;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps;

public class HonorSelfGump : StaticGump<HonorSelfGump>
{
    private readonly PlayerMobile _from;

    public HonorSelfGump(PlayerMobile from) : base(150, 50) => _from = from;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddBackground(0, 0, 245, 145, 9250);
        builder.AddButton(157, 101, 247, 248, 1);
        builder.AddButton(81, 100, 241, 248, 0);

        // Are you sure you want to use honor points on yourself?
        builder.AddHtmlLocalized(21, 20, 203, 70, 1071218, true);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            HonorVirtue.ActivateEmbrace(_from);
        }
    }
}
