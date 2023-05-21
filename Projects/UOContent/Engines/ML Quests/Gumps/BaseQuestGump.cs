using System.Collections.Generic;
using Server.Engines.MLQuests.Objectives;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Gumps
{
    public enum ButtonPosition : byte
    {
        Left,
        Right
    }

    public enum ButtonGraphic : ushort
    {
        Invalid,
        Accept = 0x2EE0,
        Clear = 0x2EE3,
        Close = 0x2EE6,
        Continue = 0x2EE9,
        Okay = 0x2EEC,
        Previous = 0x2EEF,
        Refuse = 0x2EF2,
        Resign = 0x2EF5
    }

    public abstract class BaseQuestGump : Gump
    {
        private readonly List<ButtonInfo> m_Buttons;
        private int m_Label;
        private int m_MaxPages;

        private int m_Page;
        private string m_Title;

        // RunUO optimized version
        public BaseQuestGump(int label)
            : base(75, 25)
        {
            m_Page = 0;
            m_MaxPages = 0;
            m_Label = label;
            m_Title = null;
            m_Buttons = new List<ButtonInfo>(2);

            Closable = false;

            AddPage(0);

            AddImageTiled(50, 20, 400, 460, 0x1404);
            AddImageTiled(50, 29, 30, 450, 0x28DC);
            AddImageTiled(34, 140, 17, 339, 0x242F);
            AddImage(48, 135, 0x28AB);
            AddImage(-16, 285, 0x28A2);
            AddImage(0, 10, 0x28B5);
            AddImage(25, 0, 0x28B4);
            AddImageTiled(83, 15, 350, 15, 0x280A);
            AddImage(34, 479, 0x2842);
            AddImage(442, 479, 0x2840);
            AddImageTiled(51, 479, 392, 17, 0x2775);
            AddImageTiled(415, 29, 44, 450, 0xA2D);
            AddImageTiled(415, 29, 30, 450, 0x28DC);
            // AddLabel( 100, 50, 0x481, "" );
            AddImage(370, 50, 0x589);
            AddImage(379, 60, 0x15A9);
            AddImage(425, 0, 0x28C9);
            AddImage(90, 33, 0x232D);
            AddHtmlLocalized(130, 45, 270, 16, label, 0xFFFFFF);
            AddImageTiled(130, 65, 175, 1, 0x238D);
        }

        public void BuildPage()
        {
            AddPage(++m_Page);

            if (m_Page > 1)
            {
                AddButton(
                    130,
                    430,
                    (int)ButtonGraphic.Previous,
                    (int)ButtonGraphic.Previous + 2,
                    0,
                    GumpButtonType.Page,
                    m_Page - 1
                );
            }

            if (m_Page < m_MaxPages)
            {
                AddButton(
                    275,
                    430,
                    (int)ButtonGraphic.Continue,
                    (int)ButtonGraphic.Continue + 2,
                    0,
                    GumpButtonType.Page,
                    m_Page + 1
                );
            }

            foreach (var button in m_Buttons)
            {
                AddButton(
                    button.Position == ButtonPosition.Left ? 95 : 313,
                    455,
                    (int)button.Graphic,
                    (int)button.Graphic + 2,
                    button.ButtonID
                );
            }

            if (m_Title != null)
            {
                AddHtmlLocalized(130, 68, 220, 48, 1114513, m_Title, 0x2710); // <DIV ALIGN=CENTER>~1_TOKEN~</DIV>
            }
        }

        public void SetPageCount(int maxPages)
        {
            m_MaxPages = maxPages;
        }

        public void SetTitle(TextDefinition def)
        {
            if (def.Number > 0)
            {
                m_Title = $"#{def.Number}"; // OSI does "@@#{0}" instead, why? KR client related?
            }
            else
            {
                m_Title = def.String;
            }
        }

        public void RegisterButton(ButtonPosition position, ButtonGraphic graphic, int buttonID)
        {
            m_Buttons.Add(new ButtonInfo(position, graphic, buttonID));
        }

        public void AddDescription(MLQuest quest)
        {
            AddHtmlLocalized(
                98,
                140,
                312,
                16,
                quest.IsChainTriggered || quest.NextQuest != null ? 1075024 : 1072202, // Description [(quest chain)]
                0x2710
            );

            quest.Description.AddHtmlText(this, 98, 156, 312, 240, false, true, 0x15F90, 0xBDE784);
        }

        public void AddObjectives(MLQuest quest)
        {
            AddHtmlLocalized(98, 140, 312, 16, 1049073, 0x2710); // Objective:
            // All of the following / Only one of the following
            AddHtmlLocalized(
                98,
                156,
                312,
                16,
                quest.ObjectiveType == ObjectiveType.All ? 1072208 : 1072209,
                0x2710
            );

            var y = 172;

            foreach (var objective in quest.Objectives)
            {
                objective.WriteToGump(this, ref y);

                if (objective.IsTimed)
                {
                    if (objective is CollectObjective)
                    {
                        y -= 16;
                    }

                    BaseObjectiveInstance.WriteTimeRemaining(this, ref y, objective.Duration);
                }
            }
        }

        public void AddObjectivesProgress(MLQuestInstance instance)
        {
            var quest = instance.Quest;

            AddHtmlLocalized(98, 140, 312, 16, 1049073, 0x2710); // Objective:
            // All of the following / Only one of the following
            AddHtmlLocalized(
                98,
                156,
                312,
                16,
                quest.ObjectiveType == ObjectiveType.All ? 1072208 : 1072209,
                0x2710
            );

            var y = 172;

            foreach (var objInstance in instance.Objectives)
            {
                objInstance.WriteToGump(this, ref y);
            }
        }

        public void AddRewardsPage(MLQuest quest) // For the quest log/offer gumps
        {
            AddHtmlLocalized(98, 140, 312, 16, 1072201, 0x2710); // Reward

            var y = 162;

            if (quest.Rewards.Count > 1)
            {
                // TODO: Is this what this is for? Does "Only one of the following" occur?
                AddHtmlLocalized(98, 156, 312, 16, 1072208, 0x2710); // All of the following
                y += 16;
            }

            AddRewards(quest, 105, y, 16);
        }

        public void AddRewards(MLQuest quest) // For the claim rewards gump
        {
            var y = 146;

            if (quest.Rewards.Count > 1)
            {
                // TODO: Is this what this is for? Does "Only one of the following" occur?
                AddHtmlLocalized(100, 140, 312, 16, 1072208, 0x2710); // All of the following
                y += 16;
            }

            AddRewards(quest, 107, y, 26);
        }

        public void AddRewards(MLQuest quest, int x, int y, int spacing)
        {
            var xReward = x + 28;

            foreach (var reward in quest.Rewards)
            {
                AddImage(x, y + 1, 0x4B9);
                reward.WriteToGump(this, xReward, ref y);
                y += spacing;
            }
        }

        public void AddConversation(TextDefinition text)
        {
            text.AddHtmlText(this, 98, 140, 312, 180, false, true, 0x15F90, 0xBDE784);
        }

        /* OSI gump IDs:
         * 800 - QuestOfferGump
         * 801 - QuestCancelConfirmGump
         * 802 - ?? (gets closed by Toggle Quest Item)
         * 803 - QuestRewardGump
         * 804 - ?? (gets closed by Toggle Quest Item)
         * 805 - QuestLogGump
         * 806 - QuestConversationGump (refuse / in progress)
         * 807 - ?? (gets closed by Toggle Quest Item and most quest gumps)
         * 808 - InfoNPCGump
         * 809 - QuestLogDetailedGump
         * 810 - QuestReportBackGump
         */
        public static void CloseOtherGumps(PlayerMobile pm)
        {
            pm.CloseGump<InfoNPCGump>();
            pm.CloseGump<QuestRewardGump>();
            pm.CloseGump<QuestConversationGump>();
            pm.CloseGump<QuestReportBackGump>();
            // pm.CloseGump( typeof( UnknownGump807 ) );
            pm.CloseGump<QuestCancelConfirmGump>();
        }

        private struct ButtonInfo
        {
            public ButtonPosition Position { get; }

            public ButtonGraphic Graphic { get; }

            public int ButtonID { get; }

            public ButtonInfo(ButtonPosition position, ButtonGraphic graphic, int buttonID)
            {
                Position = position;
                Graphic = graphic;
                ButtonID = buttonID;
            }
        }
    }
}
