using Server.Engines.PartySystem;

namespace Server.ContextMenus
{
    public class RemoveFromPartyEntry : ContextMenuEntry
    {
        public RemoveFromPartyEntry() : base(0198, 12)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            var p = Party.Get(from);

            if (target is not Mobile mobile || p == null || p.Leader != from || !p.Contains(mobile))
            {
                return;
            }

            if (from == mobile)
            {
                from.SendLocalizedMessage(1005446); // You may only remove yourself from a party if you are not the leader.
            }
            else
            {
                p.Remove(mobile);
            }
        }
    }
}
