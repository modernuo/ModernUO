using System.Collections;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.ConPVP
{
    public class RulesetGump : Gump
    {
        private readonly DuelContext m_DuelContext;
        private readonly Mobile m_From;
        private readonly RulesetLayout m_Page;
        private readonly bool m_ReadOnly;
        private readonly Ruleset m_Ruleset;

        public RulesetGump(Mobile from, Ruleset ruleset, RulesetLayout page, DuelContext duelContext, bool readOnly = false)
            : base(readOnly ? 310 : 50, 50)
        {
            m_From = from;
            m_Ruleset = ruleset;
            m_Page = page;
            m_DuelContext = duelContext;
            m_ReadOnly = readOnly;

            Draggable = !readOnly;

            from.CloseGump<RulesetGump>();
            from.CloseGump<DuelContextGump>();
            from.CloseGump<ParticipantGump>();

            var depthCounter = page;

            while (depthCounter != null)
            {
                depthCounter = depthCounter.Parent;
            }

            var count = page.Children.Length + page.Options.Length;

            AddPage(0);

            var height = 35 + 10 + 2 + count * 22 + 2 + 30;

            AddBackground(0, 0, 260, height, 9250);
            AddBackground(10, 10, 240, height - 20, 0xDAC);

            AddHtml(35, 25, 190, 20, Center(page.Title));

            var x = 35;
            var y = 47;

            for (var i = 0; i < page.Children.Length; ++i)
            {
                AddGoldenButton(x, y, 1 + i);
                AddHtml(x + 25, y, 250, 22, page.Children[i].Title);

                y += 22;
            }

            for (var i = 0; i < page.Options.Length; ++i)
            {
                var enabled = ruleset.Options[page.Offset + i];

                if (readOnly)
                {
                    AddImage(x, y, enabled ? 0xD3 : 0xD2);
                }
                else
                {
                    AddCheck(x, y, 0xD2, 0xD3, enabled, i);
                }

                AddHtml(x + 25, y, 250, 22, page.Options[i]);

                y += 22;
            }
        }

        public string Center(string text) => $"<CENTER>{text}</CENTER>";

        public void AddGoldenButton(int x, int y, int bid)
        {
            AddButton(x, y, 0xD2, 0xD2, bid);
            AddButton(x + 3, y + 3, 0xD8, 0xD8, bid);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_DuelContext?.Registered == false)
            {
                return;
            }

            if (!m_ReadOnly)
            {
                var opts = new BitArray(m_Page.Options.Length);

                for (var i = 0; i < info.Switches.Length; ++i)
                {
                    var sid = info.Switches[i];

                    if (sid >= 0 && sid < m_Page.Options.Length)
                    {
                        opts[sid] = true;
                    }
                }

                for (var i = 0; i < opts.Length; ++i)
                {
                    if (m_Ruleset.Options[m_Page.Offset + i] != opts[i])
                    {
                        m_Ruleset.Options[m_Page.Offset + i] = opts[i];
                        m_Ruleset.Changed = true;
                    }
                }
            }

            var bid = info.ButtonID;

            if (bid == 0)
            {
                if (m_Page.Parent != null)
                {
                    m_From.SendGump(new RulesetGump(m_From, m_Ruleset, m_Page.Parent, m_DuelContext, m_ReadOnly));
                }
                else if (!m_ReadOnly)
                {
                    m_From.SendGump(new PickRulesetGump(m_From, m_DuelContext, m_Ruleset));
                }
            }
            else
            {
                bid -= 1;

                if (bid >= 0 && bid < m_Page.Children.Length)
                {
                    m_From.SendGump(new RulesetGump(m_From, m_Ruleset, m_Page.Children[bid], m_DuelContext, m_ReadOnly));
                }
            }
        }
    }
}
