using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.MLQuests.Gumps
{
    public class QuestOfferGump : BaseMLQuestGump
    {
        private readonly MLQuest _quest;
        private readonly IQuestGiver _quester;

        public override bool Singleton => true;

        private QuestOfferGump(MLQuest quest, IQuestGiver quester)
            : base(1049010) // Quest Offer
        {
            _quest = quest;
            _quester = quester;

            SetTitle(quest.Title);
            RegisterButton(ButtonPosition.Left, ButtonGraphic.Accept, 1);
            RegisterButton(ButtonPosition.Right, ButtonGraphic.Refuse, 2);

            SetPageCount(3);
        }

        public static void DisplayTo(PlayerMobile pm, MLQuest quest, IQuestGiver quester)
        {
            if (pm?.NetState == null || quest == null)
            {
                return;
            }

            CloseOtherGumps(pm);

            pm.SendGump(new QuestOfferGump(quest, quester));
        }

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            BuildPage(ref builder);
            AddDescription(ref builder, _quest);

            BuildPage(ref builder);
            AddObjectives(ref builder, _quest);

            BuildPage(ref builder);
            AddRewardsPage(ref builder, _quest);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (sender.Mobile is not PlayerMobile pm)
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // Accept
                    {
                        _quest.OnAccept(_quester, pm);
                        break;
                    }
                case 2: // Refuse
                    {
                        _quest.OnRefuse(_quester, pm);
                        break;
                    }
            }
        }
    }
}
