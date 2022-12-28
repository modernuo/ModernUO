using System;
using System.Collections.Generic;
using Server.Buffers;

namespace Server.Commands.Generic
{
    [Flags]
    public enum CommandSupport
    {
        Single = 0x0001,
        Global = 0x0002,
        Online = 0x0004,
        Multi = 0x0008,
        Area = 0x0010,
        Self = 0x0020,
        Region = 0x0040,
        Contained = 0x0080,
        IPAddress = 0x0100,

        All = Single | Global | Online | Multi | Area | Self | Region | Contained | IPAddress,
        AllMobiles = All & ~Contained,
        AllNPCs = All & ~(IPAddress | Online | Self | Contained),
        AllItems = All & ~(IPAddress | Online | Self),

        Simple = Single | Multi,
        Complex = Global | Online | Area | Region | Contained | IPAddress
    }

    public abstract class BaseCommandImplementor
    {
        private static List<BaseCommandImplementor> m_Implementors;

        public BaseCommandImplementor() => Commands = new Dictionary<string, BaseCommand>(StringComparer.OrdinalIgnoreCase);

        public bool SupportsConditionals { get; set; }

        public string[] Accessors { get; set; }

        public string Usage { get; set; }

        public string Description { get; set; }

        public AccessLevel AccessLevel { get; set; }

        public CommandSupport SupportRequirement { get; set; }

        public Dictionary<string, BaseCommand> Commands { get; }

        public static List<BaseCommandImplementor> Implementors
        {
            get
            {
                if (m_Implementors == null)
                {
                    m_Implementors = new List<BaseCommandImplementor>();
                    RegisterImplementors();
                }

                return m_Implementors;
            }
        }

        public static void RegisterImplementors()
        {
            Register(new RegionCommandImplementor());
            Register(new GlobalCommandImplementor());
            Register(new OnlineCommandImplementor());
            Register(new SingleCommandImplementor());
            Register(new SerialCommandImplementor());
            Register(new MultiCommandImplementor());
            Register(new AreaCommandImplementor());
            Register(new SelfCommandImplementor());
            Register(new ContainedCommandImplementor());
            Register(new IPAddressCommandImplementor());

            Register(new RangeCommandImplementor());
            Register(new ScreenCommandImplementor());
            Register(new FacetCommandImplementor());
        }

        public virtual void Compile(Mobile from, BaseCommand command, ref string[] args, ref object obj)
        {
            obj = null;
        }

        public virtual void Register(BaseCommand command)
        {
            for (var i = 0; i < command.Commands.Length; ++i)
            {
                Commands[command.Commands[i]] = command;
            }
        }

        public bool CheckObjectTypes(Mobile from, BaseCommand command, Extensions ext, out bool items, out bool mobiles)
        {
            items = mobiles = false;

            var cond = ObjectConditional.Empty;

            foreach (var check in ext)
            {
                if (check is WhereExtension extension)
                {
                    cond = extension.Conditional;

                    break;
                }
            }

            var condIsItem = cond.IsItem;
            var condIsMobile = cond.IsMobile;

            switch (command.ObjectTypes)
            {
                case ObjectTypes.All:
                case ObjectTypes.Both:
                    {
                        if (condIsItem)
                        {
                            items = true;
                        }

                        if (condIsMobile)
                        {
                            mobiles = true;
                        }

                        break;
                    }
                case ObjectTypes.Items:
                    {
                        if (condIsItem)
                        {
                            items = true;
                        }
                        else if (condIsMobile)
                        {
                            from.SendMessage("You may not use a mobile type condition for this command.");
                            return false;
                        }

                        break;
                    }
                case ObjectTypes.Mobiles:
                    {
                        if (condIsMobile)
                        {
                            mobiles = true;
                        }
                        else if (condIsItem)
                        {
                            from.SendMessage("You may not use an item type condition for this command.");
                            return false;
                        }

                        break;
                    }
            }

            return true;
        }

        public void RunCommand(Mobile from, BaseCommand command, string[] args)
        {
            try
            {
                object obj = null;

                Compile(from, command, ref args, ref obj);

                RunCommand(from, obj, command, args);
            }
            catch (Exception ex)
            {
                from.SendMessage(ex.Message);
            }
        }

        public static string GenerateArgString(string[] args)
        {
            if (args.Length == 0)
            {
                return "";
            }

            // NOTE: this does not preserve the case where quotation marks are used on a single word

            using var sb = new ValueStringBuilder(stackalloc char[64]);

            for (var i = 0; i < args.Length; ++i)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }

                if (args[i].ContainsOrdinal(' '))
                {
                    sb.Append('"');
                    sb.Append(args[i]);
                    sb.Append('"');
                }
                else
                {
                    sb.Append(args[i]);
                }
            }

            return sb.ToString();
        }

        public void RunCommand(Mobile from, object obj, BaseCommand command, string[] args)
        {
            // try
            // {
            var e = new CommandEventArgs(from, command.Commands[0], GenerateArgString(args), args);

            if (!command.ValidateArgs(this, e))
            {
                return;
            }

            var flushToLog = false;

            if (obj is List<object> list)
            {
                if (list.Count > 20)
                {
                    CommandLogging.Enabled = false;
                }
                else if (list.Count == 0)
                {
                    command.LogFailure("Nothing was found to use this command on.");
                }

                command.ExecuteList(e, list);

                if (list.Count > 20)
                {
                    flushToLog = true;
                    CommandLogging.Enabled = true;
                }
            }
            else if (obj != null)
            {
                if (command.ListOptimized)
                {
                    command.ExecuteList(e, new List<object> { obj });
                }
                else
                {
                    command.Execute(e, obj);
                }
            }

            command.Flush(from, flushToLog);
            // }
            // catch ( Exception ex )
            // {
            // from.SendMessage( ex.Message );
            // }
        }

        public virtual void Process(Mobile from, BaseCommand command, string[] args)
        {
            RunCommand(from, command, args);
        }

        public virtual void Execute(CommandEventArgs e)
        {
            if (e.Length >= 1)
            {
                if (!Commands.TryGetValue(e.GetString(0), out var command))
                {
                    e.Mobile.SendMessage(
                        "That is either an invalid command name or one that does not support this modifier."
                    );
                }
                else if (e.Mobile.AccessLevel < command.AccessLevel)
                {
                    e.Mobile.SendMessage("You do not have access to that command.");
                }
                else
                {
                    var oldArgs = e.Arguments;
                    var args = new string[oldArgs.Length - 1];

                    for (var i = 0; i < args.Length; ++i)
                    {
                        args[i] = oldArgs[i + 1];
                    }

                    Process(e.Mobile, command, args);
                }
            }
            else
            {
                e.Mobile.SendMessage("You must supply a command name.");
            }
        }

        public void Register()
        {
            if (Accessors == null)
            {
                return;
            }

            for (var i = 0; i < Accessors.Length; ++i)
            {
                CommandSystem.Register(Accessors[i], AccessLevel, Execute);
            }
        }

        public static void Register(BaseCommandImplementor impl)
        {
            m_Implementors.Add(impl);
            impl.Register();
        }
    }
}
