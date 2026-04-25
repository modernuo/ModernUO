using System.Collections;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.ConPVP;

public class RulesetGump : DynamicGump
{
    private readonly DuelContext _duelContext;
    private readonly Mobile _from;
    private readonly RulesetLayout _page;
    private readonly bool _readOnly;
    private readonly Ruleset _ruleset;

    public override bool Singleton => true;

    private RulesetGump(Mobile from, Ruleset ruleset, RulesetLayout page, DuelContext duelContext, bool readOnly)
        : base(readOnly ? 310 : 50, 50)
    {
        _from = from;
        _ruleset = ruleset;
        _page = page;
        _duelContext = duelContext;
        _readOnly = readOnly;
    }

    public static void DisplayTo(Mobile from, Ruleset ruleset, RulesetLayout page, DuelContext duelContext, bool readOnly = false)
    {
        if (from?.NetState == null || ruleset == null || page == null)
        {
            return;
        }

        var gumps = from.GetGumps();
        gumps.Close<DuelContextGump>();
        gumps.Close<ParticipantGump>();

        from.SendGump(new RulesetGump(from, ruleset, page, duelContext, readOnly));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        if (_readOnly)
        {
            builder.SetNoMove();
        }

        var depthCounter = _page;

        while (depthCounter != null)
        {
            depthCounter = depthCounter.Parent;
        }

        var count = _page.Children.Length + _page.Options.Length;

        builder.AddPage();

        var height = 35 + 10 + 2 + count * 22 + 2 + 30;

        builder.AddBackground(0, 0, 260, height, 9250);
        builder.AddBackground(10, 10, 240, height - 20, 0xDAC);

        builder.AddHtml(35, 25, 190, 20, Center(_page.Title));

        var x = 35;
        var y = 47;

        for (var i = 0; i < _page.Children.Length; ++i)
        {
            AddGoldenButton(ref builder, x, y, 1 + i);
            builder.AddHtml(x + 25, y, 250, 22, _page.Children[i].Title);

            y += 22;
        }

        for (var i = 0; i < _page.Options.Length; ++i)
        {
            var enabled = _ruleset.Options[_page.Offset + i];

            if (_readOnly)
            {
                builder.AddImage(x, y, enabled ? 0xD3 : 0xD2);
            }
            else
            {
                builder.AddCheckbox(x, y, 0xD2, 0xD3, enabled, i);
            }

            builder.AddHtml(x + 25, y, 250, 22, _page.Options[i]);

            y += 22;
        }
    }

    private static string Center(string text) => $"<CENTER>{text}</CENTER>";

    private static void AddGoldenButton(ref DynamicGumpBuilder builder, int x, int y, int bid)
    {
        builder.AddButton(x, y, 0xD2, 0xD2, bid);
        builder.AddButton(x + 3, y + 3, 0xD8, 0xD8, bid);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_duelContext?.Registered == false)
        {
            return;
        }

        if (!_readOnly)
        {
            var opts = new BitArray(_page.Options.Length);

            for (var i = 0; i < info.Switches.Length; ++i)
            {
                var sid = info.Switches[i];

                if (sid >= 0 && sid < _page.Options.Length)
                {
                    opts[sid] = true;
                }
            }

            for (var i = 0; i < opts.Length; ++i)
            {
                if (_ruleset.Options[_page.Offset + i] != opts[i])
                {
                    _ruleset.Options[_page.Offset + i] = opts[i];
                    _ruleset.Changed = true;
                }
            }
        }

        var bid = info.ButtonID;

        if (bid == 0)
        {
            if (_page.Parent != null)
            {
                DisplayTo(_from, _ruleset, _page.Parent, _duelContext, _readOnly);
            }
            else if (!_readOnly)
            {
                PickRulesetGump.DisplayTo(_from, _duelContext, _ruleset);
            }
        }
        else
        {
            bid -= 1;

            if (bid >= 0 && bid < _page.Children.Length)
            {
                DisplayTo(_from, _ruleset, _page.Children[bid], _duelContext, _readOnly);
            }
        }
    }
}
