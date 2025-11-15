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

    public class CommandListGump : BaseGridGump
    {
        private const int EntriesPerPage = 15;
        private readonly List<CommandInfo> _list;

        private readonly int _page;

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

            AddNewPage();

            if (_page > 0)
            {
                AddEntryButton(20, ArrowLeftID1, ArrowLeftID2, 1, ArrowLeftWidth, ArrowLeftHeight);
            }
            else
            {
                AddEntryHeader(20);
            }

            AddEntryHtml(
                320,
                Center(
                    $"Page {_page + 1} of {(_list.Count + EntriesPerPage - 1) / EntriesPerPage}"
                )
            );

            if ((_page + 1) * EntriesPerPage < _list.Count)
            {
                AddEntryButton(20, ArrowRightID1, ArrowRightID2, 2, ArrowRightWidth, ArrowRightHeight);
            }
            else
            {
                AddEntryHeader(20);
            }

            var last = (int)AccessLevel.Player - 1;

            for (int i = _page * EntriesPerPage, line = 0; line < EntriesPerPage && i < _list.Count; ++i, ++line)
            {
                var c = _list[i];
                if (from.AccessLevel >= c.AccessLevel)
                {
                    if ((int)c.AccessLevel != last)
                    {
                        AddNewLine();
                        AddEntryHtml(20 + OffsetSize + 320, Color(c.AccessLevel.ToString(), 0xFF0000));
                        AddEntryHeader(20);
                        line++;
                    }

                    last = (int)c.AccessLevel;

                    AddNewLine();
                    string name;
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
                        name = sb.ToString();
                    }
                    else
                    {
                        name = c.Name;
                    }

                    AddEntryHtml(20 + OffsetSize + 320, name);
                    AddEntryButton(20, ArrowRightID1, ArrowRightID2, 3 + i, ArrowRightWidth, ArrowRightHeight);
                }
            }

            FinishPage();
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
                            gumps.Send(new CommandListGump(_page - 1, m, _list));
                        }

                        break;
                    }
                case 2:
                    {
                        if ((_page + 1) * EntriesPerPage < SortedHelpInfo.Count)
                        {
                            gumps.Send(new CommandListGump(_page + 1, m, _list));
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
                                gumps.Send(new CommandListGump(_page, m, _list));
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
