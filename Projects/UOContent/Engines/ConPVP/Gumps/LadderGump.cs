using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.ConPVP;

[SerializationGenerator(0, false)]
public partial class LadderItem : Item
{
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [SerializableField(0)]
    private LadderController _ladder;

    [Constructible]
    public LadderItem() : base(0x117F) => Movable = false;

    public override string DefaultName => "1v1 leaderboard";

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), 2))
        {
            var ladder = ConPVP.Ladder.Instance ?? Ladder.Ladder;

            if (ladder != null)
            {
                LadderGump.DisplayTo(from, ladder);
            }
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that
        }
    }
}

public class LadderGump : DynamicGump
{
    private readonly Ladder _ladder;

    private List<LadderEntry> _list;
    private int _page;
    private int _columnX;

    public override bool Singleton => true;

    private LadderGump(Ladder ladder, int page) : base(50, 50)
    {
        _ladder = ladder;
        _page = page;
    }

    public static void DisplayTo(Mobile from, Ladder ladder, int page = 0)
    {
        if (from?.NetState == null || ladder == null)
        {
            return;
        }

        from.SendGump(new LadderGump(ladder, page), true);
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        _columnX = 12;

        builder.AddPage();

        _list = new List<LadderEntry>(_ladder.Entries);

        var lc = Math.Min(_list.Count, 150);

        var start = _page * 15;
        var end = start + 15;

        if (end > lc)
        {
            end = lc;
        }

        var ct = end - start;

        var height = 12 + 20 + ct * 20 + 23 + 12;

        builder.AddBackground(0, 0, 499, height, 0x2436);

        for (var i = start + 1; i < end; i += 2)
        {
            builder.AddImageTiled(12, 32 + (i - start) * 20, 475, 20, 0x2430);
        }

        builder.AddAlphaRegion(10, 10, 479, height - 20);

        if (_page > 0)
        {
            builder.AddButton(446, height - 12 - 2 - 16, 0x15E3, 0x15E7, 1);
        }
        else
        {
            builder.AddImage(446, height - 12 - 2 - 16, 0x2626);
        }

        if ((_page + 1) * 15 < lc)
        {
            builder.AddButton(466, height - 12 - 2 - 16, 0x15E1, 0x15E5, 2);
        }
        else
        {
            builder.AddImage(466, height - 12 - 2 - 16, 0x2622);
        }

        builder.AddHtml(
            16,
            height - 12 - 2 - 18,
            400,
            20,
            Html.Color($"Top {lc} of {_list.Count:N0} duelists, page {_page + 1} of {(lc + 14) / 15}", 0xFFC000)
        );

        AddColumnHeader(ref builder, 75, "Rank");
        AddColumnHeader(ref builder, 115, "Level");
        AddColumnHeader(ref builder, 50, "Guild");
        AddColumnHeader(ref builder, 115, "Name");
        AddColumnHeader(ref builder, 60, "Wins");
        AddColumnHeader(ref builder, 60, "Losses");

        for (var i = start; i < end && i < lc; ++i)
        {
            var entry = _list[i];

            var y = 32 + (i - start) * 20;
            var x = 12;

            AddBorderedText(ref builder, x, y, 75, Rank(i + 1).Center(), 0xFFFFFF, 0);
            x += 75;

            builder.AddImage(x + 3, y + 4, 0x805);

            var xp = entry.Experience;
            var level = Ladder.GetLevel(xp);

            Ladder.GetLevelInfo(level, out var xpBase, out var xpAdvance);

            int width;

            var xpOffset = xp - xpBase;

            if (xpOffset >= xpAdvance)
            {
                width = 109; // level 50
            }
            else
            {
                width = (109 * xpOffset + xpAdvance / 2) / (xpAdvance - 1);
            }

            builder.AddImageTiled(x + 3, y + 4, width, 11, 0x806);
            AddBorderedText(ref builder, x, y, 115, Html.Center($"{level}"), 0xFFFFFF, 0);
            x += 115;

            var mob = entry.Mobile;

            if (mob.Guild != null)
            {
                AddBorderedText(ref builder, x, y, 50, mob.Guild.Abbreviation.Center(), 0xFFFFFF, 0);
            }

            x += 50;

            AddBorderedText(ref builder, x + 5, y, 115 - 5, mob.Name, 0xFFFFFF, 0);
            x += 115;

            AddBorderedText(ref builder, x, y, 60, Html.Center($"{entry.Wins}"), 0xFFFFFF, 0);
            x += 60;

            AddBorderedText(ref builder, x, y, 60, Html.Center($"{entry.Losses}"), 0xFFFFFF, 0);
            x += 60;
        }
    }

    public static string Rank(int num)
    {
        var numStr = num.ToString("N0");

        if (num % 100 > 10 && num % 100 < 20)
        {
            return $"{numStr}th";
        }

        return (num % 10) switch
        {
            1 => $"{numStr}st",
            2 => $"{numStr}nd",
            3 => $"{numStr}rd",
            _ => $"{numStr}th"
        };
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (info.ButtonID == 1 && _page > 0)
        {
            _page--;
            from.SendGump(this); // refresh-via-this
        }
        else if (info.ButtonID == 2 && (_page + 1) * 15 < Math.Min(_list.Count, 150))
        {
            _page++;
            from.SendGump(this); // refresh-via-this
        }
    }

    private static void AddBorderedText(ref DynamicGumpBuilder builder, int x, int y, int width, string text, int color, int borderColor)
    {
        AddColoredText(ref builder, x, y, width, text, color);
    }

    private static void AddColoredText(ref DynamicGumpBuilder builder, int x, int y, int width, string text, int color)
    {
        builder.AddHtml(x, y, width, 20, color == 0 ? text : text.Color(color));
    }

    private void AddColumnHeader(ref DynamicGumpBuilder builder, int width, string name)
    {
        builder.AddBackground(_columnX, 12, width, 20, 0x242C);
        builder.AddImageTiled(_columnX + 2, 14, width - 4, 16, 0x2430);
        AddBorderedText(ref builder, _columnX, 13, width, name.Center(), 0xFFFFFF, 0);

        _columnX += width;
    }
}
