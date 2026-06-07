using Server.Gumps;
using Server.Network;

namespace Server.Engines.MLQuests.Gumps
{
    public class QuestRewardGump : BaseMLQuestGump
    {
        private readonly MLQuestInstance _instance;

        public override bool Singleton => true;

        private QuestRewardGump(MLQuestInstance instance)
            : base(1072201) // Reward
        {
            _instance = instance;

            SetTitle(instance.Quest.Title);
            RegisterButton(ButtonPosition.Left, ButtonGraphic.Accept, 1);

            SetPageCount(1);
        }

        public static void DisplayTo(Mobile from, MLQuestInstance instance)
        {
            if (from?.NetState == null || instance == null)
            {
                return;
            }

            CloseOtherGumps(instance.Player);

            from.SendGump(new QuestRewardGump(instance));
        }

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            BuildPage(ref builder);
            AddRewards(ref builder, _instance.Quest);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                _instance.ClaimRewards();
            }
        }
    }
}
