using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Gumps
{
    public class QuestConversationGump : BaseMLQuestGump
    {
        private readonly TextDefinition _text;

        public override bool Singleton => true;

        private QuestConversationGump(MLQuest quest, TextDefinition text)
            : base(3006156) // Quest Conversation
        {
            _text = text;

            SetTitle(quest.Title);
            RegisterButton(ButtonPosition.Right, ButtonGraphic.Close, 3);

            SetPageCount(1);
        }

        public static void DisplayTo(PlayerMobile pm, MLQuest quest, TextDefinition text)
        {
            if (pm?.NetState == null || quest == null)
            {
                return;
            }

            CloseOtherGumps(pm);

            pm.SendGump(new QuestConversationGump(quest, text));
        }

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            BuildPage(ref builder);
            AddConversation(ref builder, _text);
        }
    }
}
