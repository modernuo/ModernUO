using Server.Commands.Generic;
using Server.Targeting;

namespace Server.Targets
{
  public class PickMoveTarget : Target
  {
    public PickMoveTarget() : base(-1, false, TargetFlags.None)
    {
    }

    protected override void OnTarget(Mobile from, object o)
    {
      if (!BaseCommand.IsAccessible(from, o))
      {
        from.SendLocalizedMessage(500447); // That is not accessible.
        return;
      }

      if (o is Item || o is Mobile)
        from.Target = new MoveTarget(o);
    }
  }
}