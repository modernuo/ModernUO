using Server.Targeting;

namespace Server.Engines.PartySystem
{
    public class RemovePartyTarget : Target
    {
        public RemovePartyTarget() : base(8, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is Mobile m)
            {
                var p = Party.Get(from);

                if (p == null || p.Leader != from || !p.Contains(m))
                {
                    return;
                }

                if (from == m)
                {
                    // You may only remove yourself from a party if you are not the leader.
                    from.SendLocalizedMessage(1005446);
                }
                else
                {
                    p.Remove(m);
                }
            }
        }
    }
}
