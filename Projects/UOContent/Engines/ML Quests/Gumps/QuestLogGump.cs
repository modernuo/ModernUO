using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.MLQuests.Gumps
{
    public class QuestLogGump : BaseQuestGump
    {
        private readonly bool m_CloseGumps;
        private readonly PlayerMobile m_Owner;

        public QuestLogGump(PlayerMobile pm, bool closeGumps = true)
            : base(1046026) // Quest Log
        {
            m_Owner = pm;
            m_CloseGumps = closeGumps;

            if (closeGumps)
            {
                pm.CloseGump<QuestLogGump>();
                pm.CloseGump<QuestLogDetailedGump>();
            }

            RegisterButton(ButtonPosition.Right, ButtonGraphic.Okay, 3);

            SetPageCount(1);

            BuildPage();

            int numberColor, stringColor;

            var context = MLQuestSystem.GetContext(pm);

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
                        this,
                        98,
                        140 + 21 * i,
                        270,
                        21,
                        false,
                        false,
                        numberColor,
                        stringColor
                    );
                    AddButton(368, 140 + 21 * i, 0x26B0, 0x26B1, 6 + i, GumpButtonType.Reply, 1);
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID < 6)
            {
                return;
            }

            var context = MLQuestSystem.GetContext(m_Owner);

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

            sender.Mobile.SendGump(new QuestLogDetailedGump(instances[index], m_CloseGumps));
        }
    }
}
