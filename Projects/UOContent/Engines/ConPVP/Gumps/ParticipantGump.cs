using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.ConPVP
{
    public class ParticipantGump : Gump
    {
        public ParticipantGump(Mobile from, DuelContext context, Participant p) : base(50, 50)
        {
            From = from;
            Context = context;
            Participant = p;

            from.CloseGump<RulesetGump>();
            from.CloseGump<DuelContextGump>();
            from.CloseGump<ParticipantGump>();

            var count = p.Players.Length;

            if (count < 4)
            {
                count = 4;
            }

            AddPage(0);

            var height = 35 + 10 + 22 + 22 + 30 + 22 + 2 + count * 22 + 2 + 30;

            AddBackground(0, 0, 300, height, 9250);
            AddBackground(10, 10, 280, height - 20, 0xDAC);

            AddButton(240, 25, 0xFB1, 0xFB3, 3);

            // AddButton( 223, 54, 0x265A, 0x265A, 4, );

            AddHtml(35, 25, 230, 20, Center("Participant Setup"));

            var x = 35;
            var y = 47;

            AddHtml(x, y, 200, 20, $"Team Size: {p.Players.Length}");
            y += 22;

            AddGoldenButtonLabeled(x + 20, y, 1, "Increase");
            y += 22;
            AddGoldenButtonLabeled(x + 20, y, 2, "Decrease");
            y += 30;

            AddHtml(35, y, 230, 20, Center("Players"));
            y += 22;

            for (var i = 0; i < p.Players.Length; ++i)
            {
                var pl = p.Players[i];

                AddGoldenButtonLabeled(x, y, 5 + i, $"{1 + i}: {(pl == null ? "Empty" : pl.Mobile.Name)}");
                y += 22;
            }
        }

        public Mobile From { get; }

        public DuelContext Context { get; }

        public Participant Participant { get; }

        public string Center(string text) => $"<CENTER>{text}</CENTER>";

        public void AddGoldenButton(int x, int y, int bid)
        {
            AddButton(x, y, 0xD2, 0xD2, bid);
            AddButton(x + 3, y + 3, 0xD8, 0xD8, bid);
        }

        public void AddGoldenButtonLabeled(int x, int y, int bid, string text)
        {
            AddGoldenButton(x, y, bid);
            AddHtml(x + 25, y, 200, 20, text);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!Context.Registered)
            {
                return;
            }

            var bid = info.ButtonID;

            if (bid == 0)
            {
                From.SendGump(new DuelContextGump(From, Context));
            }
            else if (bid == 1)
            {
                if (Participant.Count < 8)
                {
                    Participant.Resize(Participant.Count + 1);
                }
                else
                {
                    From.SendMessage("You may not raise the team size any further.");
                }

                From.SendGump(new ParticipantGump(From, Context, Participant));
            }
            else if (bid == 2)
            {
                if (Participant.Count > 1 && Participant.Count > Participant.FilledSlots)
                {
                    Participant.Resize(Participant.Count - 1);
                }
                else
                {
                    From.SendMessage("You may not lower the team size any further.");
                }

                From.SendGump(new ParticipantGump(From, Context, Participant));
            }
            else if (bid == 3)
            {
                if (Participant.FilledSlots > 0)
                {
                    From.SendMessage("There is at least one currently active player. You must remove them first.");
                    From.SendGump(new ParticipantGump(From, Context, Participant));
                }
                else if (Context.Participants.Count > 2)
                {
                    /*Container cont = m_Participant.Stakes;

                    if (cont != null)
                      cont.Delete();*/

                    Context.Participants.Remove(Participant);
                    From.SendGump(new DuelContextGump(From, Context));
                }
                else
                {
                    From.SendMessage("Duels must have at least two participating parties.");
                    From.SendGump(new ParticipantGump(From, Context, Participant));
                }
            }
            /*else if (bid == 4)
            {
              m_From.SendGump( new ParticipantGump( m_From, m_Context, m_Participant ) );

              Container cont = m_Participant.Stakes;

              if (cont != null && !cont.Deleted)
              {
                cont.DisplayTo( m_From );

                Item[] checks = cont.FindItemsByType( typeof( BankCheck ) );

                int gold = cont.TotalGold;

                for ( int i = 0; i < checks.Length; ++i )
                  gold += ((BankCheck)checks[i]).Worth;

                m_From.SendMessage( "This container has {0} item{1} and {2} stone{3}. In gold or check form there is a total of {4:D}gp.", cont.TotalItems, cont.TotalItems==1?"":"s", cont.TotalWeight, cont.TotalWeight==1?"":"s", gold );
              }
            }*/
            else
            {
                bid -= 5;

                if (bid >= 0 && bid < Participant.Players.Length)
                {
                    if (Participant.Players[bid] == null)
                    {
                        From.Target = new ParticipantTarget(Context, Participant, bid);
                        From.SendMessage("Target a player.");
                    }
                    else
                    {
                        Participant.Players[bid].Mobile.SendMessage("You have been removed from the duel.");

                        if (Participant.Players[bid].Mobile is PlayerMobile)
                        {
                            ((PlayerMobile)Participant.Players[bid].Mobile).DuelPlayer = null;
                        }

                        Participant.Players[bid] = null;
                        From.SendMessage("They have been removed from the duel.");
                        From.SendGump(new ParticipantGump(From, Context, Participant));
                    }
                }
            }
        }

        private class ParticipantTarget : Target
        {
            private readonly DuelContext m_Context;
            private readonly int m_Index;
            private readonly Participant m_Participant;

            public ParticipantTarget(DuelContext context, Participant p, int index) : base(12, false, TargetFlags.None)
            {
                m_Context = context;
                m_Participant = p;
                m_Index = index;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!m_Context.Registered)
                {
                    return;
                }

                var index = m_Index;

                if (index < 0 || index >= m_Participant.Players.Length)
                {
                    return;
                }

                if (targeted is not Mobile mob)
                {
                    from.SendMessage("That is not a player.");
                }
                else if (!mob.Player)
                {
                    if (mob.Body.IsHuman)
                    {
                        mob.SayTo(from, 1005443); // Nay, I would rather stay here and watch a nail rust.
                    }
                    else
                    {
                        mob.SayTo(from, 1005444); // The creature ignores your offer.
                    }
                }
                else if (AcceptDuelGump.IsIgnored(mob, from) || mob.Blessed)
                {
                    from.SendMessage("They ignore your offer.");
                }
                else
                {
                    if (mob is not PlayerMobile pm)
                    {
                        return;
                    }

                    if (pm.DuelContext != null)
                    {
                        from.SendMessage($"{pm.Name} cannot fight because they are already assigned to another duel.");
                    }
                    else if (DuelContext.CheckCombat(pm))
                    {
                        from.SendMessage(
                            $"{pm.Name} cannot fight because they have recently been in combat with another player."
                        );
                    }
                    else if (mob.HasGump<AcceptDuelGump>())
                    {
                        from.SendMessage($"{mob.Name} has already been offered a duel.");
                    }
                    else
                    {
                        if (m_Participant.Find(from) == null)
                        {
                            from.SendMessage($"You send a challenge to {mob.Name}.");
                        }
                        else
                        {
                            from.SendMessage($"You send an invitation to {mob.Name}.");
                        }

                        mob.SendGump(new AcceptDuelGump(from, mob, m_Context, m_Participant, m_Index));
                    }
                }
            }

            protected override void OnTargetFinish(Mobile from)
            {
                from.SendGump(new ParticipantGump(from, m_Context, m_Participant));
            }
        }
    }
}
