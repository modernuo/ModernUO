using System.Collections.Generic;
using System.Text;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Commands
{
    public static class HelpInfo
    {
        public static Dictionary<string, CommandInfo> HelpInfos { get; } = new();

        public static List<CommandInfo> SortedHelpInfo { get; private set; } = new();

        public static void Initialize()
        {
            CommandSystem.Register("HelpInfo", AccessLevel.Player, HelpInfo_OnCommand);

            FillTable();
        }

        [Usage("HelpInfo [<command>]"), Description(
             "Gives information on a specified command, or when no argument specified, displays a gump containing all commands"
         )]
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

                var attrs = mi.GetCustomAttributes(typeof(UsageAttribute), false);

                if (attrs.Length == 0)
                {
                    continue;
                }

                var usage = attrs[0] as UsageAttribute;

                attrs = mi.GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs.Length == 0)
                {
                    continue;
                }

                if (usage == null || !(attrs[0] is DescriptionAttribute desc))
                {
                    continue;
                }

                attrs = mi.GetCustomAttributes(typeof(AliasesAttribute), false);

                var aliases = attrs.Length == 0 ? null : attrs[0] as AliasesAttribute;

                var descString = desc.Description
                    .ReplaceOrdinal("<", "(")
                    .ReplaceOrdinal(">", ")");

                if (aliases == null)
                {
                    list.Add(new CommandInfo(e.AccessLevel, e.Command, null, usage.Usage, descString));
                }
                else
                {
                    list.Add(new CommandInfo(e.AccessLevel, e.Command, aliases.Aliases, usage.Usage, descString));

                    for (var j = 0; j < aliases.Aliases.Length; j++)
                    {
                        var newAliases = new string[aliases.Aliases.Length];

                        aliases.Aliases.CopyTo(newAliases, 0);

                        newAliases[j] = e.Command;

                        list.Add(new CommandInfo(e.AccessLevel, aliases.Aliases[j], newAliases, usage.Usage, descString));
                    }
                }
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
                    .ReplaceOrdinal(">", ")");

                if (command.Supports != CommandSupport.Single)
                {
                    var sb = new StringBuilder(50 + desc.Length);

                    sb.Append("Modifiers: ");

                    if ((command.Supports & CommandSupport.Global) != 0)
                    {
                        sb.Append("<i>Global</i>, ");
                    }

                    if ((command.Supports & CommandSupport.Online) != 0)
                    {
                        sb.Append("<i>Online</i>, ");
                    }

                    if ((command.Supports & CommandSupport.Region) != 0)
                    {
                        sb.Append("<i>Region</i>, ");
                    }

                    if ((command.Supports & CommandSupport.Contained) != 0)
                    {
                        sb.Append("<i>Contained</i>, ");
                    }

                    if ((command.Supports & CommandSupport.Multi) != 0)
                    {
                        sb.Append("<i>Multi</i>, ");
                    }

                    if ((command.Supports & CommandSupport.Area) != 0)
                    {
                        sb.Append("<i>Area</i>, ");
                    }

                    if ((command.Supports & CommandSupport.Self) != 0)
                    {
                        sb.Append("<i>Self</i>, ");
                    }

                    sb.Remove(sb.Length - 2, 2);
                    sb.Append("<br>");
                    sb.Append(desc);

                    desc = sb.ToString();
                }

                list.Add(new CommandInfo(command.AccessLevel, cmd, aliases, usage, desc));

                for (var j = 0; j < aliases.Length; j++)
                {
                    var newAliases = new string[aliases.Length];

                    aliases.CopyTo(newAliases, 0);

                    newAliases[j] = cmd;

                    list.Add(new CommandInfo(command.AccessLevel, aliases[j], newAliases, usage, desc));
                }
            }

            var commandImpls = BaseCommandImplementor.Implementors;

            for (var i = 0; i < commandImpls.Count; ++i)
            {
                var command = commandImpls[i];

                var usage = command.Usage;
                var desc = command.Description;

                if (usage == null || desc == null)
                {
                    continue;
                }

                var cmds = command.Accessors;
                var cmd = cmds[0];
                var aliases = new string[cmds.Length - 1];

                for (var j = 0; j < aliases.Length; ++j)
                {
                    aliases[j] = cmds[j + 1];
                }

                desc = desc
                    .ReplaceOrdinal("<", ")")
                    .ReplaceOrdinal(">", ")");

                list.Add(new CommandInfo(command.AccessLevel, cmd, aliases, usage, desc));

                for (var j = 0; j < aliases.Length; j++)
                {
                    var newAliases = new string[aliases.Length];

                    aliases.CopyTo(newAliases, 0);

                    newAliases[j] = cmd;

                    list.Add(new CommandInfo(command.AccessLevel, aliases[j], newAliases, usage, desc));
                }
            }

            list.Sort(new CommandInfoSorter());

            SortedHelpInfo = list;

            foreach (var c in SortedHelpInfo)
            {
                HelpInfos.TryAdd(c.Name.ToLower(), c);
            }
        }

        public class CommandListGump : BaseGridGump
        {
            private const int EntriesPerPage = 15;
            private readonly List<CommandInfo> m_List;

            private readonly int m_Page;

            public CommandListGump(int page, Mobile from, List<CommandInfo> list)
                : base(30, 30)
            {
                m_Page = page;

                if (list == null)
                {
                    m_List = new List<CommandInfo>();

                    foreach (var c in SortedHelpInfo)
                    {
                        if (from.AccessLevel >= c.AccessLevel)
                        {
                            m_List.Add(c);
                        }
                    }
                }
                else
                {
                    m_List = list;
                }

                AddNewPage();

                if (m_Page > 0)
                {
                    AddEntryButton(20, ArrowLeftID1, ArrowLeftID2, 1, ArrowLeftWidth, ArrowLeftHeight);
                }
                else
                {
                    AddEntryHeader(20);
                }

                AddEntryHtml(
                    160,
                    Center(
                        $"Page {m_Page + 1} of {(m_List.Count + EntriesPerPage - 1) / EntriesPerPage}"
                    )
                );

                if ((m_Page + 1) * EntriesPerPage < m_List.Count)
                {
                    AddEntryButton(20, ArrowRightID1, ArrowRightID2, 2, ArrowRightWidth, ArrowRightHeight);
                }
                else
                {
                    AddEntryHeader(20);
                }

                var last = (int)AccessLevel.Player - 1;

                for (int i = m_Page * EntriesPerPage, line = 0; line < EntriesPerPage && i < m_List.Count; ++i, ++line)
                {
                    var c = m_List[i];
                    if (from.AccessLevel >= c.AccessLevel)
                    {
                        if ((int)c.AccessLevel != last)
                        {
                            AddNewLine();
                            AddEntryHtml(20 + OffsetSize + 160, Color(c.AccessLevel.ToString(), 0xFF0000));
                            AddEntryHeader(20);
                            line++;
                        }

                        last = (int)c.AccessLevel;

                        AddNewLine();
                        AddEntryHtml(20 + OffsetSize + 160, c.Name);
                        AddEntryButton(20, ArrowRightID1, ArrowRightID2, 3 + i, ArrowRightWidth, ArrowRightHeight);
                    }
                }

                FinishPage();
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                var m = sender.Mobile;
                switch (info.ButtonID)
                {
                    case 0:
                        {
                            m.CloseGump<CommandInfoGump>();
                            break;
                        }
                    case 1:
                        {
                            if (m_Page > 0)
                            {
                                m.SendGump(new CommandListGump(m_Page - 1, m, m_List));
                            }

                            break;
                        }
                    case 2:
                        {
                            if ((m_Page + 1) * EntriesPerPage < SortedHelpInfo.Count)
                            {
                                m.SendGump(new CommandListGump(m_Page + 1, m, m_List));
                            }

                            break;
                        }
                    default:
                        {
                            var v = info.ButtonID - 3;

                            if (v >= 0 && v < m_List.Count)
                            {
                                var c = m_List[v];

                                if (m.AccessLevel >= c.AccessLevel)
                                {
                                    m.SendGump(new CommandInfoGump(c));
                                    m.SendGump(new CommandListGump(m_Page, m, m_List));
                                }
                                else
                                {
                                    m.SendMessage("You no longer have access to that command.");
                                    m.SendGump(new CommandListGump(m_Page, m, null));
                                }
                            }

                            break;
                        }
                }
            }
        }

        public class CommandInfoGump : Gump
        {
            public CommandInfoGump(CommandInfo info, int width = 320, int height = 200)
                : base(300, 50)
            {
                AddPage(0);

                AddBackground(0, 0, width, height, 5054);

                // AddImageTiled( 10, 10, width - 20, 20, 2624 );
                // AddAlphaRegion( 10, 10, width - 20, 20 );
                // AddHtmlLocalized( 10, 10, width - 20, 20, header, headerColor, false, false );
                AddHtml(10, 10, width - 20, 20, Color(Center(info.Name), 0xFF0000));

                // AddImageTiled( 10, 40, width - 20, height - 80, 2624 );
                // AddAlphaRegion( 10, 40, width - 20, height - 80 );

                var sb = new StringBuilder();

                sb.Append("Usage: ");
                var usage = info.Usage
                    .ReplaceOrdinal("<", "(")
                    .ReplaceOrdinal(">", ")");
                sb.Append(usage);
                sb.Append("<BR>");

                var aliases = info.Aliases;

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
                sb.Append(info.AccessLevel.ToString());
                sb.Append("<BR>");
                sb.Append("<BR>");

                sb.Append(info.Description);

                AddHtml(10, 40, width - 20, height - 80, sb.ToString(), false, true);

                // AddImageTiled( 10, height - 30, width - 20, 20, 2624 );
                // AddAlphaRegion( 10, height - 30, width - 20, 20 );
            }

            public string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

            public string Center(string text) => $"<CENTER>{text}</CENTER>";
        }
    }
}
