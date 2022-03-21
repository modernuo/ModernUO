using Server.Commands;
using Server.Commands.Generic;
using Server.Targeting;

namespace Server.Targets
{
    public class MoveTarget : Target
    {
        private readonly object m_Object;

        public MoveTarget(object o) : base(-1, true, TargetFlags.None) => m_Object = o;

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is not IPoint3D ip)
            {
                return;
            }

            if (!BaseCommand.IsAccessible(from, m_Object))
            {
                from.SendLocalizedMessage(500447); // That is not accessible.
                return;
            }

            Point3D p = ip switch
            {
                Item i => i.GetWorldTop(),
                Mobile m  => m.Location,
                _         => new Point3D(ip)
            };

            CommandLogging.WriteLine(
                from,
                "{0} {1} moving {2} to {3}",
                from.AccessLevel,
                CommandLogging.Format(from),
                CommandLogging.Format(m_Object),
                p
            );

            if (m_Object is Item item)
            {
                if (!item.Deleted)
                {
                    item.MoveToWorld(p, from.Map);
                }
            }
            else if (m_Object is Mobile m)
            {
                if (!m.Deleted)
                {
                    m.MoveToWorld(p, from.Map);
                }
            }
        }
    }
}
