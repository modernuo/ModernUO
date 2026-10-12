using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// Seeking out a personal enemy to have it out: a walk to the one this bot counts an enemy (<see cref="BotTies.DuelAt"/>),
/// ended by a challenge to a duel when it is reached. Unpaid work on the Free rung, priced at <see cref="Worth"/> a minute and
/// leaned by the bot's nature (a vindictive bot ×1.15, a forgiving one ×0.85), so only a bot with little better to do goes.
///
/// <para>
/// <b>The walk ends by queueing the challenge, never by making it.</b> A duel presses a brawl on both bots, and pressing work
/// on the bot whose own deed is being advanced would settle that deed from inside its own step; so the meeting is handed to
/// <see cref="BotGrudges"/>, whose clock makes the challenge within <c>BotCharacterModule.TickMs</c>.
/// </para>
/// </summary>
public sealed class BotGrudge : BotDeed
{
    public const string Trade = "grudge";

    /// <summary>What seeking out an enemy is reckoned at a minute. Low: it pays nothing, and paying work should win.</summary>
    public static double Worth { get; set; } = 15.0;

    /// <summary>How long the seeking may take before it is given up. Four minutes.</summary>
    public static int GiveUpMs { get; set; } = 240000;

    private readonly BotMobile _enemy;

    private readonly Map _map;

    private readonly long _began;

    private Point3D _aim;

    public BotGrudge(BotMobile enemy)
    {
        _enemy = enemy;
        _map = enemy?.Map;
        _aim = enemy?.Location ?? Point3D.Zero;
        _began = Core.TickCount;
    }

    public BotMobile Enemy => _enemy;

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _aim;

    public override double Expects => Worth;

    public override double Minutes => 3.0;

    public override bool Unpaid => true;

    public override double Coin => 0.0;

    public override string Stage => $"seeking out {_enemy?.Name ?? "an enemy"} to have it out";

    public override bool Repeats(BotDeed other) => other is BotGrudge grudge && ReferenceEquals(grudge._enemy, _enemy);

    public override void Taken(IBotWilful bot) => BotGrudges.Sought++;

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        if (_enemy is not { Deleted: false, Alive: true } || _enemy.Map != _map)
        {
            return BotDoing.Failed($"{_enemy?.Name ?? "the enemy"} is gone");
        }

        if (Core.TickCount - _began >= GiveUpMs)
        {
            return BotDoing.Failed($"could not catch up with {_enemy.Name}");
        }

        if (body.InRange(_enemy.Location, BotGrudges.Reach))
        {
            BotGrudges.Queue(body as BotMobile, _enemy);

            return BotDoing.Done($"found {_enemy.Name} to have it out");
        }

        // Re-aimed only when the enemy has gone a plan's worth of steps from the aim: the walk order must be stable between ticks.
        if (Math.Max(Math.Abs(_enemy.X - _aim.X), Math.Abs(_enemy.Y - _aim.Y)) >= BotBrawl.Restride)
        {
            _aim = _enemy.Location;
        }

        return BotDoing.Walk(_map, _aim, BotArrival.Within(Math.Max(1, BotGrudges.Reach - 2)), $"to {_enemy.Name}");
    }
}

/// <summary>Offers a bot the seeking out of its worst enemy, when it has one in the world and both are free to fight.</summary>
public sealed class BotGrudgeProposer : IBotProposer
{
    public string Name => "Grudge";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot) => BotGrudges.Propose(bot);
}

/// <summary>
/// The duels of personal feuds: who may challenge whom, the challenge, and the end of it.
///
/// <para>
/// <b>Patrick's idea of 07.10.2026: "the one who killed you in a war is a personal enemy you seek out for a duel".</b> A bot
/// may challenge another it counts at <see cref="BotTies.DuelAt"/> or worse when the two meet — sought out
/// (<see cref="BotGrudge"/>) or met by chance, looked for on <see cref="LookEveryMs"/> among the pairs at enmity — and only when
/// both are free: in the world, alive, at <see cref="FitAt"/> of their health, in no company, on no committed work, not
/// duelling, not traders (neutral), not outlaws or jailed, not in a guarded town, no creature menacing either, and their
/// guilds not at war (a war is fought by the war's own rules, not by duels). A refusal is counted by its reason and puts the
/// pair on a short rest (<see cref="RefusedRestMs"/>); a challenge puts it on a long one (<see cref="PairEveryMs"/>).
/// </para>
///
/// <para>
/// <b>Nobody dies for a grudge.</b> The duel is <see cref="BotDuel"/>'s: yielded at its share of health, called at its clock,
/// and the two cannot be reported as criminals for it. When it is decided the feud is settled: each thinks
/// <see cref="BotTies.Settled"/> better of the other, the winner and the loser each remember it, and both say so.
/// </para>
/// </summary>
public static class BotGrudges
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotGrudges));

    public static bool On { get; set; } = true;

    /// <summary>How near the two must be for a challenge. Eight tiles.</summary>
    public static int Reach { get; set; } = 8;

    /// <summary>How far an enemy may be and still be sought out. Ninety tiles.</summary>
    public static int SeekReach { get; set; } = 90;

    /// <summary>The share of its health each must have. Four fifths, the hunt's own bar.</summary>
    public static double FitAt { get; set; } = 0.8;

    /// <summary>How long a pair rests after a challenge. An hour.</summary>
    public static int PairEveryMs { get; set; } = 3600000;

    /// <summary>How long a pair rests after a challenge that could not be made. Five minutes.</summary>
    public static int RefusedRestMs { get; set; } = 300000;

    /// <summary>How often the pairs at enmity are looked at for a chance meeting. Thirty seconds.</summary>
    public static int LookEveryMs { get; set; } = 30000;

    /// <summary>The chance a bot challenges an enemy it meets by chance: vindictive, of no leaning, forgiving.</summary>
    public static double VindictiveChance { get; set; } = 1.0;

    public static double PlainChance { get; set; } = 0.5;

    public static double ForgivingChance { get; set; } = 0.2;

    /// <summary>Seeking-out deeds offered and taken.</summary>
    public static long Offered { get; private set; }

    public static long Sought { get; internal set; }

    public static long Met { get; private set; }

    public static long Challenges { get; private set; }

    public static long Begun { get; private set; }

    public static long Settled { get; private set; }

    public static long Declined { get; private set; }

    private static readonly Dictionary<string, long> _refused = new(StringComparer.Ordinal);

    private static readonly Dictionary<(string, string), long> _pairs = [];

    private static readonly List<(BotMobile A, BotMobile B)> _queue = [];

    private sealed class Bout
    {
        public BotMobile A;

        public BotMobile B;

        public long Began;
    }

    private static readonly List<Bout> _bouts = [];

    private static long _lookTick;

    private static readonly List<BotLife> _feuding = [];

    private static (string, string) Key(Mobile a, Mobile b) =>
        string.CompareOrdinal(a.Name, b.Name) <= 0 ? (a.Name, b.Name) : (b.Name, a.Name);

    private static bool Resting((string, string) key, long now) => _pairs.TryGetValue(key, out var until) && now - until < 0;

    /// <summary>The proposer's answer: a walk to this bot's worst enemy in reach, or nothing. Most bots have no enemy and pay one comparison.</summary>
    public static BotDeed Propose(IBotWilful bot)
    {
        if (!On || !BotTies.On || bot?.Self is not BotMobile body || BotLives.Of(body) is not { } life || life.Worst > BotTies.DuelAt)
        {
            return null;
        }

        if (Unfit(body) != null)
        {
            return null;
        }

        var now = Core.TickCount;
        BotMobile worst = null;
        var worstRegard = BotTies.DuelAt;

        for (var i = 0; i < life.Ties.Count; i++)
        {
            var tie = life.Ties[i];

            if (tie.Regard > worstRegard || BotLives.Body(tie.Name) is not { Alive: true } enemy || enemy.Map != body.Map
                || !body.InRange(enemy.Location, SeekReach) || Resting(Key(body, enemy), now))
            {
                continue;
            }

            worst = enemy;
            worstRegard = tie.Regard;
        }

        if (worst == null || Unfit(worst) != null || Apart(body, worst) != null)
        {
            return null;
        }

        Offered++;

        return new BotGrudge(worst);
    }

    /// <summary>A meeting the seeking walk made. The challenge is made on the clock, outside the walker's own step.</summary>
    public static void Queue(BotMobile a, BotMobile b)
    {
        if (a != null && b != null)
        {
            _queue.Add((a, b));
        }
    }

    /// <summary>Why this bot is not free to fight a duel now, or null.</summary>
    private static string Unfit(BotMobile m)
    {
        if (m is not { Deleted: false, Alive: true } || m.Map == null || m.Map == Map.Internal)
        {
            return "not in the world";
        }

        if (m.HitsMax <= 0 || m.Hits < m.HitsMax * FitAt)
        {
            return "hurt";
        }

        if (m.Squad != null)
        {
            return "in a company";
        }

        if (BotDuel.Duelling(m))
        {
            return "duelling already";
        }

        if (BotTrading.Neutral(m) || BotTrading.Passes(m))
        {
            return "a trader";
        }

        if (BotOutlaw.Outlaw(m) || BotOutlaw.Jailed(m) || m.Murderer)
        {
            return "an outlaw";
        }

        if (m.Resolve?.Deed is { } deed && (deed.Committed || deed.Summons) && deed is not BotGrudge)
        {
            return "on committed work";
        }

        if (m.Combatant != null)
        {
            return "in a fight";
        }

        return null;
    }

    /// <summary>Why these two may not duel where they are, or null.</summary>
    private static string Apart(BotMobile a, BotMobile b)
    {
        if (a.Guild is Guild ga && b.Guild is Guild gb && ga != gb && (BotRegard.AtWar(ga, gb) || BotWar.Of(ga.Name, gb.Name) != null))
        {
            return "their guilds are at war";
        }

        if (Guarded(a) || Guarded(b))
        {
            return "in a guarded town";
        }

        return null;
    }

    private static bool Guarded(Mobile m) => m.Region?.GetRegion<GuardedRegion>() is { } guarded && !guarded.IsDisabled();

    /// <summary>
    /// The clock: queued meetings challenged, chance meetings among the pairs at enmity looked for, and standing duels read for
    /// their end. Called by <see cref="BotCharacterModule"/>.
    /// </summary>
    public static void Tick(long now)
    {
        for (var i = 0; i < _queue.Count; i++)
        {
            Met++;
            Challenge(_queue[i].A, _queue[i].B, sought: true, now);
        }

        _queue.Clear();

        Judge(now);

        if (!On || !BotTies.On || now - _lookTick < Math.Max(5000, LookEveryMs))
        {
            return;
        }

        _lookTick = now;

        // Taken out first: a challenge presses work on two bots, and nothing pressed may find the register changing under a walk.
        _feuding.Clear();

        foreach (var life in BotLives.All)
        {
            if (life.Worst <= BotTies.DuelAt)
            {
                _feuding.Add(life);
            }
        }

        for (var f = 0; f < _feuding.Count; f++)
        {
            var life = _feuding[f];

            if (BotLives.Body(life.Name) is not { Alive: true } a)
            {
                continue;
            }

            for (var i = 0; i < life.Ties.Count; i++)
            {
                var tie = life.Ties[i];

                if (tie.Regard > BotTies.DuelAt || BotLives.Body(tie.Name) is not { Alive: true } b || b.Map != a.Map
                    || !a.InRange(b.Location, Reach))
                {
                    continue;
                }

                var traits = life.Traits;
                var chance = (traits & BotTrait.Vindictive) != 0 ? VindictiveChance
                    : (traits & BotTrait.Forgiving) != 0 ? ForgivingChance
                    : PlainChance;

                if (Resting(Key(a, b), now))
                {
                    continue;
                }

                if (Utility.RandomDouble() >= chance)
                {
                    Declined++;
                    _pairs[Key(a, b)] = now + RefusedRestMs;

                    continue;
                }

                Met++;

                if (Challenge(a, b, sought: false, now))
                {
                    break;
                }
            }
        }
    }

    private static bool Challenge(BotMobile a, BotMobile b, bool sought, long now)
    {
        if (!On || a == null || b == null || ReferenceEquals(a, b))
        {
            return false;
        }

        Challenges++;

        var key = Key(a, b);
        var why = a.Map != b.Map || !a.InRange(b.Location, Reach + 2) ? "too far apart"
            : Unfit(a) is { } mine ? $"the challenger is {mine}"
            : Unfit(b) is { } theirs ? $"the challenged is {theirs}"
            : Apart(a, b) ?? (BotThreat.Anything(a, BotMobile.NoticeRange) ? "a creature is near" : null);

        if (why != null)
        {
            _refused[why] = _refused.GetValueOrDefault(why) + 1;
            _pairs[key] = now + RefusedRestMs;

            return false;
        }

        _pairs[key] = now + PairEveryMs;

        var regard = BotTies.Regard(a, b);
        var fill = new Dictionary<string, string> { ["other"] = b.Name };

        BotVoice.Say(a, "local", BotVoice.Phrase("character:challenge", null, a, fill) ?? $"{b.Name}! You and I have a score to settle. Draw!", force: true);

        fill["other"] = a.Name;
        BotVoice.Say(b, "local", BotVoice.Phrase("character:accept", null, b, fill) ?? $"So be it, {a.Name}.", force: true);

        if (!BotDuel.Begin(a, b, a.Bond?.Weapon?.Skill ?? SkillName.Wrestling, b.Bond?.Weapon?.Skill ?? SkillName.Wrestling))
        {
            _refused["the duel could not be set up"] = _refused.GetValueOrDefault("the duel could not be set up") + 1;

            return false;
        }

        Begun++;
        _bouts.Add(new Bout { A = a, B = b, Began = now });

        logger.Information(
            "{A} challenged {B} to a duel over their feud ({How}): {A} thinks {Regard} of {B}, {B} {Back} of {A}; {Traits}",
            a.Name,
            b.Name,
            sought ? "sought out" : "met by chance",
            regard,
            BotTies.Regard(b, a),
            BotTraits.Words(BotTraits.Of(a))
        );

        BotEvents.Post("duel", a, $"challenged {b.Name} to a duel over their feud, regard {regard}");

        return true;
    }

    /// <summary>Reads each standing feud duel for its end, calls one that has stood too long, and settles the feud.</summary>
    private static void Judge(long now)
    {
        for (var i = _bouts.Count - 1; i >= 0; i--)
        {
            var bout = _bouts[i];
            var winner = BotDuel.Winner(bout.A);

            if (winner == null && now - bout.Began >= BotDuel.CapMs + 15000)
            {
                BotDuel.Call(bout.A, "the feud's duel stood too long");
                winner = BotDuel.Winner(bout.A);
            }

            if (winner == null)
            {
                // Both off the record: one died, and the bout was cleared. The one standing has it.
                if (!BotDuel.InBout(bout.A) && !BotDuel.InBout(bout.B))
                {
                    winner = bout.A is { Deleted: false, Alive: true } ? bout.A : bout.B;
                }
                else
                {
                    continue;
                }
            }

            _bouts.RemoveAt(i);
            BotDuel.End(bout.A, bout.B);

            var loser = ReferenceEquals(winner, bout.A) ? bout.B : bout.A;
            var how = $"{winner.Hits} hits to {loser.Hits} after {(now - bout.Began) / 1000}s";

            BotTies.Mutual(winner, loser, BotTies.Settled, "a feud fought out in a duel");
            BotBiography.Dueled(winner, loser, how);
            Settled++;

            BotVoice.Say(winner, "local", BotVoice.Phrase("character:won", null, winner, new Dictionary<string, string> { ["other"] = loser.Name }) ?? $"That settles it, {loser.Name}.", force: true);
            BotVoice.Say(loser, "local", BotVoice.Phrase("character:lost", null, loser, new Dictionary<string, string> { ["other"] = winner.Name }) ?? $"You have it, {winner.Name}. We're square.", force: true);

            logger.Information(
                "{Winner} beat {Loser} in a duel over their feud ({How}); the feud is settled: {Winner} thinks {RegardW} of {Loser} now, {Loser} {RegardL}",
                winner.Name,
                loser.Name,
                how,
                BotTies.Regard(winner, loser),
                BotTies.Regard(loser, winner)
            );
        }
    }

    private static string DescribeRefused()
    {
        if (_refused.Count == 0)
        {
            return "none";
        }

        var said = "";

        foreach (var (why, n) in _refused)
        {
            said += (said.Length == 0 ? "" : ", ") + $"{why} {n}";
        }

        return said;
    }

    public static string Describe() =>
        !On
            ? "grudge duels are off"
            : $"grudges: {Offered} seekings offered, {Sought} taken, {Met} meetings, {Declined} let pass by nature, {Challenges} challenges ({Begun} duels begun, {Settled} settled, {_bouts.Count} standing; refused: {DescribeRefused()})";

    public static void Forget()
    {
        Offered = 0;
        Sought = 0;
        Met = 0;
        Challenges = 0;
        Begun = 0;
        Settled = 0;
        Declined = 0;
        _refused.Clear();
        _pairs.Clear();
        _queue.Clear();
        _bouts.Clear();
    }
}
