using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Two of ours set against each other on purpose, and the rule that says when it is over.
///
/// <para>
/// <b>Patrick's order of 15.09.2026: Argus holds a championship between the strongest bots, one against one.</b>
/// The engine lets one player strike another in Felucca, but it charges for it: the striker is flagged criminal,
/// guards come, and the population's own reflexes read every blow as an attack. A duel is the one arrangement under
/// which two bots may hit each other with none of that — the pair is written here, <c>BotMobile.IsHarmfulCriminal</c>
/// reads it and answers no, and the two brawls pressed on them ask this class every beat whether the duel is done.
/// </para>
///
/// <para>
/// <b>Nobody dies for a title.</b> A duel is over when either duellist is down to <see cref="YieldAt"/> of its health,
/// or the clock runs out, and the one with the larger share standing wins. The bodies are the shard's fighters and
/// they have work to go back to; a championship that killed fifteen of them would be a plague beast with a trophy.
/// </para>
/// </summary>
public static class BotDuel
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDuel));

    /// <summary>The share of health at which a duellist yields. A third: low enough to be a real fight, high enough that one more blow is not a death.</summary>
    public static double YieldAt { get; set; } = 0.35;

    /// <summary>The longest a duel may run before it is called on health. Two minutes.</summary>
    public static int CapMs { get; set; } = 120000;

    /// <summary>Duels begun.</summary>
    public static long Begun { get; private set; }

    /// <summary>Duels decided by a yield.</summary>
    public static long Yielded { get; private set; }

    /// <summary>Duels decided by the clock.</summary>
    public static long Called { get; private set; }

    /// <summary>Duels in which somebody died anyway.</summary>
    public static long Deaths { get; private set; }

    private sealed class Bout
    {
        public BotMobile A;

        public BotMobile B;

        public long Began;

        public BotMobile Winner;

        public string Why;
    }

    private static readonly Dictionary<Serial, Bout> _bouts = [];

    /// <summary>Whether these two are in a duel with each other right now.</summary>
    public static bool Between(Mobile a, Mobile b) =>
        a != null && b != null && _bouts.TryGetValue(a.Serial, out var bout)
        && (ReferenceEquals(bout.A, b) || ReferenceEquals(bout.B, b));

    /// <summary>
    /// Whether this one is in any duel, or held at the arenas between its bouts (<see cref="Held"/>): either way it belongs to the
    /// referee, and nobody presses it into a company, quarrels with it, robs it or calls it away.
    ///
    /// <para>
    /// <b>The arena's entrants wait between bouts (07.10.2026, arena27).</b> Patrick's championship is held at the round arenas,
    /// where a batch fights while the next ones stand waiting in their own rings. A waiting entrant is in no duel on this record,
    /// and the checks that keep a duellist out of everything else — a company's levy (<c>BotSquads.Join</c>), a war's quarrel
    /// (<c>BotFeud.Quarrel</c>), a robbery, a chat — all ask this, so the hold is answered here once rather than at each of them.
    /// The record itself is <see cref="InBout"/>.
    /// </para>
    /// </summary>
    public static bool Duelling(Mobile m) => m != null && (_bouts.ContainsKey(m.Serial) || Held?.Invoke(m) == true);

    /// <summary>Whether this one has a bout on the record: a duel set up and not yet taken off. Not the arena's hold.</summary>
    public static bool InBout(Mobile m) => m != null && _bouts.ContainsKey(m.Serial);

    /// <summary>
    /// The arena's hold, filled by the championship (<c>BotTourney</c>, in the debugger's assembly, which this one cannot name): true
    /// for an entrant standing at the arenas between its bouts. Null while nobody is held.
    /// </summary>
    public static Func<Mobile, bool> Held { get; set; }

    /// <summary>
    /// Sets the two against each other: writes the pair and presses a brawl on each. False when either could not be
    /// pressed, in which case nothing is written.
    /// </summary>
    /// <param name="stack">
    /// The arena's bouts (07.10.2026, arena27): the entrant's wait is put down over its own work already put down for the wait, so
    /// both are taken up again in order — the wait after the bout, the work after the championship (<c>BotWill.Press</c>).
    /// </param>
    public static bool Begin(BotMobile a, BotMobile b, SkillName trainsA, SkillName trainsB, bool stack = false)
    {
        // The record, not the arena's hold: an entrant held at the arenas is exactly who is set against somebody here.
        if (a is not { Deleted: false, Alive: true } || b is not { Deleted: false, Alive: true } || a == b
            || InBout(a) || InBout(b))
        {
            return false;
        }

        var bout = new Bout { A = a, B = b, Began = Core.TickCount };

        _bouts[a.Serial] = bout;
        _bouts[b.Serial] = bout;

        var pressedA = BotWill.Press(a, new BotBrawl(b, BotBrawl.Duel, trainsA, Over), $"a duel against {b.Name}", stack);
        var pressedB = BotWill.Press(b, new BotBrawl(a, BotBrawl.Duel, trainsB, Over), $"a duel against {a.Name}", stack);

        if (!pressedA || !pressedB)
        {
            _bouts.Remove(a.Serial);
            _bouts.Remove(b.Serial);

            if (pressedA)
            {
                BotWill.Abandon(a, "the duel could not be set up", unreached: false);
            }

            if (pressedB)
            {
                BotWill.Abandon(b, "the duel could not be set up", unreached: false);
            }

            return false;
        }

        Begun++;

        logger.Information("{A} and {B} are set against each other in a duel", a.Name, b.Name);

        return true;
    }

    /// <summary>
    /// Presses the brawls of a bout already on the record again, its clock left running: for a referee who has put a pair whose
    /// brawls both ended without a winner — each lost the other — back beside each other (07.10.2026, reach28; see
    /// <c>BotTourney.Replace</c>). False, with nothing changed, when the two are not in one undecided bout or either cannot be pressed.
    /// </summary>
    public static bool Again(BotMobile a, BotMobile b, SkillName trainsA, SkillName trainsB, bool stack = false)
    {
        if (a is not { Deleted: false, Alive: true } || b is not { Deleted: false, Alive: true } || !Between(a, b)
            || _bouts[a.Serial].Winner != null)
        {
            return false;
        }

        var pressedA = BotWill.Press(a, new BotBrawl(b, BotBrawl.Duel, trainsA, Over), $"the duel against {b.Name} again", stack);
        var pressedB = BotWill.Press(b, new BotBrawl(a, BotBrawl.Duel, trainsB, Over), $"the duel against {a.Name} again", stack);

        if (pressedA && pressedB)
        {
            Pressed++;

            return true;
        }

        if (pressedA)
        {
            BotWill.Abandon(a, "the duel could not be pressed again", unreached: false);
        }

        if (pressedB)
        {
            BotWill.Abandon(b, "the duel could not be pressed again", unreached: false);
        }

        return false;
    }

    /// <summary>Bouts whose brawls were pressed again by a referee. See <see cref="Again"/>.</summary>
    public static long Pressed { get; private set; }

    /// <summary>The share of health this one has left.</summary>
    private static double Share(Mobile m) =>
        m is { Deleted: false, Alive: true, HitsMax: > 0 } ? Math.Clamp(m.Hits / (double)m.HitsMax, 0.0, 1.0) : 0.0;

    /// <summary>
    /// The brawls' rule: the reason this duel is over, or null while it goes on. Decides the winner the first time it
    /// answers, and answers the same to both duellists after that.
    /// </summary>
    private static string Over(IBotWilful bot, BotBrawl brawl)
    {
        // No bout on the record: the referee has already read the result and taken the pair off, which happens
        // between the winner's beat and the loser's. Over, not called off.
        if (bot?.Self is not { } self || !_bouts.TryGetValue(self.Serial, out var bout))
        {
            return "the duel is over";
        }

        if (bout.Winner != null)
        {
            return bout.Why;
        }

        var a = Share(bout.A);
        var b = Share(bout.B);
        var deadA = bout.A is not { Deleted: false, Alive: true };
        var deadB = bout.B is not { Deleted: false, Alive: true };

        if (deadA || deadB)
        {
            Deaths++;
            Decide(bout, deadA ? bout.B : bout.A, deadA ? bout.A : bout.B, "the other fell");
        }
        else if (a <= YieldAt || b <= YieldAt)
        {
            Yielded++;
            Decide(bout, a >= b ? bout.A : bout.B, a >= b ? bout.B : bout.A, "the other yielded");
        }
        else if (Core.TickCount - bout.Began >= CapMs)
        {
            Called++;
            Decide(bout, a >= b ? bout.A : bout.B, a >= b ? bout.B : bout.A, "the clock ran out");
        }

        return bout.Winner == null ? null : bout.Why;
    }

    private static void Decide(Bout bout, BotMobile winner, BotMobile loser, string how)
    {
        bout.Winner = winner;
        bout.Why = $"{winner?.Name} won the duel: {how}";

        // <b>Swords down this instant, not on the next beat.</b> The engine swings on Combatant whether or not a
        // deed is advancing, and a duellist below the failing share has its brawl set aside by the ladder — so the
        // loser would go on swinging at a winner whose reflex answers every blow, until somebody's deed came round.
        Disarm(bout.A);
        Disarm(bout.B);

        logger.Information(
            "{Winner} beat {Loser} in a duel ({How}) after {Seconds}s, standing at {WinnerShare:P0} against {LoserShare:P0}",
            winner?.Name,
            loser?.Name,
            how,
            (Core.TickCount - bout.Began) / 1000,
            Share(winner),
            Share(loser)
        );
    }

    private static void Disarm(Mobile m)
    {
        if (m is { Deleted: false })
        {
            m.Combatant = null;
            m.Warmode = false;
        }
    }

    /// <summary>
    /// Calls a duel that has stalled — one of the two lost the other, or nobody is swinging — on health as it
    /// stands. For whoever holds the championship: the brawls end on their own rule and this is the rule from
    /// outside.
    /// </summary>
    public static bool Call(Mobile m, string why)
    {
        if (m == null || !_bouts.TryGetValue(m.Serial, out var bout) || bout.Winner != null)
        {
            return false;
        }

        var a = Share(bout.A);
        var b = Share(bout.B);

        Called++;
        Decide(bout, a >= b ? bout.A : bout.B, a >= b ? bout.B : bout.A, why ?? "it was called");

        return true;
    }

    /// <summary>
    /// Decides a bout nobody fought — the two could not reach each other — by the rule of the clock, health as it stands, but not as
    /// a win: not counted with the clock's calls, and said without "beat" or a share of hits (07.10.2026, reach28 reviews). For the
    /// arena's referee once a pair put back beside each other has lost each other again.
    /// </summary>
    public static bool NoFight(Mobile m, string why)
    {
        if (m == null || !_bouts.TryGetValue(m.Serial, out var bout) || bout.Winner != null)
        {
            return false;
        }

        var goes = Share(bout.A) >= Share(bout.B) ? bout.A : bout.B;
        var other = ReferenceEquals(goes, bout.A) ? bout.B : bout.A;

        NoFights++;

        bout.Winner = goes;
        bout.Why = $"no fight with {other?.Name}: {why ?? "the two could not reach each other"}";

        Disarm(bout.A);
        Disarm(bout.B);

        logger.Information(
            "No fight between {A} and {B} after {Seconds}s ({Why}); {Goes} goes on by the rule of the clock, not on hits",
            bout.A?.Name,
            bout.B?.Name,
            (Core.TickCount - bout.Began) / 1000,
            why ?? "the two could not reach each other",
            goes?.Name
        );

        return true;
    }

    /// <summary>Bouts decided as no fight. See <see cref="NoFight"/>.</summary>
    public static long NoFights { get; private set; }

    /// <summary>The winner of this one's duel once it is decided, or null while it runs or when there is no duel.</summary>
    public static BotMobile Winner(Mobile m) =>
        m != null && _bouts.TryGetValue(m.Serial, out var bout) ? bout.Winner : null;

    /// <summary>Whether this one's duel has been decided.</summary>
    public static bool Decided(Mobile m) => Winner(m) != null;

    /// <summary>Takes the pair off the record. Called by whoever set the duel up, once it has read the result.</summary>
    public static void End(Mobile a, Mobile b)
    {
        if (a != null)
        {
            _bouts.Remove(a.Serial);
        }

        if (b != null)
        {
            _bouts.Remove(b.Serial);
        }
    }

    /// <summary>Calls every standing duel off. A new world, or a championship abandoned.</summary>
    public static void Forget()
    {
        _bouts.Clear();
        Begun = 0;
        Yielded = 0;
        Called = 0;
        Deaths = 0;
        Pressed = 0;
        NoFights = 0;
    }

    public static string Describe() =>
        Begun == 0
            ? "no duel has been fought"
            : $"{Begun} duels fought, {Yielded} decided by a yield at {YieldAt:P0}, {Called} by the clock at {CapMs / 1000}s, {Deaths} with a death in them, {Pressed} pressed again by a referee, {NoFights} decided as no fight; {_bouts.Count / 2} standing";
}
