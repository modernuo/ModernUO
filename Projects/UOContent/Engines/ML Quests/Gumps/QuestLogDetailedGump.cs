using Server.Gumps;
using Server.Network;

namespace Server.Engines.MLQuests.Gumps
{
    public class QuestLogDetailedGump : BaseQuestGump
    {
        private readonly bool m_CloseGumps;
        private readonly MLQuestInstance m_Instance;

        public QuestLogDetailedGump(MLQuestInstance instance, bool closeGumps = true)
            : base(1046026) // Quest Log
        {
            m_Instance = instance;
            m_CloseGumps = closeGumps;

            var pm = instance.Player;
            var quest = instance.Quest;

            if (closeGumps)
            {
                CloseOtherGumps(pm);
                pm.CloseGump<QuestLogDetailedGump>();
            }

            SetTitle(quest.Title);
            RegisterButton(ButtonPosition.Left, ButtonGraphic.Resign, 1);
            RegisterButton(ButtonPosition.Right, ButtonGraphic.Okay, 2);

            SetPageCount(3);

            BuildPage();
            AddDescription(quest);

            if (instance.Failed)                                    // only displayed on the first page
            {
                AddHtmlLocalized(160, 80, 250, 16, 500039, 0x3C00); // Failed!
            }

            BuildPage();
            AddObjectivesProgress(instance);

            BuildPage();
            AddRewardsPage(quest);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Instance.Removed)
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // Resign
                    {
                        // TODO: Custom reward loss protection? OSI doesn't have this
                        // if (m_Instance.ClaimReward)
                        // pm.SendMessage( "You cannot cancel a quest with rewards pending." );
                        // else

                        sender.Mobile.SendGump(new QuestCancelConfirmGump(m_Instance, m_CloseGumps));

                        break;
                    }
                case 2: // Okay
                    {
                        sender.Mobile.SendGump(new QuestLogGump(m_Instance.Player, m_CloseGumps));

                        break;
                    }
            }
        }
    }
}
