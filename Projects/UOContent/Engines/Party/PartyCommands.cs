namespace Server.Engines.PartySystem
{
    public class PartyCommandHandlers : PartyCommands
    {
        public static void Initialize()
        {
            Handler = new PartyCommandHandlers();
        }

        public override void OnAdd(Mobile from)
        {
            var p = Party.Get(from);

            if (p != null && p.Leader != from)
            {
                from.SendLocalizedMessage(1005453); // You may only add members to the party if you are the leader.
            }
            else if (p != null && p.Members.Count + p.Candidates.Count >= Party.Capacity)
            {
                from.SendLocalizedMessage(1008095); // You may only have 10 in your party (this includes candidates).
            }
            else
            {
                from.Target = new AddPartyTarget(from);
            }
        }

        public override void OnRemove(Mobile from, Mobile target)
        {
            var p = Party.Get(from);

            if (p == null)
            {
                from.SendLocalizedMessage(3000211); // You are not in a party.
                return;
            }

            if (p.Leader == from && target == null)
            {
                from.SendLocalizedMessage(1005455); // Who would you like to remove from your party?
                from.Target = new RemovePartyTarget();
            }
            else if ((p.Leader == from || from == target) && p.Contains(target))
            {
                p.Remove(target);
            }
        }

        public override void OnPrivateMessage(Mobile from, Mobile target, string text)
        {
            if (text.Length > 128 || (text = text.Trim()).Length == 0)
            {
                return;
            }

            var p = Party.Get(from);

            if (p?.Contains(target) == true)
            {
                p.SendPrivateMessage(from, target, text);
            }
            else
            {
                from.SendLocalizedMessage(3000211); // You are not in a party.
            }
        }

        public override void OnPublicMessage(Mobile from, string text)
        {
            if (text.Length > 128 || (text = text.Trim()).Length == 0)
            {
                return;
            }

            var p = Party.Get(from);

            if (p != null)
            {
                p.SendPublicMessage(from, text);
            }
            else
            {
                from.SendLocalizedMessage(3000211); // You are not in a party.
            }
        }

        public override void OnSetCanLoot(Mobile from, bool canLoot)
        {
            var p = Party.Get(from);

            if (p == null)
            {
                from.SendLocalizedMessage(3000211); // You are not in a party.
            }
            else
            {
                var mi = p[from];

                if (mi != null)
                {
                    mi.CanLoot = canLoot;

                    if (canLoot)
                    {
                        from.SendLocalizedMessage(1005447); // You have chosen to allow your party to loot your corpse.
                    }
                    else
                    {
                        // You have chosen to prevent your party from looting your corpse.
                        from.SendLocalizedMessage(1005448);
                    }
                }
            }
        }

        public override void OnAccept(Mobile from, Mobile sentLeader)
        {
            var leader = from.Party as Mobile;
            from.Party = null;

            var p = Party.Get(leader);

            if (leader == null || p?.Candidates.Contains(from) != true)
            {
                from.SendLocalizedMessage(3000222); // No one has invited you to be in a party.
            }
            else if (p.Members.Count + p.Candidates.Count <= Party.Capacity)
            {
                p.OnAccept(from);
            }
        }

        public override void OnDecline(Mobile from, Mobile sentLeader)
        {
            var leader = from.Party as Mobile;
            from.Party = null;

            var p = Party.Get(leader);

            if (leader == null || p?.Candidates.Contains(from) != true)
            {
                from.SendLocalizedMessage(3000222); // No one has invited you to be in a party.
            }
            else
            {
                p.OnDecline(from, leader);
            }
        }
    }
}
