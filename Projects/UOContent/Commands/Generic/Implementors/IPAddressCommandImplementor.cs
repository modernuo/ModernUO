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
                Extensions ext = Extensions.Parse(from, ref args);

                if (!CheckObjectTypes(from, command, ext, out bool _, out bool mobiles))
                    return;

                if (!mobiles) // sanity check
                {
                    command.LogFailure("This command does not support items.");
                    return;
                }

                List<object> list = new List<object>();
                List<IPAddress> addresses = new List<IPAddress>();

                List<NetState> states = TcpServer.Instances;

                for (int i = 0; i < states.Count; ++i)
                {
                    NetState ns = states[i];
                    Mobile mob = ns.Mobile;

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
