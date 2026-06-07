using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.ConPVP;

public class ParticipantGump : DynamicGump
{
    public override bool Singleton => true;

    private ParticipantGump(DuelContext context, Participant p) : base(50, 50)
    {
        Context = context;
        Participant = p;
    }

    public static void DisplayTo(Mobile from, DuelContext context, Participant p)
    {
        if (from?.NetState == null || context == null || p == null)
        {
            return;
        }

        var gumps = from.GetGumps();
        gumps.Close<RulesetGump>();
        gumps.Close<DuelContextGump>();

        from.SendGump(new ParticipantGump(context, p));
    }

    public DuelContext Context { get; }

    public Participant Participant { get; }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var count = Participant.Players.Length;

        if (count < 4)
        {
            count = 4;
        }

        builder.AddPage();

        var height = 35 + 10 + 22 + 22 + 30 + 22 + 2 + count * 22 + 2 + 30;

        builder.AddBackground(0, 0, 300, height, 9250);
        builder.AddBackground(10, 10, 280, height - 20, 0xDAC);

        builder.AddButton(240, 25, 0xFB1, 0xFB3, 3);

        builder.AddHtml(35, 25, 230, 20, Center("Participant Setup"));

        var x = 35;
        var y = 47;

        builder.AddHtml(x, y, 200, 20, $"Team Size: {Participant.Players.Length}");
        y += 22;

        AddGoldenButtonLabeled(ref builder, x + 20, y, 1, "Increase");
        y += 22;
        AddGoldenButtonLabeled(ref builder, x + 20, y, 2, "Decrease");
        y += 30;

        builder.AddHtml(35, y, 230, 20, Center("Players"));
        y += 22;

        for (var i = 0; i < Participant.Players.Length; ++i)
        {
            var pl = Participant.Players[i];

            AddGoldenButtonLabeled(ref builder, x, y, 5 + i, $"{1 + i}: {(pl == null ? "Empty" : pl.Mobile.Name)}");
            y += 22;
        }
    }

    private static string Center(string text) => $"<CENTER>{text}</CENTER>";

    private static void AddGoldenButton(ref DynamicGumpBuilder builder, int x, int y, int bid)
    {
        builder.AddButton(x, y, 0xD2, 0xD2, bid);
        builder.AddButton(x + 3, y + 3, 0xD8, 0xD8, bid);
    }

    private static void AddGoldenButtonLabeled(ref DynamicGumpBuilder builder, int x, int y, int bid, string text)
    {
        AddGoldenButton(ref builder, x, y, bid);
        builder.AddHtml(x + 25, y, 200, 20, text);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (!Context.Registered)
        {
            return;
        }

        var from = sender.Mobile;
        var bid = info.ButtonID;

        switch (bid)
        {
            case 0:
                {
                    DuelContextGump.DisplayTo(from, Context);
                    return;
                }
            case 1 when Participant.Count < 8:
                {
                    Participant.Resize(Participant.Count + 1);
                    break;
                }
            case 1:
                {
                    from.SendMessage("You may not raise the team size any further.");
                    break;
                }
            case 2 when Participant.Count > 1 && Participant.Count > Participant.FilledSlots:
                {
                    Participant.Resize(Participant.Count - 1);
                    break;
                }
            case 2:
                {
                    from.SendMessage("You may not lower the team size any further.");
                    break;
                }
            case 3 when Participant.FilledSlots > 0:
                {
                    from.SendMessage("There is at least one currently active player. You must remove them first.");
                    break;
                }
            case 3 when Context.Participants.Count > 2:
                {
                    Context.Participants.Remove(Participant);
                    DuelContextGump.DisplayTo(from, Context);
                    return;
                }
            case 3:
                {
                    from.SendMessage("Duels must have at least two participating parties.");
                    break;
                }
            default:
                {
                    bid -= 5;

                    if (bid >= 0 && bid < Participant.Players.Length)
                    {
                        if (Participant.Players[bid] == null)
                        {
                            from.Target = new ParticipantTarget(Context, Participant, bid);
                            from.SendMessage("Target a player.");
                            return;
                        }

                        Participant.Players[bid].Mobile.SendMessage("You have been removed from the duel.");

                        if (Participant.Players[bid].Mobile is PlayerMobile playerMobile)
                        {
                            playerMobile.DuelPlayer = null;
                        }

                        Participant.Players[bid] = null;
                        from.SendMessage("They have been removed from the duel.");
                    }
                    else
                    {
                        return;
                    }

                    break;
                }
        }

        from.SendGump(this); // refresh-via-this
    }

    private class ParticipantTarget : Target
    {
        private readonly DuelContext _context;
        private readonly int _index;
        private readonly Participant _participant;

        public ParticipantTarget(DuelContext context, Participant p, int index) : base(12, false, TargetFlags.None)
        {
            _context = context;
            _participant = p;
            _index = index;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!_context.Registered)
            {
                return;
            }

            if (_index < 0 || _index >= _participant.Players.Length)
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
            else if (mob is not PlayerMobile pm)
            {
            }
            else if (pm.DuelContext != null)
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
                if (_participant.Find(from) == null)
                {
                    from.SendMessage($"You send a challenge to {mob.Name}.");
                }
                else
                {
                    from.SendMessage($"You send an invitation to {mob.Name}.");
                }

                AcceptDuelGump.DisplayTo(from, mob, _context, _participant, _index);
            }
        }

        protected override void OnTargetFinish(Mobile from) => DisplayTo(from, _context, _participant);
    }
}
