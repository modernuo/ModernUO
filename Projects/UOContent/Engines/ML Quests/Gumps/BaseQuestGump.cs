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

    public abstract class BaseMLQuestGump : DynamicGump
    {
        private readonly List<ButtonInfo> _buttons;
        private readonly int _label;
        private int _maxPages;
        private int _page;
        private string _title;

        // RunUO optimized version
        protected BaseMLQuestGump(int label) : base(75, 25)
        {
            _label = label;
            _page = 0;
            _maxPages = 0;
            _title = null;
            _buttons = new List<ButtonInfo>(2);
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.SetNoClose();

            builder.AddPage();

            builder.AddImageTiled(50, 20, 400, 460, 0x1404);
            builder.AddImageTiled(50, 29, 30, 450, 0x28DC);
            builder.AddImageTiled(34, 140, 17, 339, 0x242F);
            builder.AddImage(48, 135, 0x28AB);
            builder.AddImage(-16, 285, 0x28A2);
            builder.AddImage(0, 10, 0x28B5);
            builder.AddImage(25, 0, 0x28B4);
            builder.AddImageTiled(83, 15, 350, 15, 0x280A);
            builder.AddImage(34, 479, 0x2842);
            builder.AddImage(442, 479, 0x2840);
            builder.AddImageTiled(51, 479, 392, 17, 0x2775);
            builder.AddImageTiled(415, 29, 44, 450, 0xA2D);
            builder.AddImageTiled(415, 29, 30, 450, 0x28DC);
            builder.AddImage(370, 50, 0x589);
            builder.AddImage(379, 60, 0x15A9);
            builder.AddImage(425, 0, 0x28C9);
            builder.AddImage(90, 33, 0x232D);
            builder.AddHtmlLocalized(130, 45, 270, 16, _label, 0x7FFF);
            builder.AddImageTiled(130, 65, 175, 1, 0x238D);

            // Reset paging for subclass content
            _page = 0;

            BuildContent(ref builder);
        }

        protected abstract void BuildContent(ref DynamicGumpBuilder builder);

        protected void BuildPage(ref DynamicGumpBuilder builder)
        {
            builder.AddPage(++_page);

            if (_page > 1)
            {
                builder.AddButton(
                    130,
                    430,
                    (int)ButtonGraphic.Previous,
                    (int)ButtonGraphic.Previous + 2,
                    0,
                    GumpButtonType.Page,
                    _page - 1
                );
            }

            if (_page < _maxPages)
            {
                builder.AddButton(
                    275,
                    430,
                    (int)ButtonGraphic.Continue,
                    (int)ButtonGraphic.Continue + 2,
                    0,
                    GumpButtonType.Page,
                    _page + 1
                );
            }

            for (var i = 0; i < _buttons.Count; i++)
            {
                var button = _buttons[i];
                builder.AddButton(
                    button.Position == ButtonPosition.Left ? 95 : 313,
                    455,
                    (int)button.Graphic,
                    (int)button.Graphic + 2,
                    button.ButtonID
                );
            }

            if (_title != null)
            {
                builder.AddHtmlLocalized(130, 68, 220, 48, 1114513, _title, 0x2710); // <DIV ALIGN=CENTER>~1_TOKEN~</DIV>
            }
        }

        protected void SetPageCount(int maxPages)
        {
            _maxPages = maxPages;
        }

        protected void SetTitle(TextDefinition def)
        {
            if (def.Number > 0)
            {
                _title = $"#{def.Number}"; // OSI does "@@#{0}" instead, why? KR client related?
            }
            else
            {
                _title = def.String;
            }
        }

        protected void RegisterButton(ButtonPosition position, ButtonGraphic graphic, int buttonID)
        {
            _buttons.Add(new ButtonInfo(position, graphic, buttonID));
        }

        protected static void AddDescription(ref DynamicGumpBuilder builder, MLQuest quest)
        {
            builder.AddHtmlLocalized(
                98,
                140,
                312,
                16,
                quest.IsChainTriggered || quest.NextQuest != null ? 1075024 : 1072202, // Description [(quest chain)]
                0x2710
            );

            quest.Description.AddHtmlText(ref builder, 98, 156, 312, 240, false, true, 0x5F90, 0xBDE784);
        }

        protected static void AddObjectives(ref DynamicGumpBuilder builder, MLQuest quest)
        {
            builder.AddHtmlLocalized(98, 140, 312, 16, 1049073, 0x2710); // Objective:
            // All of the following / Only one of the following
            builder.AddHtmlLocalized(
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
                objective.WriteToGump(ref builder, ref y);

                if (objective.IsTimed)
                {
                    if (objective is CollectObjective)
                    {
                        y -= 16;
                    }

                    BaseObjectiveInstance.WriteTimeRemaining(ref builder, ref y, objective.Duration);
                }
            }
        }

        protected static void AddObjectivesProgress(ref DynamicGumpBuilder builder, MLQuestInstance instance)
        {
            var quest = instance.Quest;

            builder.AddHtmlLocalized(98, 140, 312, 16, 1049073, 0x2710); // Objective:
            // All of the following / Only one of the following
            builder.AddHtmlLocalized(
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
                objInstance.WriteToGump(ref builder, ref y);
            }
        }

        protected static void AddRewardsPage(ref DynamicGumpBuilder builder, MLQuest quest) // For the quest log/offer gumps
        {
            builder.AddHtmlLocalized(98, 140, 312, 16, 1072201, 0x2710); // Reward

            var y = 162;

            if (quest.Rewards.Count > 1)
            {
                // TODO: Is this what this is for? Does "Only one of the following" occur?
                builder.AddHtmlLocalized(98, 156, 312, 16, 1072208, 0x2710); // All of the following
                y += 16;
            }

            AddRewards(ref builder, quest, 105, y, 16);
        }

        protected static void AddRewards(ref DynamicGumpBuilder builder, MLQuest quest) // For the claim rewards gump
        {
            var y = 146;

            if (quest.Rewards.Count > 1)
            {
                // TODO: Is this what this is for? Does "Only one of the following" occur?
                builder.AddHtmlLocalized(100, 140, 312, 16, 1072208, 0x2710); // All of the following
                y += 16;
            }

            AddRewards(ref builder, quest, 107, y, 26);
        }

        protected static void AddRewards(ref DynamicGumpBuilder builder, MLQuest quest, int x, int y, int spacing)
        {
            var xReward = x + 28;

            foreach (var reward in quest.Rewards)
            {
                builder.AddImage(x, y + 1, 0x4B9);
                reward.WriteToGump(ref builder, xReward, ref y);
                y += spacing;
            }
        }

        protected static void AddConversation(ref DynamicGumpBuilder builder, TextDefinition text)
        {
            text.AddHtmlText(ref builder, 98, 140, 312, 180, false, true, 0x5F90, 0xBDE784);
        }

        /* OSI gump IDs:
         * 800 - QuestOfferGump
         * 801 - QuestCancelConfirmGump
         * 802 - ?? (gets closed by Toggle Quest Item)
         * 803 - QuestRewardGump
         * 804 - ?? (gets closed by Toggle Quest Item and most quest gumps)
         * 805 - QuestLogGump
         * 806 - QuestConversationGump (refuse / in progress)
         * 807 - ?? (gets closed by Toggle Quest Item and most quest gumps)
         * 808 - InfoNPCGump
         * 809 - QuestLogDetailedGump
         * 810 - QuestReportBackGump
         */
        public static void CloseOtherGumps(PlayerMobile pm)
        {
            var gumps = pm.GetGumps();

            gumps.Close<InfoNPCGump>();
            gumps.Close<QuestRewardGump>();
            gumps.Close<QuestConversationGump>();
            gumps.Close<QuestReportBackGump>();
            gumps.Close<QuestCancelConfirmGump>();
        }

        private readonly struct ButtonInfo
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
