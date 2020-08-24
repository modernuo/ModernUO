using Server.Mobiles;

namespace Server.Engines.MLQuests.Gumps
{
    public class QuestConversationGump : BaseQuestGump
    {
        public QuestConversationGump(MLQuest quest, PlayerMobile pm, TextDefinition text)
            : base(3006156) // Quest Conversation
        {
            CloseOtherGumps(pm);

            SetTitle(quest.Title);
            RegisterButton(ButtonPosition.Right, ButtonGraphic.Close, 3);

            SetPageCount(1);

            BuildPage();
            AddConversation(text);
        }
    }
}
