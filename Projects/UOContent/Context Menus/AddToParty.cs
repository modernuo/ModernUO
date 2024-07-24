using Server.Engines.PartySystem;

namespace Server.ContextMenus
{
    public class AddToPartyEntry : ContextMenuEntry
    {
        public AddToPartyEntry() : base(0197, 12)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (target is not Mobile targetMobile)
            {
                return;
            }

            var p = Party.Get(from);
            var mp = Party.Get(targetMobile);

            if (from == targetMobile)
            {
                from.SendLocalizedMessage(1005439); // You cannot add yourself to a party.
            }
            else if (p != null && p.Leader != from)
            {
                from.SendLocalizedMessage(1005453); // You may only add members to the party if you are the leader.
            }
            else if (p != null && p.Members.Count + p.Candidates.Count >= Party.Capacity)
            {
                from.SendLocalizedMessage(1008095); // You may only have 10 in your party (this includes candidates).
            }
            else if (!targetMobile.Player)
            {
                from.SendLocalizedMessage(1005444); // The creature ignores your offer.
            }
            else if (mp != null && mp == p)
            {
                from.SendLocalizedMessage(1005440); // This person is already in your party!
            }
            else if (mp != null)
            {
                from.SendLocalizedMessage(1005441); // This person is already in a party!
            }
            else
            {
                Party.Invite(from, targetMobile);
            }
        }
    }
}
