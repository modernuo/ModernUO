using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.MLQuests.Gumps
{
    public class QuestLogGump : BaseMLQuestGump
    {
        private readonly bool _closeGumps;
        private readonly PlayerMobile _owner;

        public override bool Singleton => true;

        private QuestLogGump(PlayerMobile pm, bool closeGumps)
            : base(1046026) // Quest Log
        {
            _owner = pm;
            _closeGumps = closeGumps;

            RegisterButton(ButtonPosition.Right, ButtonGraphic.Okay, 3);

            SetPageCount(1);
        }

        public static void DisplayTo(Mobile from, PlayerMobile pm, bool closeGumps = true)
        {
            if (from?.NetState == null || pm == null)
            {
                return;
            }

            if (closeGumps)
            {
                pm.CloseGump<QuestLogDetailedGump>();
            }

            from.SendGump(new QuestLogGump(pm, closeGumps));
        }

        public static void DisplayTo(PlayerMobile pm)
        {
            if (pm?.NetState == null)
            {
                return;
            }

            pm.CloseGump<QuestLogDetailedGump>();
            pm.SendGump(new QuestLogGump(pm, true));
        }

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            BuildPage(ref builder);

            int numberColor, stringColor;

            var context = MLQuestSystem.GetContext(_owner);

            if (context != null)
            {
                var instances = context.QuestInstances;

                for (var i = 0; i < instances.Count; ++i)
                {
                    if (instances[i].Failed)
                    {
                        numberColor = 0x3C00;
                        stringColor = 0x7B0000;
                    }
                    else
                    {
                        numberColor = stringColor = 0xFFFFFF;
                    }

                    instances[i].Quest.Title.AddHtmlText(
                        ref builder,
                        98,
                        140 + 21 * i,
                        270,
                        21,
                        false,
                        false,
                        numberColor,
                        stringColor
                    );
                    builder.AddButton(368, 140 + 21 * i, 0x26B0, 0x26B1, 6 + i, GumpButtonType.Reply, 1);
                }
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (info.ButtonID < 6)
            {
                return;
            }

            var context = MLQuestSystem.GetContext(_owner);

            if (context == null)
            {
                return;
            }

            var instances = context.QuestInstances;
            var index = info.ButtonID - 6;

            if (index >= instances.Count)
            {
                return;
            }

            QuestLogDetailedGump.DisplayTo(sender.Mobile, instances[index], _closeGumps);
        }
    }
}
