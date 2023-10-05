using System;
using System.Collections.Generic;
using Server.Items;
using Server.Targeting;

namespace Server.Commands.Generic
{
    public class ContainedCommandImplementor : BaseCommandImplementor
    {
        public ContainedCommandImplementor()
        {
            Accessors = new[] { "Contained" };
            SupportRequirement = CommandSupport.Contained;
            AccessLevel = AccessLevel.GameMaster;
            Usage = "Contained <command> [condition]";
            Description =
                "Invokes the command on all child items in a targeted container. Optional condition arguments can further restrict the set of objects.";
        }

        public override void Process(Mobile from, BaseCommand command, string[] args)
        {
            if (command.ValidateArgs(this, new CommandEventArgs(from, command.Commands[0], GenerateArgString(args), args)))
            {
                from.BeginTarget(
                    -1,
                    command.ObjectTypes == ObjectTypes.All,
                    TargetFlags.None,
                    (m, targeted, a) => OnTarget(m, targeted, command, a),
                    args
                );
            }
        }

        public void OnTarget(Mobile from, object targeted, BaseCommand command, string[] args)
        {
            if (!BaseCommand.IsAccessible(from, targeted))
            {
                from.SendLocalizedMessage(500447); // That is not accessible.
                return;
            }

            if (command.ObjectTypes == ObjectTypes.Mobiles)
            {
                return; // sanity check
            }

            if (targeted is not Container cont)
            {
                from.SendMessage("That is not a container.");
                return;
            }

            try
            {
                var ext = Extensions.Parse(from, ref args);

                if (!CheckObjectTypes(from, command, ext, out var items, out var _))
                {
                    return;
                }

                if (!items)
                {
                    from.SendMessage("This command only works on items.");
                    return;
                }

                var list = new List<object>();

                foreach (var item in cont.FindItems())
                {
                    if (ext.IsValid(item))
                    {
                        list.Add(item);
                    }
                }

                ext.Filter(list);

                RunCommand(from, list, command, args);
            }
            catch (Exception e)
            {
                from.SendMessage(e.Message);
            }
        }
    }
}
