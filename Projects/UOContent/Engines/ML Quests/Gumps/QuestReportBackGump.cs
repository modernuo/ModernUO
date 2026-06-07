using Server.Gumps;
using Server.Network;

namespace Server.Engines.MLQuests.Gumps
{
    public class QuestReportBackGump : BaseMLQuestGump
    {
        private readonly MLQuestInstance _instance;

        public override bool Singleton => true;

        private QuestReportBackGump(MLQuestInstance instance)
            : base(3006156) // Quest Conversation
        {
            _instance = instance;

            SetTitle(instance.Quest.Title);
            RegisterButton(ButtonPosition.Left, ButtonGraphic.Continue, 4);
            RegisterButton(ButtonPosition.Right, ButtonGraphic.Close, 3);

            SetPageCount(1);
        }

        public static void DisplayTo(Mobile from, MLQuestInstance instance)
        {
            if (from?.NetState == null || instance == null)
            {
                return;
            }

            // TODO: Check close sequence
            CloseOtherGumps(instance.Player);

            from.SendGump(new QuestReportBackGump(instance));
        }

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            BuildPage(ref builder);
            AddConversation(ref builder, _instance.Quest.CompletionMessage);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (info.ButtonID == 4)
            {
                _instance.ContinueReportBack(true);
            }
        }
    }
}
