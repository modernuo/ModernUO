using System.Collections;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.ConPVP;

public class ReadyUpGump : DynamicGump
{
    private readonly DuelContext _context;
    private readonly Mobile _from;

    public override bool Singleton => true;

    private ReadyUpGump(Mobile from, DuelContext context) : base(50, 50)
    {
        _from = from;
        _context = context;
    }

    public static void DisplayTo(Mobile from, DuelContext context)
    {
        if (from?.NetState == null || context == null)
        {
            return;
        }

        from.SendGump(new ReadyUpGump(from, context), true);
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.SetNoClose();
        builder.AddPage();

        if (_context.Rematch)
        {
            var height = 25 + 20 + 10 + 22 + 25;

            builder.AddBackground(0, 0, 210, height, 9250);
            builder.AddBackground(10, 10, 190, height - 20, 0xDAC);

            builder.AddHtml(35, 25, 140, 20, Center("Rematch?"));

            builder.AddButton(35, 55, 247, 248, 1);
            builder.AddButton(115, 55, 242, 241, 2);
        }
        else
        {
            builder.AddPage(1);

            var parts = _context.Participants;

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

            builder.AddBackground(0, 0, 260, height, 9250);
            builder.AddBackground(10, 10, 240, height - 20, 0xDAC);

            builder.AddHtml(35, 25, 190, 20, Center("Participants"));

            var y = 20 + 25;

            for (var i = 0; i < parts.Count; ++i)
            {
                var p = parts[i];

                y += 4;

                var offset = 0;

                if (p.Players.Length > 1)
                {
                    builder.AddHtml(35, y, 176, 20, $"Team #{i + 1}");
                    y += 22;
                    offset = 10;
                }

                for (var j = 0; j < p.Players.Length; ++j)
                {
                    var pl = p.Players[j];

                    var name = pl == null ? "(Empty)" : pl.Mobile.Name;

                    builder.AddHtml(35 + offset, y, 166, 20, name);

                    y += 22;
                }
            }

            y += 8;

            builder.AddHtml(35, y, 176, 20, "Continue?");

            y -= 2;

            builder.AddButton(102, y, 247, 248, 0, GumpButtonType.Page, 2);
            builder.AddButton(169, y, 242, 241, 2);

            builder.AddPage(2);

            var ruleset = _context.Ruleset;
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

            builder.AddBackground(0, 0, 260, height, 9250);
            builder.AddBackground(10, 10, 240, height - 20, 0xDAC);

            builder.AddHtml(35, 25, 190, 20, Center("Rules"));

            builder.AddHtml(35, 50, 190, 20, $"Set: {basedef.Title}");

            y = 70;

            for (var i = 0; i < ruleset.Flavors.Count; ++i, y += 18)
            {
                builder.AddHtml(35, y, 190, 20, $" + {ruleset.Flavors[i].Title}");
            }

            y += 4;

            if (changes > 0)
            {
                builder.AddHtml(35, y, 190, 20, "Modifications:");
                y += 20;

                for (var i = 0; i < opts.Length; ++i)
                {
                    if (defs[i] != opts[i])
                    {
                        var name = ruleset.Layout.FindByIndex(i);

                        if (name != null) // sanity
                        {
                            builder.AddImage(35, y, opts[i] ? 0xD3 : 0xD2);
                            builder.AddHtml(60, y, 165, 22, name);
                        }

                        y += 22;
                    }
                }
            }
            else
            {
                builder.AddHtml(35, y, 190, 20, "Modifications: None");
                y += 20;
            }

            y += 8;

            builder.AddHtml(35, y, 176, 20, "Continue?");

            y -= 2;

            builder.AddButton(102, y, 247, 248, 1);
            builder.AddButton(169, y, 242, 241, 3);
        }
    }

    private static string Center(string text) => $"<CENTER>{text}</CENTER>";

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (!_context.Registered || !_context.ReadyWait)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1: // okay
                {
                    if (_from is not PlayerMobile pm)
                    {
                        break;
                    }

                    pm.DuelPlayer.Ready = true;
                    _context.SendReadyGump();

                    break;
                }
            case 2: // reject participants
                {
                    _context.RejectReady(_from, "participants");
                    break;
                }
            case 3: // reject rules
                {
                    _context.RejectReady(_from, "rules");
                    break;
                }
        }
    }
}
