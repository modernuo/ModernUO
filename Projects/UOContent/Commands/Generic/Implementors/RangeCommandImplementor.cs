namespace Server.Commands.Generic
{
    public class RangeCommandImplementor : BaseCommandImplementor
    {
        public RangeCommandImplementor()
        {
            Accessors = new[] { "Range" };
            SupportRequirement = CommandSupport.Area;
            SupportsConditionals = true;
            AccessLevel = AccessLevel.GameMaster;
            Usage = "Range <range> <command> [condition]";
            Description =
                "Invokes the command on all appropriate objects within a specified range of you. Optional condition arguments can further restrict the set of objects.";

            Instance = this;
        }

        public static RangeCommandImplementor Instance { get; private set; }

        public override void Execute(CommandEventArgs e)
        {
            if (e.Length >= 2)
            {
                var range = e.GetInt32(0);

                if (range < 0)
                {
                    e.Mobile.SendMessage("The range must not be negative.");
                }
                else
                {
                    Commands.TryGetValue(e.GetString(1), out var command);

                    if (command == null)
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
                        var args = new string[oldArgs.Length - 2];

                        for (var i = 0; i < args.Length; ++i)
                        {
                            args[i] = oldArgs[i + 2];
                        }

                        Process(range, e.Mobile, command, args);
                    }
                }
            }
            else
            {
                e.Mobile.SendMessage("You must supply a range and a command name.");
            }
        }

        public void Process(int range, Mobile from, BaseCommand command, string[] args)
        {
            var impl = AreaCommandImplementor.Instance;

            if (impl == null)
            {
                return;
            }

            var map = from.Map;

            if (map == null || map == Map.Internal)
            {
                return;
            }

            var start = new Point3D(from.X - range, from.Y - range, from.Z);
            var end = new Point3D(from.X + range, from.Y + range, from.Z);

            impl.OnTarget(from, map, start, end, command, args);
        }
    }
}
