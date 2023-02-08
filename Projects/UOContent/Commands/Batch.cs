using System;
using System.Collections.Generic;
using System.Reflection;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Commands
{
    public class Batch : BaseCommand
    {
        public Batch()
        {
            Commands = new[] { "Batch" };
            ListOptimized = true;
        }

        public BaseCommandImplementor Scope { get; set; }

        public string Condition { get; set; } = "";

        public List<BatchCommand> BatchCommands { get; } = new();

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            if (list.Count == 0)
            {
                LogFailure("Nothing was found to use this command on.");
                return;
            }

            try
            {
                var commands = new BaseCommand[BatchCommands.Count];
                var eventArgs = new CommandEventArgs[BatchCommands.Count];

                for (var i = 0; i < BatchCommands.Count; ++i)
                {
                    var bc = BatchCommands[i];

                    bc.GetDetails(out var commandString, out var argString, out var args);

                    var command = Scope.Commands[commandString];

                    commands[i] = command;
                    eventArgs[i] = new CommandEventArgs(e.Mobile, commandString, argString, args);

                    if (command == null)
                    {
                        e.Mobile.SendMessage(
                            $"That is either an invalid command name or one that does not support this modifier: {commandString}."                            
                        );
                        return;
                    }

                    if (e.Mobile.AccessLevel < command.AccessLevel)
                    {
                        e.Mobile.SendMessage($"You do not have access to that command: {commandString}.");
                        return;
                    }

                    if (!command.ValidateArgs(Scope, eventArgs[i]))
                    {
                        return;
                    }
                }

                for (var i = 0; i < commands.Length; ++i)
                {
                    var command = commands[i];
                    var bc = BatchCommands[i];

                    if (list.Count > 20)
                    {
                        CommandLogging.Enabled = false;
                    }

                    var propertyChains = new Dictionary<Type, PropertyInfo[]>();
                    var usedList = new List<object>(list.Count);

                    for (var j = 0; j < list.Count; ++j)
                    {
                        var obj = list[j];

                        if (obj == null)
                        {
                            continue;
                        }

                        var type = obj.GetType();
                        var failReason = "";

                        if (!propertyChains.TryGetValue(type, out var chain))
                        {
                            propertyChains[type] = chain = Properties.GetPropertyInfoChain(
                                e.Mobile,
                                type,
                                bc.Object,
                                PropertyAccess.Read,
                                out failReason
                            );
                        }

                        if (chain == null)
                        {
                            continue;
                        }

                        var endProp = Properties.GetPropertyInfo(ref obj, chain, out failReason);

                        if (endProp == null)
                        {
                            continue;
                        }

                        try
                        {
                            obj = endProp.GetValue(obj, null);

                            if (obj != null)
                            {
                                usedList.Add(obj);
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    command.ExecuteList(eventArgs[i], usedList);

                    if (list.Count > 20)
                    {
                        CommandLogging.Enabled = true;
                    }

                    command.Flush(e.Mobile, list.Count > 20);
                }
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage(ex.Message);
            }
        }

        public bool Run(Mobile from)
        {
            if (Scope == null)
            {
                from.SendMessage("You must select the batch command scope.");
                return false;
            }

            if (Condition.Length > 0 && !Scope.SupportsConditionals)
            {
                from.SendMessage("This command scope does not support conditionals.");
                return false;
            }

            if (Condition.Length > 0 && !Utility.InsensitiveStartsWith(Condition, "where"))
            {
                from.SendMessage("The condition field must start with \"where\".");
                return false;
            }

            var args = CommandSystem.Split(Condition);

            Scope.Process(from, this, args);

            return true;
        }

        public static void Initialize()
        {
            CommandSystem.Register("Batch", AccessLevel.Counselor, Batch_OnCommand);
        }

        [Usage("Batch")]
        [Description("Allows multiple commands to be run at the same time.")]
        public static void Batch_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendGump(new BatchGump(e.Mobile, new Batch()));
        }
    }

    public class BatchCommand
    {
        public BatchCommand(string command, string obj)
        {
            Command = command;
            Object = obj;
        }

        public string Command { get; set; }

        public string Object { get; set; }

        public void GetDetails(out string command, out string argString, out string[] args)
        {
            var indexOf = Command.IndexOfOrdinal(' ');

            if (indexOf >= 0)
            {
                argString = Command[(indexOf + 1)..];

                command = Command[..indexOf];
                args = CommandSystem.Split(argString);
            }
            else
            {
                argString = "";
                command = Command.ToLower();
                args = Array.Empty<string>();
            }
        }
    }

    public class BatchGump : BaseGridGump
    {
        private readonly Batch m_Batch;
        private readonly Mobile m_From;

        public BatchGump(Mobile from, Batch batch) : base(30, 30)
        {
            m_From = from;
            m_Batch = batch;

            Render();
        }

        public void Render()
        {
            AddNewPage();

            /* Header */
            AddEntryHeader(20);
            AddEntryHtml(180, Center("Batch Commands"));
            AddEntryHeader(20);
            AddNewLine();

            AddEntryHeader(9);
            AddEntryLabel(191, "Run Batch");
            AddEntryButton(20, ArrowRightID1, ArrowRightID2, GetButtonID(1, 0, 0), ArrowRightWidth, ArrowRightHeight);
            AddNewLine();

            AddBlankLine();

            /* Scope */
            AddEntryHeader(20);
            AddEntryHtml(180, Center("Scope"));
            AddEntryHeader(20);
            AddNewLine();

            AddEntryHeader(9);
            AddEntryLabel(191, m_Batch.Scope == null ? "Select Scope" : m_Batch.Scope.Accessors[0]);
            AddEntryButton(20, ArrowRightID1, ArrowRightID2, GetButtonID(1, 0, 1), ArrowRightWidth, ArrowRightHeight);
            AddNewLine();

            AddBlankLine();

            /* Condition */
            AddEntryHeader(20);
            AddEntryHtml(180, Center("Condition"));
            AddEntryHeader(20);
            AddNewLine();

            AddEntryHeader(9);
            AddEntryText(202, 0, m_Batch.Condition);
            AddEntryHeader(9);
            AddNewLine();

            AddBlankLine();

            /* Commands */
            AddEntryHeader(20);
            AddEntryHtml(180, Center("Commands"));
            AddEntryHeader(20);

            for (var i = 0; i < m_Batch.BatchCommands.Count; ++i)
            {
                var bc = m_Batch.BatchCommands[i];

                AddNewLine();

                AddImageTiled(CurrentX, CurrentY, 9, 2, 0x24A8);
                AddImageTiled(CurrentX, CurrentY + 2, 2, EntryHeight + OffsetSize + EntryHeight - 4, 0x24A8);
                AddImageTiled(CurrentX, CurrentY + EntryHeight + OffsetSize + EntryHeight - 2, 9, 2, 0x24A8);
                AddImageTiled(CurrentX + 3, CurrentY + 3, 6, EntryHeight + EntryHeight - 4 - OffsetSize, HeaderGumpID);

                IncreaseX(9);
                AddEntryText(202, 1 + i * 2, bc.Command);
                AddEntryHeader(9, 2);

                AddNewLine();

                IncreaseX(9);
                AddEntryText(202, 2 + i * 2, bc.Object);
            }

            AddNewLine();

            AddEntryHeader(9);
            AddEntryLabel(191, "Add New Command");
            AddEntryButton(20, ArrowRightID1, ArrowRightID2, GetButtonID(1, 0, 2), ArrowRightWidth, ArrowRightHeight);

            FinishPage();
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!SplitButtonID(info.ButtonID, 1, out var type, out var index))
            {
                return;
            }

            var entry = info.GetTextEntry(0);

            if (entry != null)
            {
                m_Batch.Condition = entry.Text;
            }

            for (var i = m_Batch.BatchCommands.Count - 1; i >= 0; --i)
            {
                var sc = m_Batch.BatchCommands[i];

                entry = info.GetTextEntry(1 + i * 2);

                if (entry != null)
                {
                    sc.Command = entry.Text;
                }

                entry = info.GetTextEntry(2 + i * 2);

                if (entry != null)
                {
                    sc.Object = entry.Text;
                }

                if (sc.Command.Length == 0 && sc.Object.Length == 0)
                {
                    m_Batch.BatchCommands.RemoveAt(i);
                }
            }

            switch (type)
            {
                case 0: // main
                    {
                        switch (index)
                        {
                            case 0: // run
                                {
                                    m_Batch.Run(m_From);
                                    break;
                                }
                            case 1: // set scope
                                {
                                    m_From.SendGump(new BatchScopeGump(m_From, m_Batch));
                                    return;
                                }
                            case 2: // add command
                                {
                                    m_Batch.BatchCommands.Add(new BatchCommand("", ""));
                                    break;
                                }
                        }

                        break;
                    }
            }

            m_From.SendGump(new BatchGump(m_From, m_Batch));
        }
    }

    public class BatchScopeGump : BaseGridGump
    {
        private readonly Batch m_Batch;
        private readonly Mobile m_From;

        public BatchScopeGump(Mobile from, Batch batch) : base(30, 30)
        {
            m_From = from;
            m_Batch = batch;

            Render();
        }

        public void Render()
        {
            AddNewPage();

            /* Header */
            AddEntryHeader(20);
            AddEntryHtml(140, Center("Change Scope"));
            AddEntryHeader(20);

            /* Options */
            for (var i = 0; i < BaseCommandImplementor.Implementors.Count; ++i)
            {
                var impl = BaseCommandImplementor.Implementors[i];

                if (m_From.AccessLevel < impl.AccessLevel)
                {
                    continue;
                }

                AddNewLine();

                AddEntryLabel(20 + OffsetSize + 140, impl.Accessors[0]);
                AddEntryButton(20, ArrowRightID1, ArrowRightID2, GetButtonID(1, 0, i), ArrowRightWidth, ArrowRightHeight);
            }

            FinishPage();
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (SplitButtonID(info.ButtonID, 1, out var type, out var index))
            {
                switch (type)
                {
                    case 0:
                        {
                            if (index < BaseCommandImplementor.Implementors.Count)
                            {
                                var impl = BaseCommandImplementor.Implementors[index];

                                if (m_From.AccessLevel >= impl.AccessLevel)
                                {
                                    m_Batch.Scope = impl;
                                }
                            }

                            break;
                        }
                }
            }

            m_From.SendGump(new BatchGump(m_From, m_Batch));
        }
    }
}
