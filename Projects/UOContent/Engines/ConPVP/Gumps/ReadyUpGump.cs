using System.Collections;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.ConPVP
{
    public class ReadyUpGump : Gump
    {
        private readonly DuelContext m_Context;
        private readonly Mobile m_From;

        public ReadyUpGump(Mobile from, DuelContext context) : base(50, 50)
        {
            m_From = from;
            m_Context = context;

            Closable = false;
            AddPage(0);

            if (context.Rematch)
            {
                var height = 25 + 20 + 10 + 22 + 25;

                AddBackground(0, 0, 210, height, 9250);
                AddBackground(10, 10, 190, height - 20, 0xDAC);

                AddHtml(35, 25, 140, 20, Center("Rematch?"));

                AddButton(35, 55, 247, 248, 1);
                AddButton(115, 55, 242, 241, 2);
            }
            else
            {
                AddPage(1);

                var parts = context.Participants;

                var height = 25 + 20;

                for (var i = 0; i < parts.Count; ++i)
                {
                    var p = parts[i];

                    height += 4;

                    if (p.Players.Length > 1)
                    {
                        height += 22;
                    }

                    height += p.Players.Length * 22;
                }

                height += 10 + 22 + 25;

                AddBackground(0, 0, 260, height, 9250);
                AddBackground(10, 10, 240, height - 20, 0xDAC);

                AddHtml(35, 25, 190, 20, Center("Participants"));

                var y = 20 + 25;

                for (var i = 0; i < parts.Count; ++i)
                {
                    var p = parts[i];

                    y += 4;

                    var offset = 0;

                    if (p.Players.Length > 1)
                    {
                        AddHtml(35, y, 176, 20, $"Team #{i + 1}");
                        y += 22;
                        offset = 10;
                    }

                    for (var j = 0; j < p.Players.Length; ++j)
                    {
                        var pl = p.Players[j];

                        var name = pl == null ? "(Empty)" : pl.Mobile.Name;

                        AddHtml(35 + offset, y, 166, 20, name);

                        y += 22;
                    }
                }

                y += 8;

                AddHtml(35, y, 176, 20, "Continue?");

                y -= 2;

                AddButton(102, y, 247, 248, 0, GumpButtonType.Page, 2);
                AddButton(169, y, 242, 241, 2);

                AddPage(2);

                var ruleset = context.Ruleset;
                var basedef = ruleset.Base;

                height = 25 + 20 + 5 + 20 + 20 + 4;

                var changes = 0;

                BitArray defs;

                if (ruleset.Flavors.Count > 0)
                {
                    defs = new BitArray(basedef.Options);

                    for (var i = 0; i < ruleset.Flavors.Count; ++i)
                    {
                        defs.Or(ruleset.Flavors[i].Options);
                    }

                    height += ruleset.Flavors.Count * 18;
                }
                else
                {
                    defs = basedef.Options;
                }

                var opts = ruleset.Options;

                for (var i = 0; i < opts.Length; ++i)
                {
                    if (defs[i] != opts[i])
                    {
                        ++changes;
                    }
                }

                height += changes * 22;

                height += 10 + 22 + 25;

                AddBackground(0, 0, 260, height, 9250);
                AddBackground(10, 10, 240, height - 20, 0xDAC);

                AddHtml(35, 25, 190, 20, Center("Rules"));

                AddHtml(35, 50, 190, 20, $"Set: {basedef.Title}");

                y = 70;

                for (var i = 0; i < ruleset.Flavors.Count; ++i, y += 18)
                {
                    AddHtml(35, y, 190, 20, $" + {ruleset.Flavors[i].Title}");
                }

                y += 4;

                if (changes > 0)
                {
                    AddHtml(35, y, 190, 20, "Modifications:");
                    y += 20;

                    for (var i = 0; i < opts.Length; ++i)
                    {
                        if (defs[i] != opts[i])
                        {
                            var name = ruleset.Layout.FindByIndex(i);

                            if (name != null) // sanity
                            {
                                AddImage(35, y, opts[i] ? 0xD3 : 0xD2);
                                AddHtml(60, y, 165, 22, name);
                            }

                            y += 22;
                        }
                    }
                }
                else
                {
                    AddHtml(35, y, 190, 20, "Modifications: None");
                    y += 20;
                }

                y += 8;

                AddHtml(35, y, 176, 20, "Continue?");

                y -= 2;

                AddButton(102, y, 247, 248, 1);
                AddButton(169, y, 242, 241, 3);
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
            if (!m_Context.Registered || !m_Context.ReadyWait)
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // okay
                    {
                        if (m_From is not PlayerMobile pm)
                        {
                            break;
                        }

                        pm.DuelPlayer.Ready = true;
                        m_Context.SendReadyGump();

                        break;
                    }
                case 2: // reject participants
                    {
                        m_Context.RejectReady(m_From, "participants");
                        break;
                    }
                case 3: // reject rules
                    {
                        m_Context.RejectReady(m_From, "rules");
                        break;
                    }
            }
        }
    }
}
