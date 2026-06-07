using Server.Gumps;
using Server.Network;

namespace Server.Engines.MLQuests.Gumps
{
    public class QuestCancelConfirmGump : DynamicGump
    {
        private readonly bool _closeGumps;
        private readonly MLQuestInstance _instance;

        public override bool Singleton => true;

        private QuestCancelConfirmGump(MLQuestInstance instance, bool closeGumps) : base(120, 50)
        {
            _instance = instance;
            _closeGumps = closeGumps;
        }

        public static void DisplayTo(Mobile from, MLQuestInstance instance, bool closeGumps = true)
        {
            if (from?.NetState == null || instance == null)
            {
                return;
            }

            if (closeGumps)
            {
                BaseMLQuestGump.CloseOtherGumps(instance.Player);
            }

            from.SendGump(new QuestCancelConfirmGump(instance, closeGumps));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.SetNoClose();

            builder.AddPage();

            builder.AddImageTiled(0, 0, 348, 262, 0xA8E);
            builder.AddAlphaRegion(0, 0, 348, 262);

            builder.AddImage(0, 15, 0x27A8);
            builder.AddImageTiled(0, 30, 17, 200, 0x27A7);
            builder.AddImage(0, 230, 0x27AA);

            builder.AddImage(15, 0, 0x280C);
            builder.AddImageTiled(30, 0, 300, 17, 0x280A);
            builder.AddImage(315, 0, 0x280E);

            builder.AddImage(15, 244, 0x280C);
            builder.AddImageTiled(30, 244, 300, 17, 0x280A);
            builder.AddImage(315, 244, 0x280E);

            builder.AddImage(330, 15, 0x27A8);
            builder.AddImageTiled(330, 30, 17, 200, 0x27A7);
            builder.AddImage(330, 230, 0x27AA);

            builder.AddImage(333, 2, 0x2716);
            builder.AddImage(333, 248, 0x2716);
            builder.AddImage(2, 248, 0x2716);
            builder.AddImage(2, 2, 0x2716);

            builder.AddHtmlLocalized(25, 22, 200, 20, 1049000, 0x7D00); // Confirm Quest Cancellation
            builder.AddImage(25, 40, 0xBBF);

            /*
             * This quest will give you valuable information, skills
             * and equipment that will help you advance in the
             * game at a quicker pace.<BR>
             * <BR>
             * Are you certain you wish to cancel at this time?
             */
            builder.AddHtmlLocalized(25, 55, 300, 120, 1060836, 0x7FFF);

            var quest = _instance.Quest;

            if (quest.IsChainTriggered || quest.NextQuest != null)
            {
                builder.AddRadio(25, 145, 0x25F8, 0x25FB, false, 2);
                builder.AddHtmlLocalized(60, 150, 280, 20, 1075023, 0x7FFF); // Yes, I want to quit this entire chain!
            }

            builder.AddRadio(25, 180, 0x25F8, 0x25FB, true, 1);
            builder.AddHtmlLocalized(60, 185, 280, 20, 1049005, 0x7FFF); // Yes, I really want to quit this quest!

            builder.AddRadio(25, 215, 0x25F8, 0x25FB, false, 0);
            builder.AddHtmlLocalized(60, 220, 280, 20, 1049006, 0x7FFF); // No, I don't want to quit.

            builder.AddButton(265, 220, 0xF7, 0xF8, 7);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_instance.Removed)
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 7: // Okay
                    {
                        if (info.IsSwitched(2))
                        {
                            _instance.Cancel(true);
                        }
                        else if (info.IsSwitched(1))
                        {
                            _instance.Cancel(false);
                        }

                        QuestLogGump.DisplayTo(sender.Mobile, _instance.Player, _closeGumps);
                        break;
                    }
            }
        }
    }
}
