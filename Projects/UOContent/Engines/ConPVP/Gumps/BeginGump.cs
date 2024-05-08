using Server.Gumps;

namespace Server.Engines.ConPVP
{
    public class BeginGump : Gump
    {
        private const int LabelColor32 = 0xFFFFFF;
        private const int BlackColor32 = 0x000008;

        public BeginGump(int count) : base(50, 50)
        {
            AddPage(0);

            const int offset = 50;

            AddBackground(1, 1, 398, 202 - offset, 3600);

            AddImageTiled(16, 15, 369, 173 - offset, 3604);
            AddAlphaRegion(16, 15, 369, 173 - offset);

            AddImage(215, -43, 0xEE40);

            var duelCountdownBlack = "Duel Countdown".Center(BlackColor32);
            AddHtml(22 - 1, 22, 294, 20, duelCountdownBlack);
            AddHtml(22 + 1, 22, 294, 20, duelCountdownBlack);
            AddHtml(22, 22 - 1, 294, 20, duelCountdownBlack);
            AddHtml(22, 22 + 1, 294, 20, duelCountdownBlack);
            AddHtml(22, 22, 294, 20, "Duel Countdown".Center(LabelColor32));

            var beginMessageBlack =
                "The arranged duel is about to begin. During this countdown period you may not cast spells and you may not move. This message will close automatically when the period ends."
                    .Color(BlackColor32);

            AddHtml(22 - 1, 50, 294, 80, beginMessageBlack);
            AddHtml(22 + 1, 50, 294, 80, beginMessageBlack);
            AddHtml(22, 50 - 1, 294, 80, beginMessageBlack);
            AddHtml(22, 50 + 1, 294, 80, beginMessageBlack);
            AddHtml(
                22,
                50,
                294,
                80,
                "The arranged duel is about to begin. During this countdown period you may not cast spells and you may not move. This message will close automatically when the period ends."
                    .Color(0xFFCC66)
            );

            AddButton(314 - 50, 157 - offset, 247, 248, 1);
        }
    }
}
