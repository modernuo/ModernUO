using Server.Gumps;
using Server.Network;

namespace Server.Engines.ConPVP;

public class PickRulesetGump : DynamicGump
{
    private readonly DuelContext _context;
    private readonly Ruleset[] _defaults;
    private readonly Ruleset[] _flavors;
    private readonly Mobile _from;
    private readonly Ruleset _ruleset;

    public override bool Singleton => true;

    private PickRulesetGump(Mobile from, DuelContext context, Ruleset ruleset) : base(50, 50)
    {
        _from = from;
        _context = context;
        _ruleset = ruleset;
        _defaults = ruleset.Layout.Defaults;
        _flavors = ruleset.Layout.Flavors;
    }

    public static void DisplayTo(Mobile from, DuelContext context, Ruleset ruleset)
    {
        if (from?.NetState == null || ruleset == null)
        {
            return;
        }

        from.SendGump(new PickRulesetGump(from, context, ruleset));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var height = 25 + 20 + (_defaults.Length + 1) * 22 + 6 + 20 + _flavors.Length * 22 + 25;

        builder.AddPage();

        builder.AddBackground(0, 0, 260, height, 9250);
        builder.AddBackground(10, 10, 240, height - 20, 0xDAC);

        builder.AddHtml(35, 25, 190, 20, Center("Rules"));

        var y = 25 + 20;

        for (var i = 0; i < _defaults.Length; ++i)
        {
            var cur = _defaults[i];

            builder.AddHtml(35 + 14, y, 176, 20, cur.Title);

            if (_ruleset.Base == cur && !_ruleset.Changed)
            {
                builder.AddImage(35, y + 4, 0x939);
            }
            else if (_ruleset.Base == cur)
            {
                builder.AddButton(35, y + 4, 0x93A, 0x939, 2 + i);
            }
            else
            {
                builder.AddButton(35, y + 4, 0x938, 0x939, 2 + i);
            }

            y += 22;
        }

        builder.AddHtml(35 + 14, y, 176, 20, "Custom");
        builder.AddButton(35, y + 4, _ruleset.Changed ? 0x939 : 0x938, 0x939, 1);

        y += 22;
        y += 6;

        builder.AddHtml(35, y, 190, 20, Center("Flavors"));
        y += 20;

        for (var i = 0; i < _flavors.Length; ++i)
        {
            var cur = _flavors[i];

            builder.AddHtml(35 + 14, y, 176, 20, cur.Title);

            if (_ruleset.Flavors.Contains(cur))
            {
                builder.AddButton(35, y + 4, 0x939, 0x938, 2 + _defaults.Length + i);
            }
            else
            {
                builder.AddButton(35, y + 4, 0x938, 0x939, 2 + _defaults.Length + i);
            }

            y += 22;
        }
    }

    private static string Center(string text) => $"<CENTER>{text}</CENTER>";

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_context?.Registered == false)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 0: // closed
                {
                    if (_context != null)
                    {
                        DuelContextGump.DisplayTo(_from, _context);
                    }

                    break;
                }
            case 1: // customize
                {
                    RulesetGump.DisplayTo(_from, _ruleset, _ruleset.Layout, _context);
                    break;
                }
            default:
                {
                    var idx = info.ButtonID - 2;

                    if (idx >= 0 && idx < _defaults.Length)
                    {
                        _ruleset.ApplyDefault(_defaults[idx]);
                        _from.SendGump(this); // refresh-via-this
                    }
                    else
                    {
                        idx -= _defaults.Length;

                        if (idx >= 0 && idx < _flavors.Length)
                        {
                            if (_ruleset.Flavors.Contains(_flavors[idx]))
                            {
                                _ruleset.RemoveFlavor(_flavors[idx]);
                            }
                            else
                            {
                                _ruleset.AddFlavor(_flavors[idx]);
                            }

                            _from.SendGump(this); // refresh-via-this
                        }
                    }

                    break;
                }
        }
    }
}
