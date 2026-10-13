using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Logging;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// How long a bot takes to answer what happens to it and to its own: the stopwatch between an event and the first decision
/// that answers it (06.10.2026, react14; Patrick: "the bots must make decisions about a fight and about help instantly").
///
/// <para>
/// <b>An instrument first, and the one that was there lied.</b> <c>BotCry</c> printed "the first helper setting out 1.1s after
/// the cry on average", and the cry's stamp is rewritten by every renewal — a bot in trouble renews its cry on every turn — so
/// that figure was the age of the last renewal, never more than a turn or two, whatever the wait had been. It also counted an
/// offer as a helper setting out: <c>BotRescuer.Propose</c> stamps the cry answered when it <em>offers</em> the rescue, and the
/// auction may still turn the offer down for the work in hand. Nothing else on the shard timed a reaction at all.
/// </para>
///
/// <para>
/// <b>Seven events, each with its own clock.</b> The clock starts at the engine's own event — a blow in <c>OnDamage</c>, a
/// creature that has taken the bot as its combatant moving in sight of it (<c>OnMovement</c>), health crossing a line, the
/// first cry of a bout, the end of a fight — and stops at the first decision that answers it: a fight, a flight or a mending
/// taken (<c>BotWill.Commit</c>, <c>Press</c>, a resumption), the reflex that squares the bot up to its attacker, a company
/// that already has an enemy, a helper taking the rescue, a healer beginning the heal. Every event ends in exactly one bucket:
/// the bot was already doing what answers it, it answered (the spread), it never did and lived, or it died with nothing
/// decided. A decision that came at the event itself is a zero in the spread and is counted beside it, so a median of nought
/// can be read for what it is.
/// </para>
///
/// <para>
/// <b>Cheap by construction.</b> Driven by events the engine already raises; a dictionary lookup a blow. Two spatial queries
/// exist, each once per bout and never per beat: who of ours was in earshot at the first cry, and whether anybody able to mend
/// stood in reach when a bot became a patient — without them "nobody came" cannot be told from "nobody was there".
/// </para>
/// </summary>
public static class BotReflex
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotReflex));

    /// <summary>Whether the stopwatch runs. Off, every hook returns at once and the line reads nought.</summary>
    public static bool On { get; set; } = true;

    /// <summary>
    /// One unanswered cry with somebody in earshot in this many is said in the log with who was in earshot at the cry: its class,
    /// how far off, what it held and which company it was in (12.10.2026, w5). The counters said 1 142 such cries in a night
    /// and could not say who stood by; this is the sample the next analysis reads. Twenty-five: forty-odd lines a night. Nought: none.
    /// </summary>
    public static int SampleEvery { get; set; } = 25;

    /// <summary>How long an event may stand unanswered before it is counted as never answered. Half a minute.</summary>
    public static int LapseMs { get; set; } = 30000;

    /// <summary>How far off a creature set on a bot is reckoned in its sight: a screen, as a player has.</summary>
    public static int Sight { get; set; } = 18;

    /// <summary>How long a creature seen coming may go unseen before the bout is closed as lost interest. Ten seconds.</summary>
    public static int LostMs { get; set; } = 10000;

    /// <summary>How an event was answered. The spread is over all of them; the split says which decision it was.</summary>
    private enum How
    {
        Fought,
        SquaredUp,
        Company,
        Ran,
        Mended,
        Drank,
        FellIn,
        Rescue,
        Quarry,
        SetOut,
        Healed,
        Work,
        Resumed,
        Turned,
        Cast,
        Stood,
        Spared,
        Count
    }

    private static readonly string[] Hows =
    [
        "took the fight", "squared up at once", "left to a company already fighting", "ran", "took a mending",
        "drank", "fell in with a company", "took the rescue", "went for what was on it", "set out to mend it",
        "began the heal", "took new work", "took up what it had put down", "its company turned on it", "began the cast",
        "took the stand", "a company spared it members"
    ];

    /// <summary>The upper edges of the spread's buckets, in milliseconds. The last bucket is everything past the last edge.</summary>
    private static readonly int[] Edges =
        [100, 200, 300, 400, 500, 750, 1000, 1500, 2000, 3000, 4000, 6000, 8000, 10000, 15000, 20000, 30000, 45000, 60000];

    /// <summary>Latencies, kept as counts by bucket so that a median and a ninth decile can be read without keeping every one.</summary>
    private sealed class Spread
    {
        private readonly long[] _n = new long[Edges.Length + 1];

        public long Count { get; private set; }

        /// <summary>Answers given at the event itself, on the same tick. Zeroes in the spread.</summary>
        public long AtOnce { get; private set; }

        public long WorstMs { get; private set; }

        public double SumMs { get; private set; }

        public void Add(long ms)
        {
            if (ms < 0)
            {
                ms = 0;
            }

            var at = Edges.Length;

            for (var i = 0; i < Edges.Length; i++)
            {
                if (ms <= Edges[i])
                {
                    at = i;

                    break;
                }
            }

            _n[at]++;
            Count++;
            SumMs += ms;

            if (ms == 0)
            {
                AtOnce++;
            }

            if (ms > WorstMs)
            {
                WorstMs = ms;
            }
        }

        /// <summary>
        /// The upper edge of the bucket the <paramref name="q"/>-th share of the answers ends in: "no more than". Nought when
        /// that share answered at the event itself; the worst seen for the open-ended last bucket; minus one when empty.
        /// </summary>
        public long Quantile(double q)
        {
            if (Count == 0)
            {
                return -1;
            }

            var want = Math.Max(1L, (long)Math.Ceiling(Count * q));

            if (AtOnce >= want)
            {
                return 0;
            }

            var seen = 0L;

            for (var i = 0; i < _n.Length; i++)
            {
                seen += _n[i];

                if (seen >= want)
                {
                    return i < Edges.Length ? Math.Min(Edges[i], WorstMs) : WorstMs;
                }
            }

            return WorstMs;
        }

        public void Write(Utf8JsonWriter w, string name)
        {
            w.WritePropertyName(name);
            w.WriteStartObject();
            w.WriteNumber("count", Count);
            w.WriteNumber("atOnce", AtOnce);
            w.WriteNumber("medianMs", Quantile(0.5));
            w.WriteNumber("p90Ms", Quantile(0.9));
            w.WriteNumber("worstMs", WorstMs);
            w.WriteNumber("meanMs", Count > 0 ? Math.Round(SumMs / Count, 0) : 0.0);
            w.WritePropertyName("buckets");
            w.WriteStartArray();

            for (var i = 0; i < _n.Length; i++)
            {
                w.WriteNumberValue(_n[i]);
            }

            w.WriteEndArray();
            w.WriteEndObject();
        }

        public void Clear()
        {
            Array.Clear(_n);
            Count = 0;
            AtOnce = 0;
            WorstMs = 0;
            SumMs = 0.0;
        }

        /// <summary>"median ≤400ms, p90 ≤3.0s, worst 14.2s over 212 (57 at once)", or "none".</summary>
        public string Say() =>
            Count == 0
                ? "none"
                : $"median {Span(Quantile(0.5), true)}, p90 {Span(Quantile(0.9), true)}, worst {Span(WorstMs, false)} over {Count} ({AtOnce} at once)";

        private static string Span(long ms, bool bound) =>
            ms <= 0 ? "0" : ms < 1000 ? $"{(bound ? "≤" : "")}{ms}ms" : $"{(bound ? "≤" : "")}{ms / 1000.0:F1}s";
    }

    /// <summary>One kind of event: how many, and the one bucket each ended in.</summary>
    private sealed class Tally
    {
        public Tally(string key) => Key = key;

        public string Key { get; }

        public long Events;

        /// <summary>The bot was already doing what answers it when the event came: no decision was wanted.</summary>
        public long Already;

        public readonly Spread Answered = new();

        public readonly long[] By = new long[(int)How.Count];

        /// <summary>Lapsed with nothing decided, the bot alive.</summary>
        public long Never;

        /// <summary>The bot died with nothing decided.</summary>
        public long NeverDied;

        /// <summary>The bot died within the bout though it had answered.</summary>
        public long DiedAnyway;

        public void Answer(long ms, How how)
        {
            Answered.Add(ms);
            By[(int)how]++;
        }

        public void Clear()
        {
            Events = 0;
            Already = 0;
            Never = 0;
            NeverDied = 0;
            DiedAnyway = 0;
            Answered.Clear();
            Array.Clear(By);
        }

        public string Split()
        {
            using var sb = ValueStringBuilder.Create(128);
            var said = 0;

            for (var i = 0; i < By.Length; i++)
            {
                if (By[i] == 0)
                {
                    continue;
                }

                if (said++ > 0)
                {
                    sb.Append(", ");
                }

                sb.Append($"{By[i]} {Hows[i]}");
            }

            return said == 0 ? "" : $" [{sb.ToString()}]";
        }

        public void Write(Utf8JsonWriter w)
        {
            w.WritePropertyName(Key);
            w.WriteStartObject();
            w.WriteNumber("events", Events);
            w.WriteNumber("already", Already);
            w.WriteNumber("never", Never);
            w.WriteNumber("neverDied", NeverDied);
            w.WriteNumber("diedAnyway", DiedAnyway);
            Answered.Write(w, "answered");
            w.WritePropertyName("by");
            w.WriteStartObject();

            for (var i = 0; i < By.Length; i++)
            {
                if (By[i] > 0)
                {
                    w.WriteNumber(Hows[i], By[i]);
                }
            }

            w.WriteEndObject();
        }
    }

    /// <summary>Struck by a creature, and by a bot: two events, because nothing that answers the one answers the other.</summary>
    private static readonly Tally _struck = new("struck");

    private static readonly Tally _assailed = new("struckByBot");

    /// <summary>A creature set on the bot seen coming, before its first blow.</summary>
    private static readonly Tally _menaced = new("seenComing");

    /// <summary>Health crossing the line the ladder calls failing.</summary>
    private static readonly Tally _failing = new("failing");

    /// <summary>The first cry of a bout, to the first of ours taking it up.</summary>
    private static readonly Tally _cry = new("cry");

    /// <summary>A bot becoming a patient with somebody able to mend in reach, to that somebody deciding to.</summary>
    private static readonly Tally _patient = new("patient");

    /// <summary>A fight or a flight ending, to the next thing taken.</summary>
    private static readonly Tally _over = new("fightOver");

    /// <summary>Of the struck bouts nothing answered, those in which the bot had judged itself outmatched and walked on.</summary>
    private static long _struckOutmatched;

    /// <summary>Creatures seen coming whose first blow landed before the bot had decided anything, and the warning it had had.</summary>
    private static readonly Spread _warning = new();

    /// <summary>Creatures seen coming that lost interest, or were left behind, with neither a blow nor a decision.</summary>
    private static long _lost;

    /// <summary>Tiles off, summed, at which a creature set on a bot was first seen coming.</summary>
    private static long _seenOff;

    /// <summary>Of the failing bouts nothing answered, those in which the bot held a fight of its own and fought on.</summary>
    private static long _failingFoughtOn;

    /// <summary>Failing bouts ended by somebody else's heal before the bot had decided anything.</summary>
    private static long _failingHelped;

    /// <summary>First cries with nobody of ours able to fight within the cry's reach, and from a bot in a company.</summary>
    private static long _cryDeaf;

    private static long _cryCompanied;

    /// <summary>First cries from a member whose company already had what was on it for its enemy: counted with the already.</summary>
    private static long _cryCompanyOn;

    /// <summary>Helpers that took a cry up, all told: the first is the reaction, the rest are company.</summary>
    private static long _helpers;

    /// <summary>Unanswered cries by whether anybody was there to hear: the true failures are the first.</summary>
    private static long _cryNeverHeard;

    /// <summary>
    /// Of the unanswered, cries that lapsed with the crier alive and whole: above <c>BotCry.DireAt</c> of its health, the thing it
    /// cried about dead or gone. A bot that won its fight inside the cry's span needed nobody; counted beside the never, never
    /// taken out of it (12.10.2026, w5).
    /// </summary>
    private static long _cryWhole;

    /// <summary>Bots that became patients with nobody able to mend within reach: not events, a denominator.</summary>
    private static long _patientAlone;

    /// <summary>Patients that mended themselves before anybody else had decided to.</summary>
    private static long _patientSelf;

    /// <summary>From becoming a patient to somebody else's heal beginning on it (the cast aimed, the bandage begun).</summary>
    private static readonly Spread _healed = new();

    /// <summary>Fights that ended for a bot in a company: the company's to say what next, so no clock is started.</summary>
    private static long _overCompany;

    /// <summary>Everything timed for one bot. One object a bot, made at its first event.</summary>
    private sealed class State
    {
        public bool StruckOpen;
        public bool StruckAnswered;
        public bool StruckByBot;
        public bool Outmatched;
        public long StruckTick;
        public long BlowTick;

        public bool MenaceOpen;
        public bool MenaceWarned;
        public bool FailCompany;
        public bool FailCaster;
        public Serial MenaceBy;
        public long MenaceTick;
        public long MenaceSeenTick;

        public bool FailOpen;
        public bool FailAnswered;
        public bool FailFighting;
        public long FailTick;

        public bool CryOpen;
        public bool CryAnswered;
        public bool CryDeaf;
        public long CryTick;
        public long CryLastTick;
        public Serial CryWhat;

        /// <summary>The nearest of ours able to fight at the first cry: who, how far, what it held, which company. For the sample.</summary>
        public Serial CryNearest;
        public int CryNearOff;
        public string CryNearHeld;
        public int CryNearSquad;

        public bool HurtOpen;
        public bool HurtDecided;
        public long HurtTick;

        public bool OverOpen;
        public long OverTick;
    }

    private static readonly Dictionary<Serial, State> _states = [];

    /// <summary>Which crier each creature is on, by the creature: so a hunt taken against it reads as help for the crier.</summary>
    private static readonly Dictionary<Serial, Serial> _crierByFoe = [];

    private static State Of(Serial serial)
    {
        if (!_states.TryGetValue(serial, out var state))
        {
            _states[serial] = state = new State();
        }

        return state;
    }

    private static State Find(Mobile m) => m != null && _states.TryGetValue(m.Serial, out var state) ? state : null;

    /// <summary>
    /// Something took health off this bot. Called from <c>OnDamage</c> before the health is taken off, whatever did it: a
    /// poison's tick crosses a line as surely as a blow.
    /// </summary>
    public static void Damaged(BotMobile bot, int amount, Mobile from, bool willKill)
    {
        if (!On || bot == null || bot.Deleted || !bot.Alive || willKill)
        {
            return;
        }

        var now = Core.TickCount;
        var s = Of(bot.Serial);

        Lapse(bot, s, now);

        if (bot.HitsMax > 0 && amount > 0)
        {
            var before = bot.Hits;
            var after = before - amount;
            var failAt = bot.HitsMax * BotLadder.Fraction(bot);

            if (before > failAt && after <= failAt)
            {
                Fails(bot, s, now);
            }

            var hurtAt = bot.HitsMax * BotMend.Hurt;

            if (before >= hurtAt && after < hurtAt)
            {
                Hurts(bot, s, now);
            }
        }

        if (from == null || from == bot)
        {
            return;
        }

        // The creature it had seen coming has landed its blow before anything was decided: the warning was not used.
        if (s.MenaceOpen && from.Serial == s.MenaceBy)
        {
            s.MenaceOpen = false;
            _warning.Add(now - s.MenaceTick);

            // Why nothing had been decided, one cause each, asked in the order that names the nearest one (06.10.2026, react16).
            var off = Math.Max(Math.Abs(from.X - bot.X), Math.Abs(from.Y - bot.Y));
            var reach = from is BaseCreature coming ? BotMobile.MenaceRangeOf(coming) : BotMobile.MenaceRange;

            if (s.MenaceWarned)
            {
                if (bot.Squad != null)
                {
                    _firstCompanyTold++;
                }
                else
                {
                    _firstWarned++;
                }
            }
            else if (bot.Squad != null)
            {
                _firstCompany++;
            }
            else if (off > reach)
            {
                _firstFar++;
            }
            else if (bot.Region?.IsPartOf<Server.Regions.TownRegion>() == true)
            {
                _firstTown++;
            }
            else
            {
                _firstInside++;
            }
        }

        if (s.StruckOpen)
        {
            s.BlowTick = now;

            return;
        }

        s.StruckOpen = true;
        s.StruckAnswered = false;
        s.StruckByBot = from is PlayerMobile;
        s.Outmatched = false;
        s.StruckTick = now;
        s.BlowTick = now;

        var tally = s.StruckByBot ? _assailed : _struck;

        tally.Events++;

        var deed = bot.Resolve?.Deed;

        if (deed is BotBolt || deed is BotBrawl || deed?.Foe != null)
        {
            // Running already, or in a fight of its own: the blow asks nothing new of it.
            tally.Already++;
            s.StruckAnswered = true;
        }
        else if (bot.Squad is { Focus: { Deleted: false, Alive: true } })
        {
            // Asked after BotSquads.Note has heard the blow: the company has an enemy, this one or another.
            tally.Answer(0, How.Company);
            s.StruckAnswered = true;
        }
    }

    /// <summary>What the bot made of the odds at a blow: outmatched is remembered, so a bout nothing answered can say it walked on.</summary>
    public static void Stood(BotMobile bot, BotStand stand)
    {
        if (!On || stand != BotStand.Outmatched || Find(bot) is not { StruckOpen: true, StruckAnswered: false } s)
        {
            return;
        }

        s.Outmatched = true;
    }

    /// <summary>The reflex squared the bot up to what struck it and sent it walking at it: a decision, and taken on the blow.</summary>
    public static void Engaged(BotMobile bot)
    {
        if (!On || Find(bot) is not { StruckOpen: true, StruckAnswered: false } s)
        {
            return;
        }

        s.StruckAnswered = true;
        (s.StruckByBot ? _assailed : _struck).Answer(Core.TickCount - s.StruckTick, How.SquaredUp);
    }

    /// <summary>First blows that landed with the warning unused, by why: see <see cref="Damaged"/> and the line.</summary>
    private static long _firstCompany;

    private static long _firstCompanyTold;

    private static long _firstWarned;

    private static long _firstFar;

    private static long _firstTown;

    private static long _firstInside;

    /// <summary>Failing bouts nothing answered, of a member of a company: the company's fight, which the member does not leave.</summary>
    private static long _failingNeverCompany;

    /// <summary>And of a lone bot with a caster the worst of what was round it: fought to the end, by order.</summary>
    private static long _failingNeverCaster;

    /// <summary>The bot (or its company) has been told of what is coming for it. Whatever follows, it was not for want of seeing.</summary>
    public static void Warned(BotMobile bot)
    {
        if (On && Find(bot) is { MenaceOpen: true } s)
        {
            s.MenaceWarned = true;
        }
    }

    /// <summary>
    /// A healer has begun a cast for this patient: the decision, which the heal follows by the cast's own time (a second to
    /// two and a half). Until react16 the stopwatch heard only the heal landing, so a medic's answer could never read "at once".
    /// </summary>
    public static void Casting(Mobile healer, Mobile patient)
    {
        if (!On || healer == null || healer == patient || Find(patient) is not { HurtOpen: true, HurtDecided: false } p)
        {
            return;
        }

        p.HurtDecided = true;
        _patient.Answer(Core.TickCount - p.HurtTick, How.Cast);
    }

    /// <summary>The same reflex before any blow: the bot has squared up to something seen coming. See <c>BotMobile.Menace</c>.</summary>
    public static void Met(BotMobile bot)
    {
        if (!On || Find(bot) is not { MenaceOpen: true } s)
        {
            return;
        }

        s.MenaceOpen = false;
        _menaced.Answer(Core.TickCount - s.MenaceTick, How.SquaredUp);
    }

    /// <summary>
    /// A creature that has this bot as its combatant moved within the bot's sight. Called from <c>OnMovement</c>, which the
    /// engine raises for every body near a mover; the caller has already asked the two cheap questions.
    /// </summary>
    public static void Seen(BotMobile bot, BaseCreature creature)
    {
        if (!On || bot == null || creature == null)
        {
            return;
        }

        var now = Core.TickCount;
        var s = Of(bot.Serial);

        if (s.MenaceOpen)
        {
            if (s.MenaceBy == creature.Serial)
            {
                s.MenaceSeenTick = now;

                return;
            }

            // One at a time: the first seen coming is the one timed, until it is answered, strikes or loses interest.
            if (now - s.MenaceSeenTick < LostMs)
            {
                return;
            }

            s.MenaceOpen = false;
            _lost++;
        }

        // Already being hit: whatever comes now is no warning of anything.
        if (s.StruckOpen && now - s.BlowTick < BotLadder.HuntedMs)
        {
            return;
        }

        if (creature.Controlled || creature.Summoned || !bot.InRange(creature.Location, Sight))
        {
            return;
        }

        var deed = bot.Resolve?.Deed;

        // Its own quarry coming at it, a flight already begun, its company's enemy, or a fight it is already swinging in.
        if (deed is BotBolt || ReferenceEquals(deed?.Foe, creature) || ReferenceEquals(bot.Squad?.Focus, creature)
            || ReferenceEquals(bot.Combatant, creature))
        {
            return;
        }

        if (!BotThreat.Hostile(bot, creature))
        {
            return;
        }

        s.MenaceOpen = true;
        s.MenaceWarned = false;
        s.MenaceBy = creature.Serial;
        s.MenaceTick = now;
        s.MenaceSeenTick = now;

        _menaced.Events++;
        _seenOff += Math.Max(Math.Abs(creature.X - bot.X), Math.Abs(creature.Y - bot.Y));
    }

    /// <summary>
    /// A piece of work came into a bot's hands: won at auction, pressed on it, or taken up again. The one place every clock
    /// that a decision stops is stopped.
    /// </summary>
    public static void Took(IBotWilful bot, BotDeed deed, bool resumed = false)
    {
        if (!On || deed == null || bot?.Self is not { } body)
        {
            return;
        }

        var now = Core.TickCount;

        if (_states.TryGetValue(body.Serial, out var s))
        {
            if (s.OverOpen)
            {
                s.OverOpen = false;
                _over.Answer(now - s.OverTick, resumed ? How.Resumed : How.Work);
            }

            // The stand is its own bucket (12.10.2026, w5): a fight carried on for want of a flight by a bot the odds refused, and
            // the one decision the failing rung can now take besides running, mending and drinking. Named apart so that the
            // share it adds to "answered" can be read off, never folded into "took the fight".
            var how = deed is BotBolt ? How.Ran
                : deed is BotSalve { OnSelf: true } ? How.Mended
                : deed is BotRescue { Own: true, ToTheEnd: true } ? How.Stood
                : deed is BotBrawl || deed.Foe != null ? How.Fought
                : deed is BotEnlist ? How.FellIn
                : How.Count;

            if (how != How.Count)
            {
                if (s.StruckOpen && !s.StruckAnswered)
                {
                    s.StruckAnswered = true;
                    (s.StruckByBot ? _assailed : _struck).Answer(now - s.StruckTick, how);
                }

                if (s.MenaceOpen && how != How.Mended && (how is not (How.Fought or How.Stood) || deed.Foe?.Serial == s.MenaceBy))
                {
                    s.MenaceOpen = false;
                    _menaced.Answer(now - s.MenaceTick, how);
                }

                if (s.FailOpen && !s.FailAnswered && how is How.Ran or How.Mended or How.Stood)
                {
                    s.FailAnswered = true;
                    _failing.Answer(now - s.FailTick, how);
                }
            }
        }

        // And the other half: this bot's decision is somebody else's answer.
        if (deed is BotRescue { Own: false, Friend: { } friend })
        {
            Helped(friend.Serial, now, How.Rescue);
        }
        else if (deed.Foe is { } foe && _crierByFoe.TryGetValue(foe.Serial, out var crier) && crier != body.Serial)
        {
            Helped(crier, now, How.Quarry);
        }

        if (deed is BotSalve { OnSelf: false, Patient: { } patient } && Find(patient) is { HurtOpen: true, HurtDecided: false } p)
        {
            p.HurtDecided = true;
            _patient.Answer(now - p.HurtTick, How.SetOut);
        }
    }

    /// <summary>A company has taken this for its enemy. If one of ours is crying about it, that cry is answered. Called by <c>BotSquad.Engage</c>.</summary>
    public static void CompanyOn(Mobile focus)
    {
        if (!On || focus == null || !_crierByFoe.TryGetValue(focus.Serial, out var crier))
        {
            return;
        }

        if (!_states.TryGetValue(crier, out var c) || !c.CryOpen || c.CryAnswered || c.CryWhat != focus.Serial)
        {
            return;
        }

        Helped(crier, Core.TickCount, How.Turned);
    }

    /// <summary>A company has spared members to this crier (<c>BotSquad.Spare</c>): the cry is answered. Until 12.10.2026 (w5) the spare told nobody and read as never.</summary>
    public static void Spared(Mobile crier)
    {
        if (On && crier != null)
        {
            Helped(crier.Serial, Core.TickCount, How.Spared);
        }
    }

    private static void Helped(Serial crier, long now, How how)
    {
        if (!_states.TryGetValue(crier, out var c) || !c.CryOpen || now - c.CryLastTick >= BotCry.HoldsMs)
        {
            return;
        }

        _helpers++;

        if (c.CryAnswered)
        {
            return;
        }

        c.CryAnswered = true;
        _cry.Answer(now - c.CryTick, how);
    }

    /// <summary>A piece of work ended. A fight or a flight that ended by itself starts the clock to whatever is taken next.</summary>
    public static void Ended(IBotWilful bot, BotDeed deed, BotEnding ending)
    {
        if (!On || deed == null || ending is not (BotEnding.Done or BotEnding.Failed) || bot?.Self is not { Alive: true } body)
        {
            return;
        }

        if (deed is not (BotBolt or BotBrawl) && deed.Foe == null)
        {
            return;
        }

        _over.Events++;

        if (bot.Squad != null)
        {
            _overCompany++;
            _over.Already++;

            return;
        }

        var s = Of(body.Serial);

        s.OverOpen = true;
        s.OverTick = Core.TickCount;
    }

    /// <summary>
    /// Somebody of ours has said it is being set upon. Called by <c>BotCry.Raise</c> on every renewal; a bout of crying is one
    /// event, from its first cry until the cries stop for as long as a cry holds.
    /// </summary>
    public static void Cried(Mobile who, Mobile what)
    {
        if (!On || who is not BotMobile bot || what == null)
        {
            return;
        }

        var now = Core.TickCount;
        var s = Of(who.Serial);

        if (s.CryOpen && now - s.CryLastTick >= BotCry.HoldsMs)
        {
            CloseCry(bot, s, false);
        }

        if (!s.CryOpen)
        {
            s.CryOpen = true;
            s.CryAnswered = false;
            s.CryTick = now;
            s.CryDeaf = !Heard(bot, s);

            _cry.Events++;

            if (s.CryDeaf)
            {
                _cryDeaf++;
            }

            if (bot.Squad != null)
            {
                _cryCompanied++;

                // <b>A company's answer is its enemy, and the first build of this stopwatch did not hear it (06.10.2026).</b> A
                // member takes no rescue and no hunt of its own — its company turns on what attacks it — so 80 of build 401's
                // first 130 cries came from members and could only ever read "never". A cry about the company's own enemy is
                // already being answered; a company that turns on it later answers it then (CompanyOn).
                if (ReferenceEquals(bot.Squad.Focus, what))
                {
                    s.CryAnswered = true;
                    _cry.Already++;
                    _cryCompanyOn++;
                }
            }
        }

        s.CryLastTick = now;

        if (s.CryWhat != what.Serial)
        {
            s.CryWhat = what.Serial;
            _crierByFoe[what.Serial] = who.Serial;
        }
    }

    /// <summary>
    /// Whether anybody of ours able to fight stands within the cry's reach, and who the nearest of them is. One query a bout of
    /// crying; the whole reach is walked rather than stopped at the first found, for the sample's sake (12.10.2026, w5).
    /// </summary>
    private static bool Heard(BotMobile crier, State s)
    {
        s.CryNearest = Serial.Zero;
        s.CryNearOff = int.MaxValue;
        s.CryNearHeld = null;
        s.CryNearSquad = 0;

        var map = crier.Map;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        foreach (var mobile in map.GetMobilesInRange<Mobile>(crier.Location, BotCry.Carries))
        {
            if (mobile == crier || mobile is not IBotAlly { AbleToFight: true })
            {
                continue;
            }

            var off = Math.Max(Math.Abs(mobile.X - crier.X), Math.Abs(mobile.Y - crier.Y));

            if (off >= s.CryNearOff)
            {
                continue;
            }

            s.CryNearest = mobile.Serial;
            s.CryNearOff = off;

            if (mobile is BotMobile near)
            {
                s.CryNearHeld = near.Resolve?.Deed?.Kind;
                s.CryNearSquad = near.Squad?.Id ?? 0;
            }
        }

        return s.CryNearOff != int.MaxValue;
    }

    private static void CloseCry(Mobile crier, State s, bool died)
    {
        s.CryOpen = false;

        if (s.CryAnswered)
        {
            if (died)
            {
                _cry.DiedAnyway++;
            }

            return;
        }

        if (died)
        {
            _cry.NeverDied++;
        }
        else
        {
            _cry.Never++;

            // Whole, and the thing it cried about dead or gone: it needed nobody. Beside the never, not out of it.
            if (crier is { Deleted: false, Alive: true, HitsMax: > 0 } && crier.Hits >= crier.HitsMax * BotCry.DireAt
                && World.FindMobile(s.CryWhat) is not { Deleted: false, Alive: true })
            {
                _cryWhole++;
            }
        }

        if (s.CryDeaf)
        {
            return;
        }

        _cryNeverHeard++;

        // The first, the (SampleEvery + 1)th, and so on: at one, every one (a remainder test against one never fires at one).
        if (SampleEvery <= 0 || (_cryNeverHeard - 1) % SampleEvery != 0 || crier == null)
        {
            return;
        }

        // The sample: who was nearest and able when the cry went up, what it holds now, and whose it is — Heard counts any able ally
        // within the cry's reach, a hostile guild's included, so without the guild and the hostility the next analysis could not tell
        // "nobody came" from "nobody of ours was there" (13.10.2026, w5 review B). See SampleEvery.
        var near = World.FindMobile(s.CryNearest);
        var foe = World.FindMobile(s.CryWhat);

        logger.Information(
            "A cry nobody answered: {Crier} cried about {Foe} and {Fate}; the nearest of ours able at the cry was {Near} the {Class} of {Guild}{Hostile} {Off} tiles off, holding {Held}{Company}, now holding {Now}{Squad}",
            crier.Name,
            foe?.Name ?? "something",
            died ? "died" : crier.HitsMax > 0 ? $"is at {crier.Hits / (double)crier.HitsMax:P0}" : "lives",
            near?.Name ?? "nobody found",
            (near as BotMobile)?.Class?.Name ?? "?",
            (near?.Guild as Guilds.Guild)?.Name ?? "no guild",
            near != null && BotRegard.Hostile(near, crier) ? " (hostile to the crier)" : "",
            s.CryNearOff,
            s.CryNearHeld ?? "nothing",
            s.CryNearSquad > 0 ? $" in company {s.CryNearSquad}" : "",
            (near as BotMobile)?.Resolve?.Deed?.ToString() ?? "nothing",
            (near as BotMobile)?.Squad is { } squad ? $" in company {squad.Id}" : ""
        );
    }

    /// <summary>Health has crossed the line the ladder calls failing.</summary>
    private static void Fails(BotMobile bot, State s, long now)
    {
        if (s.FailOpen)
        {
            return;
        }

        s.FailOpen = true;
        s.FailAnswered = false;
        s.FailTick = now;

        _failing.Events++;

        var deed = bot.Resolve?.Deed;

        s.FailFighting = deed?.Foe != null || deed is BotBrawl;
        s.FailCompany = bot.Squad != null;

        // The worst of what is round it a caster: no flight is offered from one (Patrick, 30.09: fight to the end), so a bout
        // nothing answers there is the order carried out, not a decision missed. One query a crossing.
        s.FailCaster = BotThreat.Strongest(bot, BotBolt.Watch) is { AI: AIType.AI_Mage };

        if (deed is BotBolt || deed is BotSalve { OnSelf: true })
        {
            _failing.Already++;
            s.FailAnswered = true;
        }
    }

    /// <summary>Health has crossed the line at which a bot is worth mending. A patient only if somebody near could mend it.</summary>
    private static void Hurts(BotMobile bot, State s, long now)
    {
        if (s.HurtOpen)
        {
            return;
        }

        var map = bot.Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        var able = false;

        foreach (var mobile in map.GetMobilesInRange<Mobile>(bot.Location, BotSurgeon.Reach))
        {
            if (mobile != bot && mobile is IBotAlly && mobile.Alive && (BotMend.Spell(mobile, bot) >= 0 || BotMend.Cloth(mobile) > 0))
            {
                able = true;

                break;
            }
        }

        if (!able)
        {
            _patientAlone++;

            return;
        }

        s.HurtOpen = true;
        s.HurtDecided = false;
        s.HurtTick = now;

        _patient.Events++;
    }

    /// <summary>A heal has begun: a cast aimed or a bandage started, on itself or on another. Called by <c>BotMend</c>.</summary>
    public static void Mending(Mobile healer, Mobile patient)
    {
        if (!On || healer == null || Find(patient) is not { } s)
        {
            return;
        }

        var now = Core.TickCount;

        if (healer == patient)
        {
            if (s.FailOpen && !s.FailAnswered)
            {
                s.FailAnswered = true;
                _failing.Answer(now - s.FailTick, How.Mended);
            }

            if (s.HurtOpen)
            {
                s.HurtOpen = false;

                if (!s.HurtDecided)
                {
                    _patientSelf++;
                }
            }

            return;
        }

        if (s.FailOpen && !s.FailAnswered)
        {
            // Somebody else's answer, not this bot's: the bout is over and nothing is added to the spread.
            s.FailOpen = false;
            _failingHelped++;
        }

        if (s.HurtOpen)
        {
            s.HurtOpen = false;

            if (!s.HurtDecided)
            {
                _patient.Answer(now - s.HurtTick, How.Healed);
            }

            _healed.Add(now - s.HurtTick);
        }
    }

    /// <summary>A bottle went down. Called by <c>BotMend.Swallow</c>.</summary>
    public static void Drank(Mobile bot)
    {
        if (!On || Find(bot) is not { FailOpen: true, FailAnswered: false } s)
        {
            return;
        }

        s.FailAnswered = true;
        _failing.Answer(Core.TickCount - s.FailTick, How.Drank);
    }

    /// <summary>The bot died. Whatever stood unanswered is counted as that; whatever it had answered, as died anyway.</summary>
    public static void Died(BotMobile bot)
    {
        if (!On || Find(bot) is not { } s)
        {
            return;
        }

        var now = Core.TickCount;

        if (s.StruckOpen && now - s.BlowTick < BotLadder.HuntedMs)
        {
            var tally = s.StruckByBot ? _assailed : _struck;

            if (s.StruckAnswered)
            {
                tally.DiedAnyway++;
            }
            else
            {
                tally.NeverDied++;
            }

            s.StruckOpen = false;
        }

        if (s.FailOpen)
        {
            if (s.FailAnswered)
            {
                _failing.DiedAnyway++;
            }
            else
            {
                _failing.NeverDied++;

                if (s.FailCompany)
                {
                    _failingNeverCompany++;
                }
                else if (s.FailCaster)
                {
                    _failingNeverCaster++;
                }
            }

            s.FailOpen = false;
        }

        if (s.CryOpen && now - s.CryLastTick < BotCry.HoldsMs)
        {
            CloseCry(bot, s, true);
        }

        if (s.HurtOpen)
        {
            if (s.HurtDecided)
            {
                _patient.DiedAnyway++;
            }
            else
            {
                _patient.NeverDied++;
            }

            s.HurtOpen = false;
        }

        if (s.MenaceOpen)
        {
            s.MenaceOpen = false;
            _menaced.NeverDied++;
        }

        if (s.OverOpen)
        {
            s.OverOpen = false;
            _over.NeverDied++;
        }

        Lapse(bot, s, now);
    }

    /// <summary>Closes whatever of this bot's has stood past its time. Asked at its next event and at every summary.</summary>
    private static void Lapse(Mobile bot, State s, long now)
    {
        if (s.StruckOpen && now - s.BlowTick >= BotLadder.HuntedMs)
        {
            s.StruckOpen = false;

            if (!s.StruckAnswered)
            {
                (s.StruckByBot ? _assailed : _struck).Never++;

                if (s.Outmatched)
                {
                    _struckOutmatched++;
                }
            }
        }

        if (s.MenaceOpen && now - s.MenaceSeenTick >= LostMs)
        {
            s.MenaceOpen = false;
            _lost++;
        }

        if (s.FailOpen && (now - s.FailTick >= LapseMs
                           || s.FailAnswered && bot is { HitsMax: > 0 } && bot.Hits > bot.HitsMax * BotLadder.Fraction(bot)))
        {
            s.FailOpen = false;

            if (!s.FailAnswered)
            {
                _failing.Never++;

                if (s.FailCompany)
                {
                    _failingNeverCompany++;
                }
                else if (s.FailCaster)
                {
                    _failingNeverCaster++;
                }

                if (s.FailFighting)
                {
                    _failingFoughtOn++;
                }
            }
        }

        if (s.CryOpen && now - s.CryLastTick >= BotCry.HoldsMs)
        {
            CloseCry(bot, s, false);
        }

        if (s.HurtOpen && now - s.HurtTick >= LapseMs)
        {
            s.HurtOpen = false;

            if (!s.HurtDecided)
            {
                _patient.Never++;
            }
        }

        if (s.OverOpen && now - s.OverTick >= LapseMs)
        {
            s.OverOpen = false;
            _over.Never++;
        }
    }

    /// <summary>Every bot's stale bouts closed, so that a summary counts what has lapsed since the bot's last event.</summary>
    private static void Sweep()
    {
        var now = Core.TickCount;

        foreach (var (serial, state) in _states)
        {
            Lapse(World.FindMobile(serial), state, now);
        }

        // The index of who cries about what is only ever a hint; emptied when it has grown, and refilled by the next cries.
        if (_crierByFoe.Count > 2048)
        {
            _crierByFoe.Clear();

            foreach (var state in _states.Values)
            {
                state.CryWhat = Serial.Zero;
            }
        }
    }

    private static string Line(Tally t, string name) =>
        $"{name}: {t.Events}, {t.Already} already at it, answered {t.Answered.Say()}{t.Split()}, never {t.Never + t.NeverDied} ({t.NeverDied} died with nothing decided), {t.DiedAnyway} died though they had answered";

    /// <summary>The <c>Reflexes:</c> line. Cumulative since the start, like every number in the summary.</summary>
    public static string Describe()
    {
        if (!On)
        {
            return "the stopwatch is off";
        }

        Sweep();

        return
            $"{Line(_struck, "struck by a creature")} ({_struckOutmatched} of the never had judged themselves outmatched and walked on)"
            + $"; {Line(_assailed, "struck by a bot")}"
            + $"; {Line(_menaced, $"a creature set on the bot seen coming within {Sight} tiles")}, first seen {(_menaced.Events > 0 ? _seenOff / (double)_menaced.Events : 0.0):F1} tiles off on average; its first blow landed before anything was decided {_warning.Count} times, the warning unused {_warning.Say()} (by why: {_firstCompany} on a member of a company that was not told, {_firstCompanyTold} on a member whose company was told, {_firstFar} struck from beyond the range the bot answers at, {_firstWarned} on a bot on the rung with nothing it could take, {_firstTown} in a town, {_firstInside} inside the range and never put on the rung); {_lost} lost interest or were left behind"
            + $"; {Line(_failing, "health crossing the failing line")} ({_failingNeverCompany} of the never were members of a company, {_failingNeverCaster} lone bots with a caster on them (fought to the end, by order), {_failingFoughtOn} fought on in a fight of their own, {_failingHelped} more were healed by another first)"
            + $"; {Line(_cry, "first cries")} ({_cryDeaf} with nobody of ours able within {BotCry.Carries} tiles, {_cryCompanied} from a bot in a company, {_cryCompanyOn} of them about the company's own enemy and counted already at it; {_cryNeverHeard} of the never had somebody in earshot, {_cryWhole} of the never lapsed with the crier whole and the thing dead or gone; {_helpers} helpers in all)"
            + $"; {Line(_patient, $"bots under {BotMend.Hurt:P0} with a healer within {BotSurgeon.Reach}")} ({_patientAlone} more had nobody able to mend in reach, {_patientSelf} mended themselves first); the heal itself begun {_healed.Say()}"
            + $"; {Line(_over, "fights and flights ended")} ({_overCompany} of the already were in a company, whose next move is the company's)";
    }

    /// <summary>The same counters as the <c>reflexes</c> block of <c>/api/state</c>.</summary>
    public static void Web(Utf8JsonWriter w)
    {
        Sweep();

        w.WritePropertyName("reflexes");
        w.WriteStartObject();
        w.WriteBoolean("on", On);
        w.WritePropertyName("edgesMs");
        w.WriteStartArray();

        for (var i = 0; i < Edges.Length; i++)
        {
            w.WriteNumberValue(Edges[i]);
        }

        w.WriteEndArray();

        _struck.Write(w);
        w.WriteNumber("neverOutmatched", _struckOutmatched);
        w.WriteEndObject();

        _assailed.Write(w);
        w.WriteEndObject();

        _menaced.Write(w);
        w.WriteNumber("lost", _lost);
        w.WriteNumber("firstSeenTiles", _menaced.Events > 0 ? Math.Round(_seenOff / (double)_menaced.Events, 1) : 0.0);
        _warning.Write(w, "struckFirst");
        w.WriteNumber("struckFirstCompanyNotTold", _firstCompany);
        w.WriteNumber("struckFirstCompanyTold", _firstCompanyTold);
        w.WriteNumber("struckFirstFromBeyond", _firstFar);
        w.WriteNumber("struckFirstOnTheRung", _firstWarned);
        w.WriteNumber("struckFirstInTown", _firstTown);
        w.WriteNumber("struckFirstInside", _firstInside);
        w.WriteEndObject();

        _failing.Write(w);
        w.WriteNumber("neverFoughtOn", _failingFoughtOn);
        w.WriteNumber("neverInACompany", _failingNeverCompany);
        w.WriteNumber("neverFromACaster", _failingNeverCaster);
        w.WriteNumber("healedByAnother", _failingHelped);
        w.WriteEndObject();

        _cry.Write(w);
        w.WriteNumber("nobodyInEarshot", _cryDeaf);
        w.WriteNumber("fromACompany", _cryCompanied);
        w.WriteNumber("aboutTheCompanysEnemy", _cryCompanyOn);
        w.WriteNumber("neverThoughHeard", _cryNeverHeard);
        w.WriteNumber("neverButWhole", _cryWhole);
        w.WriteNumber("helpers", _helpers);
        w.WriteEndObject();

        _patient.Write(w);
        w.WriteNumber("noHealerInReach", _patientAlone);
        w.WriteNumber("mendedItself", _patientSelf);
        _healed.Write(w, "healBegun");
        w.WriteEndObject();

        _over.Write(w);
        w.WriteNumber("inACompany", _overCompany);
        w.WriteEndObject();

        w.WriteEndObject();
    }

    /// <summary>Clears everything. Called with the cries' own counters on a world reload.</summary>
    public static void Forget()
    {
        _states.Clear();
        _crierByFoe.Clear();

        _struck.Clear();
        _assailed.Clear();
        _menaced.Clear();
        _failing.Clear();
        _cry.Clear();
        _patient.Clear();
        _over.Clear();

        _warning.Clear();
        _healed.Clear();

        _struckOutmatched = 0;
        _lost = 0;
        _seenOff = 0;
        _failingFoughtOn = 0;
        _failingHelped = 0;
        _cryDeaf = 0;
        _cryCompanied = 0;
        _firstCompany = 0;
        _firstCompanyTold = 0;
        _firstWarned = 0;
        _firstFar = 0;
        _firstTown = 0;
        _firstInside = 0;
        _failingNeverCompany = 0;
        _failingNeverCaster = 0;
        _cryCompanyOn = 0;
        _helpers = 0;
        _cryNeverHeard = 0;
        _cryWhole = 0;
        _patientAlone = 0;
        _patientSelf = 0;
        _overCompany = 0;
    }
}
