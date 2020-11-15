using System;
using System.Collections.Generic;
using Server.Network;

namespace Server.Commands.Generic
{
    public class OnlineCommandImplementor : BaseCommandImplementor
    {
        public OnlineCommandImplementor()
        {
            Accessors = new[] { "Online" };
            SupportRequirement = CommandSupport.Online;
            SupportsConditionals = true;
            AccessLevel = AccessLevel.GameMaster;
            Usage = "Online <command> [condition]";
            Description =
                "Invokes the command on all mobiles that are currently logged in. Optional condition arguments can further restrict the set of objects.";
        }

        public override void Compile(Mobile from, BaseCommand command, ref string[] args, ref object obj)
        {
            try
            {
                var ext = Extensions.Parse(from, ref args);

                if (!CheckObjectTypes(from, command, ext, out var _, out var mobiles))
                {
                    return;
                }

                if (!mobiles) // sanity check
                {
                    command.LogFailure("This command does not support items.");
                    return;
                }

                var list = new List<object>();

                foreach (var ns in TcpServer.Instances)
                {
                    var mob = ns.Mobile;

                    if (mob == null)
                    {
                        continue;
                    }

                    if (!BaseCommand.IsAccessible(from, mob))
                    {
                        continue;
                    }

                    if (ext.IsValid(mob))
                    {
                        list.Add(mob);
                    }
                }

                ext.Filter(list);

                obj = list;
            }
            catch (Exception ex)
            {
                from.SendMessage(ex.Message);
            }
        }
    }
}
