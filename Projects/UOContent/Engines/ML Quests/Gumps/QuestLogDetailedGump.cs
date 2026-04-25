using Server.Gumps;
using Server.Network;

namespace Server.Engines.MLQuests.Gumps
{
    public class QuestLogDetailedGump : BaseMLQuestGump
    {
        private readonly bool _closeGumps;
        private readonly MLQuestInstance _instance;

        public override bool Singleton => true;

        private QuestLogDetailedGump(MLQuestInstance instance, bool closeGumps)
            : base(1046026) // Quest Log
        {
            _instance = instance;
            _closeGumps = closeGumps;

            SetTitle(instance.Quest.Title);
            RegisterButton(ButtonPosition.Left, ButtonGraphic.Resign, 1);
            RegisterButton(ButtonPosition.Right, ButtonGraphic.Okay, 2);

            SetPageCount(3);
        }

        public static void DisplayTo(Mobile from, MLQuestInstance instance, bool closeGumps = true)
        {
            if (from?.NetState == null || instance == null)
            {
                return;
            }

            if (closeGumps)
            {
                CloseOtherGumps(instance.Player);
            }

            from.SendGump(new QuestLogDetailedGump(instance, closeGumps));
        }

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            var quest = _instance.Quest;

            BuildPage(ref builder);
            AddDescription(ref builder, quest);

            if (_instance.Failed)                                       // only displayed on the first page
            {
                builder.AddHtmlLocalized(160, 80, 250, 16, 500039, 0x3C00); // Failed!
            }

            BuildPage(ref builder);
            AddObjectivesProgress(ref builder, _instance);

            BuildPage(ref builder);
            AddRewardsPage(ref builder, quest);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_instance.Removed)
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // Resign
                    {
                        // TODO: Custom reward loss protection? OSI doesn't have this
                        // if (_instance.ClaimReward)
                        // pm.SendMessage( "You cannot cancel a quest with rewards pending." );
                        // else

                        QuestCancelConfirmGump.DisplayTo(sender.Mobile, _instance, _closeGumps);

                        break;
                    }
                case 2: // Okay
                    {
                        QuestLogGump.DisplayTo(sender.Mobile, _instance.Player, _closeGumps);

                        break;
                    }
            }
        }
    }
}
