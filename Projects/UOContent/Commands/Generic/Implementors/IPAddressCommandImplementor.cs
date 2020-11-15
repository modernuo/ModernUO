using System;
using System.Collections.Generic;
using System.Net;
using Server.Network;

namespace Server.Commands.Generic
{
    public class IPAddressCommandImplementor : BaseCommandImplementor
    {
        public IPAddressCommandImplementor()
        {
            Accessors = new[] { "IPAddress" };
            SupportRequirement = CommandSupport.IPAddress;
            SupportsConditionals = true;
            AccessLevel = AccessLevel.Administrator;
            Usage = "IPAddress <command> [condition]";
            Description =
                "Invokes the command on one mobile from each IP address that is logged in. Optional condition arguments can further restrict the set of objects.";
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
                var addresses = new List<IPAddress>();

                foreach (var ns in TcpServer.Instances)
                {
                    var mob = ns.Mobile;

                    if (mob != null && !addresses.Contains(ns.Address) && ext.IsValid(mob))
                    {
                        list.Add(mob);
                        addresses.Add(ns.Address);
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
