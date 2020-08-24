using System;
using System.Collections.Generic;

namespace Server.Commands.Generic
{
    public class GlobalCommandImplementor : BaseCommandImplementor
    {
        public GlobalCommandImplementor()
        {
            Accessors = new[] { "Global" };
            SupportRequirement = CommandSupport.Global;
            SupportsConditionals = true;
            AccessLevel = AccessLevel.Administrator;
            Usage = "Global <command> [condition]";
            Description =
                "Invokes the command on all appropriate objects in the world. Optional condition arguments can further restrict the set of objects.";
        }

        public override void Compile(Mobile from, BaseCommand command, ref string[] args, ref object obj)
        {
            try
            {
                Extensions ext = Extensions.Parse(from, ref args);

                if (!CheckObjectTypes(from, command, ext, out bool items, out bool mobiles))
                    return;

                List<object> list = new List<object>();

                if (items)
                    foreach (Item item in World.Items.Values)
                        if (ext.IsValid(item))
                            list.Add(item);

                if (mobiles)
                    foreach (Mobile mob in World.Mobiles.Values)
                        if (ext.IsValid(mob))
                            list.Add(mob);

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
