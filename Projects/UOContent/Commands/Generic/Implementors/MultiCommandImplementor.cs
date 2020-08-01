using Server.Targeting;

namespace Server.Commands.Generic
{
  public class MultiCommandImplementor : BaseCommandImplementor
  {
    public MultiCommandImplementor()
    {
      Accessors = new[] { "Multi", "m" };
      SupportRequirement = CommandSupport.Multi;
      AccessLevel = AccessLevel.Counselor;
      Usage = "Multi <command>";
      Description = "Invokes the command on multiple targeted objects.";
    }

    public override void Process(Mobile from, BaseCommand command, string[] args)
    {
      if (command.ValidateArgs(this, new CommandEventArgs(from, command.Commands[0], GenerateArgString(args), args)))
        from.BeginTarget(-1, command.ObjectTypes == ObjectTypes.All, TargetFlags.None,
          (m, targeted, a) => OnTarget(m, targeted, command, a), args);
    }

    public void OnTarget(Mobile from, object targeted, BaseCommand command, string[] args)
    {
      if (!BaseCommand.IsAccessible(from, targeted))
      {
        from.SendLocalizedMessage(500447); // That is not accessible.
        from.BeginTarget(-1, command.ObjectTypes == ObjectTypes.All, TargetFlags.None,
          (m, t, a) => OnTarget(m, t, command, a), args);
        return;
      }

      switch (command.ObjectTypes)
      {
        case ObjectTypes.Both:
          {
            if (!(targeted is Item || targeted is Mobile))
            {
              from.SendMessage("This command does not work on that.");
              return;
            }

            break;
          }
        case ObjectTypes.Items:
          {
            if (!(targeted is Item))
            {
              from.SendMessage("This command only works on items.");
              return;
            }

            break;
          }
        case ObjectTypes.Mobiles:
          {
            if (!(targeted is Mobile))
            {
              from.SendMessage("This command only works on mobiles.");
              return;
            }

            break;
          }
      }

      RunCommand(from, targeted, command, args);

      from.BeginTarget(-1, command.ObjectTypes == ObjectTypes.All, TargetFlags.None,
        (m, t, a) => OnTarget(m, t, command, a), args);
    }
  }
}
