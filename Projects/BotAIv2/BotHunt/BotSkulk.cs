using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Practising being unseen: hiding again and again where it stands, and once it hides well enough for the engine to allow
/// it, moving a few quiet steps at a time.
///
/// <para>
/// <b>Patrick's order of 17.09.2026: criminals train their stealth.</b> A fighter tempted to rob that cannot hide yet
/// (<see cref="BotShadow.RobHiding"/>) is pressed into this instead of a robbery — his choice, over robbing openly — and a
/// bot that has ever practised is pressed back into it every <see cref="EveryMs"/>. Its Hiding above nought is the only mark
/// a crook carries that outlives a restart, because <see cref="BotProgress"/> keeps every skill a bot has any of. The rate is
/// the engine's own: a use every ten seconds, a tenth of a point when the gain roll comes up, which is some six or seven hours
/// of practice from nothing to the eighty that Stealth wants.
/// </para>
/// </summary>
public sealed class BotSkulk : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSkulk));

    public const string Trade = "skulk";

    /// <summary>How long one stint of practice lasts. Five minutes: thirty uses of the skill.</summary>
    public static int StintMs { get; set; } = 300000;

    /// <summary>How often a bot that has practised before is pressed back into practice. Half an hour; nought never.</summary>
    public static int EveryMs { get; set; } = 1800000;

    /// <summary>
    /// How many of the island's best hiders still short of a robbery's threshold practise in earnest on
    /// <see cref="EagerEveryMs"/> instead of the half-hourly itch. Nought puts everybody back on the itch.
    ///
    /// <para>
    /// <b>Patrick's words were "the most skilled and cunning".</b> Hiding is gained at the engine's own rate, which he
    /// chose over an accelerated one, and that rate falls off a cliff at ten: the nine stints that ended together at
    /// 22:31:40 on 17.09.2026 gained between 0.2 and 1.2 apiece against the 2.5 to 8.5 of the stints below ten. At one
    /// stint a half hour that is thirteen hours from eleven to the thirty a robbery wants, and The Shadow could not be
    /// founded in an evening. The dial that must not move is the gain; the dial that may is how hard a bot that has
    /// chosen this path works at it. A handful practise like apprentices — a stint, a breath, another stint — and the
    /// rest keep the idle itch, so the island loses six pairs of hands and not sixty.
    /// </para>
    ///
    /// <para>
    /// <b>Withdrawn the same night, on Patrick's word.</b> "Let the thieves train like ordinary bots as far as skills go;
    /// we are looking at the long run." So this is nought, and every bot with any hiding keeps the half-hourly itch: the
    /// road to the thirty a robbery wants is thirteen hours and is meant to be. The machinery is left standing behind
    /// the dial, because the reckoning above is still true and he may yet want it.
    /// </para>
    /// </summary>
    public static int Apprentices { get; set; }

    /// <summary>How often an apprentice is pressed back into practice. Five minutes: one stint after another.</summary>
    public static int EagerEveryMs { get; set; } = 300000;

    /// <summary>The most apprentices the dial may name, so the short list can be an array and never an allocation.</summary>
    private const int MostApprentices = 32;

    /// <summary>How far from where the stint began a quiet step may take it.</summary>
    public static int Wander { get; set; } = 4;

    /// <summary>
    /// What a stint is held at against other work, a minute: the robbery's own, because it is what the thought of robbery
    /// became. Unpaid work is weighed at its typed claim (<c>BotAppraisal</c>), so this is a priority and not a wage.
    ///
    /// <para>
    /// <b>Practice at one a minute lasted fifteen seconds.</b> The first stint on build 112, Pell's at 20:37:22 on 17.09.2026,
    /// was "jumped by hunt at 87/min after 0.3 of 5.0 minutes": work that will not wait — a creature in reach — is set
    /// against the work in hand with no margin and no hold, and a fighter out prowling always has a creature in reach.
    /// Something actually hitting the bot is a rung above and still takes it off practice.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 400.0;

    /// <summary>Stints begun.</summary>
    public static long Stints { get; private set; }

    /// <summary>Stints that ran to their clock.</summary>
    public static long Ended { get; private set; }

    /// <summary>Stints pressed by the half-hour urge rather than by a temptation.</summary>
    public static long Urged { get; private set; }

    /// <summary>Of those, the ones pressed on an apprentice's five-minute clock.</summary>
    public static long Eager { get; private set; }

    /// <summary>
    /// How long an itch that has come due waits for the work in hand to end before it presses anyway. Ten minutes.
    ///
    /// <para>
    /// <b>The itch dropped what the bot was doing, and nothing about practice is that urgent.</b> On build 198 (05:45–06:31,
    /// 24.09.2026) 21 stints were pressed and 14 of them dropped the work in hand — prowls, hunts, and Ilsa's inscribing five
    /// scrolls into a stint of six — eleven of the window's 27 drops. Every hider's clock starts at the same boot, so they
    /// fell due together (06:17:27: Ilsa, Aric, Kelda at once). A player practises after the fight, not in the middle of it.
    /// So a due itch waits, and is scratched between two pieces of work (BotWill's beat asks, before the auction hands out
    /// the next one); only one kept waiting longer than this presses as before. Nought presses at once, as it used to.
    /// </para>
    /// </summary>
    public static int WaitMs { get; set; } = 600000;

    /// <summary>Stints begun between two pieces of work after the itch had waited for the one in hand to end.</summary>
    public static long Waited { get; private set; }

    private static readonly HashSet<Serial> _waiting = [];

    /// <summary>Hiding gained over every stint that ran to its clock.</summary>
    public static double HidingGained { get; private set; }

    /// <summary>Stealth gained over every stint that ran to its clock.</summary>
    public static double StealthGained { get; private set; }

    private static readonly Dictionary<Serial, long> _urgedTick = [];

    private static readonly HashSet<Serial> _apprentices = [];

    private static readonly Serial[] _best = new Serial[MostApprentices];

    private static readonly double[] _bestAt = new double[MostApprentices];

    private readonly Map _map;

    /// <summary>Where the stint practises; moved to wherever the bot stands when the stint is taken up again. See <see cref="Resumed"/>.</summary>
    private Point3D _where;

    private bool _started;

    private long _began;

    /// <summary>When the stint was put down, so that <see cref="Resumed"/> can move its clock by the pause. See <see cref="Paused"/>.</summary>
    private long _pausedAt;

    /// <summary>Stints not taken up again because the bot came back somewhere it could not practise. See <see cref="Stale"/>.</summary>
    public static long StaleResumes { get; private set; }

    private double _hiding;

    private double _stealth;

    private Point3D _aim = Point3D.Zero;

    public BotSkulk(Map map, Point3D where)
    {
        _map = map;
        _where = where;
    }

    public override string Kind => Trade;

    public override bool Committed => true;

    /// <summary>
    /// Put down for what will not wait and taken up again, rather than dropped (12.10.2026, w2). A stint is five minutes of
    /// standing still, and a company's call cut 373 of them short in 10.2 h on 11.10 — "dropped skulk: pressed to enlist" was
    /// 80 % of every skulk dropped and the largest single kind among the shard's drops. Not steadfast: a real offer still
    /// outbids practice, as it should. See <see cref="Resumed"/> for what a stint taken up again does.
    /// </summary>
    public override bool Resumes => true;

    public override bool Unpaid => true;

    /// <summary>Standing still, hidden, saying the same thing for five minutes is what practice looks like. It ends on its own clock.</summary>
    public override bool Still => true;

    /// <summary>A bot already practising is not pressed into practice again: the urge and the temptation both lead here.</summary>
    public override bool Repeats(BotDeed other) => other is BotSkulk;

    /// <summary>A hidden bot that runs is revealed.</summary>
    public override bool Hurries => false;

    /// <summary>A hidden bot that rides is revealed.</summary>
    public override bool Afoot => true;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => Prior;

    public override double Minutes => StintMs / 60000.0;

    public override SkillName? Trains => SkillName.Hiding;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override int Made => 0;

    public override string Stage => "practising hiding";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || body.Map != _map)
        {
            return BotDoing.Failed("no body");
        }

        var now = Core.TickCount;

        // Somewhere else to practise, such as The Shadow's hideout: go there first, and the stint's clock starts on arrival.
        if (!_started && !body.InRange(_where, 8))
        {
            return BotDoing.Walk(_map, _where, BotArrival.Within(4), "to practise out of sight");
        }

        if (!_started)
        {
            _started = true;
            _began = now;
            _hiding = body.Skills.Hiding.Base;
            _stealth = body.Skills.Stealth.Base;
            _urgedTick[body.Serial] = now;
            Stints++;
        }

        if (now - _began >= StintMs)
        {
            return Finish(body);
        }

        var quietly = BotShadow.MayStealth(body);

        // Off the horse to practise moving hidden: the engine reveals a rider at its first step.
        if (quietly && body.Mounted)
        {
            BotStable.Alight(body);
        }

        // A quiet step in hand is taken, somewhere near where the stint began.
        if (quietly && body.Hidden && body.AllowedStealthSteps > 0)
        {
            if (_aim == Point3D.Zero || body.InRange(_aim, 0))
            {
                _aim = Aim();
            }

            if (_aim != Point3D.Zero)
            {
                return BotDoing.Walk(_map, _aim, BotArrival.Within(0), "moving quietly");
            }
        }

        if (!BotShadow.Ready(body))
        {
            return BotDoing.Work("keeping still");
        }

        if (quietly && body.Hidden)
        {
            BotShadow.Quiet(body);
            _aim = Point3D.Zero;

            return BotDoing.Work("trying to move quietly");
        }

        BotShadow.Hide(body);

        return BotDoing.Work("trying to hide");
    }

    public override void Drop(IBotWilful bot)
    {
        if (bot?.Self is { Hidden: true } body)
        {
            body.RevealingAction();
        }
    }

    public override void Paused(IBotWilful bot) => _pausedAt = Core.TickCount;

    /// <summary>
    /// A stint taken up again practises where the bot now stands, with its clock moved by the pause (the Steadfast contract: a
    /// clock inside the work is put right, not restarted — restarted, a stint jumped by a flight every couple of minutes would
    /// never end, and <see cref="Committed"/> keeps the review from bidding it away). Not moved, the clock would have run out
    /// in the company and the first beat back would have "practised" five minutes of fighting. <see cref="Aim"/> picks its quiet
    /// steps round where the stint began, which may be a company's field away, so that place moves too. A stint not yet begun —
    /// The Shadow's member on its way to the hideout — keeps its destination. Whether here is a place to practise at all is
    /// <see cref="Stale"/>'s question, asked before this.
    /// </summary>
    public override void Resumed(IBotWilful bot)
    {
        if (bot?.Self is not { } body)
        {
            return;
        }

        var hideout = BotUnderworld.Hideout;

        if (_started || hideout == Point3D.Zero || _where != hideout)
        {
            _where = body.Location;
        }

        _aim = Point3D.Zero;

        if (_started)
        {
            _began += Core.TickCount - _pausedAt;
        }
    }

    /// <summary>
    /// Why a stint put down for a company is not taken up where the company left the bot (12.10.2026, w2, review B). Companies
    /// end where they stand — a withdrawal with the creature that beat them still there, a delve party buried in the Orc Caves —
    /// and a stint taken up there is five minutes of standing still, revealed as often as not, in the one place a bot should not.
    /// A bot sees with its own eyes: a cell, a dungeon, the zones' ground that is not clear, or anything hostile within
    /// <see cref="BotBolt.Watch"/> tiles refuses the stint with its own words; the itch presses another one in half an hour,
    /// somewhere else. Counted in <see cref="StaleResumes"/> and in <c>BotWill.KeptStale</c>.
    /// </summary>
    public override string Stale(IBotWilful bot)
    {
        if (bot?.Self is not { } body || body.Map is not { } map || map == Map.Internal)
        {
            return null;
        }

        string why;

        if (BotOutlaw.Jailed(body))
        {
            why = "the bot is in a cell";
        }
        else if (BotDungeon.Under(body.Location))
        {
            why = "the company left it underground";
        }
        else if (BotZones.LevelAt(map, body.X, body.Y) is var level && level != BotZoneLevel.Clear)
        {
            why = level switch
            {
                BotZoneLevel.Edge => "it stands on the fringe of hostile ground",
                BotZoneLevel.Hostile => "it stands on hostile ground",
                _ => "it stands on deadly ground"
            };
        }
        else if (BotThreat.ThreatPower(body, BotBolt.Watch) > 0.0)
        {
            why = $"something hostile stands within {BotBolt.Watch} tiles";
        }
        else
        {
            return null;
        }

        StaleResumes++;

        return $"no place to practise hiding: {why}";
    }

    private BotDoing Finish(Mobile body)
    {
        Ended++;

        var hiding = body.Skills.Hiding.Base;
        var stealth = body.Skills.Stealth.Base;

        HidingGained += Math.Max(0.0, hiding - _hiding);
        StealthGained += Math.Max(0.0, stealth - _stealth);

        if (body.Hidden)
        {
            body.RevealingAction();
        }

        logger.Information(
            "{Name} practised hiding for {Minutes} minutes: Hiding {From:F1} to {To:F1}, Stealth {StealthFrom:F1} to {StealthTo:F1}",
            body.Name,
            StintMs / 60000,
            _hiding,
            hiding,
            _stealth,
            stealth
        );

        return BotDoing.Done($"practised hiding: Hiding {hiding:F1}, Stealth {stealth:F1}");
    }

    /// <summary>A tile a quiet step or two away, near where the stint began, that a mobile may stand on; or nowhere.</summary>
    private Point3D Aim()
    {
        for (var i = 0; i < 8; i++)
        {
            var x = _where.X + Utility.RandomMinMax(-Wander, Wander);
            var y = _where.Y + Utility.RandomMinMax(-Wander, Wander);
            var z = _map.GetAverageZ(x, y);

            if (_map.CanSpawnMobile(x, y, z))
            {
                return new Point3D(x, y, z);
            }
        }

        return Point3D.Zero;
    }

    /// <summary>
    /// Presses a bot that has practised before back into practice once every <see cref="EveryMs"/>. Asked for each fighter on
    /// <see cref="BotRobber"/>'s sweep, which is the same population the thought of robbery comes to.
    /// </summary>
    public static void Urge(BotMobile body, long now)
    {
        if (!BotShadow.Running || EveryMs <= 0 || body is not { Deleted: false, Alive: true, Fallen: false } || body.Skills.Hiding.Base <= 0.0)
        {
            return;
        }

        // The fence has no use for hiding: it walks into the towns the band cannot, in the open, as itself.
        if (BotFence.Is(body))
        {
            return;
        }

        var hiding = body.Skills.Hiding;
        var stealth = body.Skills.Stealth;

        if (hiding.Base >= hiding.Cap && (!BotShadow.MayStealth(body) || stealth.Base >= stealth.Cap))
        {
            return;
        }

        // The first sweep after a boot starts the clock rather than pressing the whole underworld at once.
        if (!_urgedTick.TryGetValue(body.Serial, out var last))
        {
            _urgedTick[body.Serial] = now;

            return;
        }

        var apprentice = _apprentices.Contains(body.Serial);
        var every = EveryMs;

        if (BotUnderworld.Member(body))
        {
            every = BotUnderworld.MemberPracticeEveryMs;
        }
        else if (apprentice)
        {
            every = EagerEveryMs;
        }

        if (now - last < every)
        {
            return;
        }

        // Due, and the bot is in the middle of something: the clock is left where it is and the itch is scratched the next
        // time the bot's hands are empty. See WaitMs.
        if (body.Resolve?.Deed != null && now - last < every + WaitMs)
        {
            _waiting.Add(body.Serial);

            return;
        }

        _urgedTick[body.Serial] = now;

        // Nor over an enlistment on its way to its company, nor over a stint already put down beneath whatever took the bot off
        // it (12.10.2026, w2): a stint resumes now, and a second one pressed on top would drop the work in hand to make room.
        if (body.Map is not { } map || map == Map.Internal || body.Squad != null || BotDuel.Duelling(body) || BotOutlaw.Jailed(body)
            || body.Resolve?.Deed is BotSkulk or BotRob or BotLieLow or BotBrawl or BotManhunt or BotSentence or BotEnlist
            || body.Resolve?.Paused?.Deed is BotSkulk)
        {
            return;
        }

        if (BotLadder.Standing(body) is not (BotStanding.Free or BotStanding.Busy))
        {
            return;
        }

        // The Shadow practises at its hideout, out of everybody's sight.
        var where = BotUnderworld.Member(body) && BotUnderworld.Hideout != Point3D.Zero ? BotUnderworld.Hideout : body.Location;

        var waited = _waiting.Remove(body.Serial) && body.Resolve?.Deed == null;

        if (BotWill.Press(body, new BotSkulk(map, where), apprentice ? "practising in earnest" : "the itch to practise hiding"))
        {
            Urged++;

            if (waited)
            {
                Waited++;
            }

            if (apprentice)
            {
                Eager++;
            }
        }
    }

    /// <summary>
    /// Names the island's most promising thieves-to-be, once a sweep: the <see cref="Apprentices"/> best hiders who
    /// cannot yet rob. They are pressed back into practice every <see cref="EagerEveryMs"/> and everybody else on the
    /// half-hourly itch, so the road to The Shadow is walked by a few in earnest instead of by everybody at a crawl.
    ///
    /// <para>
    /// Re-reckoned every sweep from the roster as it stands, so a bot that reaches the threshold, is taken into The
    /// Shadow, dies or is jailed leaves the list of its own accord and the next best takes its place. The short list is
    /// two arrays and a set held between sweeps; nothing is allocated.
    /// </para>
    /// </summary>
    public static void Rank(IReadOnlyList<BotMobile> bots)
    {
        if (bots == null)
        {
            return;
        }

        _apprentices.Clear();

        var want = Math.Clamp(Apprentices, 0, MostApprentices);

        if (!BotShadow.Running || want == 0)
        {
            return;
        }

        var held = 0;

        for (var i = 0; i < bots.Count; i++)
        {
            var body = bots[i];

            if (!Fits(body))
            {
                continue;
            }

            var hiding = body.Skills.Hiding.Base;

            if (hiding <= 0.0 || hiding >= BotShadow.RobHiding)
            {
                continue;
            }

            int at;

            if (held < want)
            {
                at = held++;
            }
            else if (hiding > _bestAt[want - 1])
            {
                at = want - 1;
            }
            else
            {
                continue;
            }

            while (at > 0 && _bestAt[at - 1] < hiding)
            {
                _bestAt[at] = _bestAt[at - 1];
                _best[at] = _best[at - 1];
                at--;
            }

            _bestAt[at] = hiding;
            _best[at] = body.Serial;
        }

        for (var i = 0; i < held; i++)
        {
            _apprentices.Add(_best[i]);
        }
    }

    /// <summary>Whether this bot could take the thief's road at all: the same classes the temptation itself reads.</summary>
    private static bool Fits(BotMobile body)
    {
        if (body is not { Deleted: false, Alive: true, Fallen: false } || body.Class is not { } klass)
        {
            return false;
        }

        if (klass.Role is BotRole.Producer or BotRole.Medic || klass.Unpaid || klass.Leads)
        {
            return false;
        }

        return !BotOutlaw.Jailed(body) && !BotUnderworld.Member(body);
    }

    public static string Describe() =>
        $"{Stints} stints of practising hiding ({Urged} of them from the itch: {Waited} of those begun between two pieces of work after waiting for the one in hand, {Eager} an apprentice's; {_waiting.Count} itches waiting now, {_apprentices.Count} apprentices named), {Ended} run to the end, gaining {HidingGained:F1} Hiding and {StealthGained:F1} Stealth between them; {StaleResumes} put down for a company and not taken up where it left the bot";

    public static void Forget()
    {
        Stints = 0;
        Ended = 0;
        Urged = 0;
        Waited = 0;
        _waiting.Clear();
        Eager = 0;
        HidingGained = 0;
        StealthGained = 0;
        StaleResumes = 0;
        _urgedTick.Clear();
        _apprentices.Clear();
    }
}
