using Server.Gumps;

namespace Server.Engines.MLQuests.Gumps
{
    public class InfoNPCGump : BaseMLQuestGump
    {
        private readonly TextDefinition _title;
        private readonly TextDefinition _message;

        public override bool Singleton => true;

        private InfoNPCGump(TextDefinition title, TextDefinition message)
            : base(1060668) // INFORMATION
        {
            _title = title;
            _message = message;

            RegisterButton(ButtonPosition.Left, ButtonGraphic.Close, 3);

            SetPageCount(1);
        }

        public static void DisplayTo(Mobile from, TextDefinition title, TextDefinition message)
        {
            if (from?.NetState == null)
            {
                return;
            }

            from.SendGump(new InfoNPCGump(title, message));
        }

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            BuildPage(ref builder);
            _title.AddHtmlText(ref builder, 160, 108, 250, 16, false, false, 0x2710, 0x4AC684);
            _message.AddHtmlText(ref builder, 98, 156, 312, 180, false, true, 0x5F90, 0xBDE784);
        }
    }
}
