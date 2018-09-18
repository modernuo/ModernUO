using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        from.BeginTarget(-1, command.ObjectTypes == ObjectTypes.All, TargetFlags.None,
          new TargetStateCallback(OnTarget), new object[] { command, args });
    }

    public void OnTarget(Mobile from, object targeted, object state)
    {
      if (!BaseCommand.IsAccessible(from, targeted))
      {
        from.SendLocalizedMessage(500447); // That is not accessible.
        return;
      }

      object[] states = (object[])state;
      BaseCommand command = (BaseCommand)states[0];
      string[] args = (string[])states[1];

      if (command.ObjectTypes == ObjectTypes.Mobiles)
        return; // sanity check

      if (!(targeted is Container cont))
      {
        from.SendMessage("That is not a container.");
        return;
      }
      
      try
      {
        Extensions ext = Extensions.Parse(from, ref args);

        if (!CheckObjectTypes(from, command, ext, out bool items, out bool _))
          return;

        if (!items)
        {
          from.SendMessage("This command only works on items.");
          return;
        }

        List<Item> list = cont.FindItemsByType<Item>().Where(item => ext.IsValid(item)).ToList();

        // TODO: Is there a way to avoid using ArrayList?
        ext.Filter(new ArrayList(list));

        RunCommand(from, list, command, args);
      }
      catch (Exception e)
      {
        from.SendMessage(e.Message);
      }
    }
  }
}