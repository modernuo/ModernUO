using Server.Gumps;

namespace Server.Engines.ConPVP;

public class BeginGump : StaticGump<BeginGump>
{
    private const int LabelColor32 = 0xFFFFFF;
    private const int BlackColor32 = 0x000008;

    public override bool Singleton => true;

    private BeginGump() : base(50, 50)
    {
    }

    public static void DisplayTo(Mobile from, int count)
    {
        if (from?.NetState == null)
        {
            return;
        }

        from.SendGump(new BeginGump());
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        const int offset = 50;

        builder.AddBackground(1, 1, 398, 202 - offset, 3600);

        builder.AddImageTiled(16, 15, 369, 173 - offset, 3604);
        builder.AddAlphaRegion(16, 15, 369, 173 - offset);

        builder.AddImage(215, -43, 0xEE40);

        var duelCountdownBlack = "Duel Countdown".Center(BlackColor32);
        builder.AddHtml(22 - 1, 22, 294, 20, duelCountdownBlack);
        builder.AddHtml(22 + 1, 22, 294, 20, duelCountdownBlack);
        builder.AddHtml(22, 22 - 1, 294, 20, duelCountdownBlack);
        builder.AddHtml(22, 22 + 1, 294, 20, duelCountdownBlack);
        builder.AddHtml(22, 22, 294, 20, "Duel Countdown".Center(LabelColor32));

        var beginMessageBlack =
            "The arranged duel is about to begin. During this countdown period you may not cast spells and you may not move. This message will close automatically when the period ends."
                .Color(BlackColor32);

        builder.AddHtml(22 - 1, 50, 294, 80, beginMessageBlack);
        builder.AddHtml(22 + 1, 50, 294, 80, beginMessageBlack);
        builder.AddHtml(22, 50 - 1, 294, 80, beginMessageBlack);
        builder.AddHtml(22, 50 + 1, 294, 80, beginMessageBlack);
        builder.AddHtml(
            22,
            50,
            294,
            80,
            "The arranged duel is about to begin. During this countdown period you may not cast spells and you may not move. This message will close automatically when the period ends."
                .Color(0xFFCC66)
        );

        builder.AddButton(314 - 50, 157 - offset, 247, 248, 1);
    }
}
