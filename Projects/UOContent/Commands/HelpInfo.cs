using System;
using System.Collections.Generic;
using System.Reflection;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Network;
using Server.Text;

namespace Server.Commands;

public static class HelpInfo
{
    private static List<CommandInfo> _sortedHelpInfo;
    private static Dictionary<string, CommandInfo> _helpInfos;

    public static Dictionary<string, CommandInfo> HelpInfos
    {
        get
        {
            if (_helpInfos == null)
            {
                FillTable();
            }

            return _helpInfos;
        }
    }

    public static List<CommandInfo> SortedHelpInfo
    {
        get
        {
            if (_sortedHelpInfo == null)
            {
                FillTable();
            }

            return _sortedHelpInfo;
        }
    }

    public static void Configure()
    {
        CommandSystem.Register("HelpInfo", AccessLevel.Player, HelpInfo_OnCommand);
    }

    public static void Initialize()
    {
        FillTable();
    }

    [Usage("HelpInfo [<command>]")]
    [Description("Gives information on a specified command, or when no argument specified, displays a gump containing all commands")]
    private static void HelpInfo_OnCommand(CommandEventArgs e)
    {
        if (e.Length > 0)
        {
            var arg = e.GetString(0).ToLower();
            if (HelpInfos.TryGetValue(arg, out var c))
            {
                var m = e.Mobile;

                if (m.AccessLevel >= c.AccessLevel)
                {
                    m.SendGump(new CommandInfoGump(c));
                }
                else
                {
                    m.SendMessage("You don't have access to that command.");
                }

                return;
            }

            e.Mobile.SendMessage($"Command '{arg}' not found!");
        }

        e.Mobile.SendGump(new CommandListGump(0, e.Mobile, null));
    }

    public static void FillTable()
    {
        var list = new List<CommandInfo>();
        var commands = CommandEntry.GetList();

        for (var i = 0; i < commands.Count; ++i)
        {
            var e = commands[i];

            var mi = e.Handler.Method;

            var usageAttr = mi.GetCustomAttribute(typeof(UsageAttribute), false) as UsageAttribute;
            var usage = usageAttr?.Usage;

            var descAttr = mi.GetCustomAttribute(typeof(DescriptionAttribute), false) as DescriptionAttribute;
            var desc = descAttr?.Description
                .ReplaceOrdinal("<", "(")
                .ReplaceOrdinal(">", ")");

            if (e.Handler.Target != null && usage == null && desc == null)
            {
                continue;
            }

            var aliasesAttr = mi.GetCustomAttribute(typeof(AliasesAttribute), false) as AliasesAttribute;
            var aliases = aliasesAttr?.Aliases;

            list.Add(new CommandInfo(e.AccessLevel, e.Command, aliases, usage ?? e.Command, null, desc ?? "No description available."));
        }

        for (var i = 0; i < TargetCommands.AllCommands.Count; ++i)
        {
            var command = TargetCommands.AllCommands[i];

            var usage = command.Usage;
            var desc = command.Description;

            if (usage == null || desc == null)
            {
                continue;
            }

            var cmds = command.Commands;
            var cmd = cmds[0];
            var aliases = new string[cmds.Length - 1];

            for (var j = 0; j < aliases.Length; ++j)
            {
                aliases[j] = cmds[j + 1];
            }

            desc = desc
                .ReplaceOrdinal("<", "(")
                .ReplaceOrdinal(">", ")") ?? "No description available.";

            List<string> modsList = [];

            foreach (var baseImpl in BaseCommandImplementor.Implementors)
            {
                if ((baseImpl.SupportRequirement & command.Supports) != 0)
                {
                    modsList.Add(baseImpl.Accessors[0]);
                }
            }

            var modifiers = modsList.ToArray();

            list.Add(new CommandInfo(command.AccessLevel, cmd, aliases, usage, modifiers, desc));
        }

        var commandImpls = BaseCommandImplementor.Implementors;

        for (var i = 0; i < commandImpls.Count; ++i)
        {
            var command = commandImpls[i];

            var cmds = command.Accessors;
            var cmd = cmds[0];

            var desc = command.Description ?? "No description available.";
            var usage = command.Usage ?? cmd;
            var aliases = new string[cmds.Length - 1];

            for (var j = 0; j < aliases.Length; ++j)
            {
                aliases[j] = cmds[j + 1];
            }

            desc = desc
                .ReplaceOrdinal("<", ")")
                .ReplaceOrdinal(">", ")");

            list.Add(new CommandInfo(command.AccessLevel, cmd, aliases, usage, null, desc));
        }

        list.Sort(new CommandInfoSorter());

        _sortedHelpInfo = list;
        _helpInfos = [];

        foreach (var c in _sortedHelpInfo)
        {
            _helpInfos.TryAdd(c.Name.ToLower(), c);
        }
    }

    public class CommandListGump : DynamicGump
    {
        private const int EntriesPerPage = 15;

        // Layout constants matching BaseGridGump defaults
        private const int ContentWidth = 360;  // 20 + 320 + 20
        private const int MaxRows = 16;        // 1 header + 15 entries
        private const int Col0Width = 20;      // Button column
        private const int Col1Width = 320;     // Content column
        private const int Col2Width = 20;      // Button column

        private static readonly GridEntryStyle Style = GridEntryStyle.Default;

        private readonly List<CommandInfo> _list;
        private int _page;

        public CommandListGump(int page, Mobile from, List<CommandInfo> list) : base(30, 30)
        {
            _page = page;

            if (list == null)
            {
                _list = [];

                foreach (var c in SortedHelpInfo)
                {
                    if (from.AccessLevel >= c.AccessLevel)
                    {
                        _list.Add(c);
                    }
                }
            }
            else
            {
                _list = list;
            }
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            var totalWidth = Style.GetTotalWidth(ContentWidth);
            var totalHeight = Style.GetTotalHeight(MaxRows);

            // Calculate column positions (matching cursor behavior with OffsetSize gaps)
            var originX = Style.ContentOriginX;
            var originY = Style.ContentOriginY;
            Span<int> colPos = stackalloc int[3];
            Span<int> colWidths = stackalloc int[3];
            colPos[0] = originX;
            colWidths[0] = Col0Width;
            colPos[1] = originX + Col0Width + Style.OffsetSize;
            colWidths[1] = Col1Width;
            colPos[2] = colPos[1] + Col1Width + Style.OffsetSize;
            colWidths[2] = Col2Width;

            // Background
            builder.AddGridBackground(totalWidth, totalHeight, Style);

            var rowY = originY;
            var totalPages = (_list.Count + EntriesPerPage - 1) / EntriesPerPage;

            // Header row
            var headerCell0 = new GridCell(colPos[0], rowY, colWidths[0], Style.EntryHeight);
            var headerCell1 = new GridCell(colPos[1], rowY, colWidths[1], Style.EntryHeight);
            var headerCell2 = new GridCell(colPos[2], rowY, colWidths[2], Style.EntryHeight);

            if (_page > 0)
            {
                builder.AddEntryArrowLeft(headerCell0, Style, 1);
            }
            else
            {
                builder.AddEntryHeader(headerCell0, Style);
            }

            builder.AddImageTiled(headerCell1, Style.EntryGumpID);
            builder.AddHtml(headerCell1, $"Page {_page + 1} of {totalPages}", Style.TextOffsetX, 0, align: TextAlignment.Center);

            if ((_page + 1) * EntriesPerPage < _list.Count)
            {
                builder.AddEntryArrowRight(headerCell2, Style, 2);
            }
            else
            {
                builder.AddEntryHeader(headerCell2, Style);
            }

            // Data rows
            var last = (int)AccessLevel.Player - 1;
            var dataContentWidth = Col0Width + Style.OffsetSize + Col1Width; // 341

            for (int i = _page * EntriesPerPage, line = 0; line < EntriesPerPage && i < _list.Count; ++i, ++line)
            {
                var c = _list[i];

                // Access level separator
                if ((int)c.AccessLevel != last)
                {
                    rowY += Style.EntryHeight + Style.OffsetSize;
                    var sepContentCell = new GridCell(colPos[0], rowY, dataContentWidth, Style.EntryHeight);
                    var sepButtonCell = new GridCell(colPos[2], rowY, colWidths[2], Style.EntryHeight);

                    builder.AddImageTiled(sepContentCell, Style.EntryGumpID);
                    builder.AddHtml(sepContentCell, $"{c.AccessLevel}", Style.TextOffsetX, 0, GumpTextColors.BrightRed);
                    builder.AddEntryHeader(sepButtonCell, Style);
                    line++;
                }

                last = (int)c.AccessLevel;

                // Entry row
                rowY += Style.EntryHeight + Style.OffsetSize;
                var contentCell = new GridCell(colPos[0], rowY, dataContentWidth, Style.EntryHeight);
                var buttonCell = new GridCell(colPos[2], rowY, colWidths[2], Style.EntryHeight);

                builder.AddImageTiled(contentCell, Style.EntryGumpID);

                if (c.Aliases?.Length > 0)
                {
                    using var sb = ValueStringBuilder.Create();
                    sb.Append($"{c.Name} <i>(");
                    for (var j = 0; j < c.Aliases.Length; ++j)
                    {
                        if (j != 0)
                        {
                            sb.Append(", ");
                        }
                        sb.Append(c.Aliases[j]);
                    }
                    sb.Append(")</i>");
                    builder.AddHtml(contentCell, sb.RawChars[..sb.Length], Style.TextOffsetX, 0);
                }
                else
                {
                    builder.AddHtml(contentCell, c.Name, Style.TextOffsetX, 0);
                }

                builder.AddEntryArrowRight(buttonCell, Style, 3 + i);
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var m = sender.Mobile;
            var gumps = m.GetGumps();

            switch (info.ButtonID)
            {
                case 0:
                {
                    gumps.Close<CommandInfoGump>();
                    break;
                }
                case 1:
                {
                    if (_page > 0)
                    {
                        _page--;
                        m.SendGump(this);
                    }
                    break;
                }
                case 2:
                {
                    if ((_page + 1) * EntriesPerPage < _list.Count)
                    {
                        _page++;
                        m.SendGump(this);
                    }
                    break;
                }
                default:
                {
                    var v = info.ButtonID - 3;

                    if (v >= 0 && v < _list.Count)
                    {
                        var c = _list[v];

                        if (m.AccessLevel >= c.AccessLevel)
                        {
                            gumps.Send(new CommandInfoGump(c));
                            m.SendGump(this);
                        }
                        else
                        {
                            m.SendMessage("You no longer have access to that command.");
                            gumps.Send(new CommandListGump(_page, m, null));
                        }
                    }
                    break;
                }
            }
        }
    }

    public class CommandInfoGump : DynamicGump
    {
        private readonly int _width;
        private readonly int _height;
        private readonly CommandInfo _info;

        public CommandInfoGump(CommandInfo info, int width = 320, int height = 200) : base(300, 50)
        {
            _width = width;
            _height = height;
            _info = info;
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(0, 0, _width, _height, 5054);
            builder.AddHtml(10, 10, _width - 20, 20, _info.Name, color: "#FF0000", align: TextAlignment.Center);

            using var sb = ValueStringBuilder.Create();

            sb.Append("Usage: ");
            var usage = _info.Usage
                .ReplaceOrdinal("<", "(")
                .ReplaceOrdinal(">", ")");
            sb.Append(usage);
            sb.Append("<BR>");

            var aliases = _info.Aliases;

            if (aliases?.Length > 0)
            {
                sb.Append($"Alias{(aliases.Length == 1 ? "" : "es")}: ");

                for (var i = 0; i < aliases.Length; ++i)
                {
                    if (i != 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(aliases[i]);
                }

                sb.Append("<BR>");
            }

            sb.Append("AccessLevel: ");
            sb.Append(_info.AccessLevel.ToString());
            sb.Append("<BR>");

            if (_info.Modifiers?.Length > 0)
            {
                sb.Append("Modifiers: ");
                for (var i = 0; i < _info.Modifiers.Length; ++i)
                {
                    if (i != 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(_info.Modifiers[i]);
                }
                sb.Append("<BR>");
                sb.Append("<BR>");
            }
            sb.Append(_info.Description);

            builder.AddHtml(10, 40, _width - 20, _height - 80, sb.AsSpan(true), background: false, scrollbar: true);
        }
    }
}
