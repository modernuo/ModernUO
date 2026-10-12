using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// A healer standing by one of ours who is fighting, close enough to bind a wound the moment there is one.
///
/// <para>
/// <b>What a healer does while nobody is hurt yet.</b> Measured on the morning of 14.09.2026: healers spent 26% of
/// their working minutes looking for a fight and 5% in one, against 10% on their own trade. A medic that goes
/// looking for fights is a fifth fighter with a green staff, and one that stays at home is a healer nobody can reach.
/// The mending itself was never missing — <c>BotSurgeon</c> offers a wound to whoever can see it — what was missing
/// was a healer anywhere near the wounds. Games that ship a support role give it a place beside somebody rather than a
/// quarry of its own: Horizon Zero Dawn's herds hand out roles, Halo's squads hand out styles. This is the same shape
/// without an order in it: the healer weighs standing by a hunter against everything else it could do.
/// </para>
///
/// <para>
/// <b>Put down for the wound, taken up again after it.</b> Steadfast, so a salve — a summons — pauses this instead of
/// ending it, and when the wound is bound the healer walks back to its fighter. A support role is a string of
/// interruptions, and that is exactly the case the pause was built for: each wound must not be a new decision about
/// where to be.
/// </para>
///
/// <para>
/// Paid nothing by itself — the skill arrives through the mending it makes possible and is booked there — so it is
/// unpaid, and its proposer is its only gate: a healer, fit, with cloth or a heal and the herbs for it, a fighter of
/// ours actually engaged within reach, and nobody else already standing by that fighter.
/// </para>
/// </summary>
public sealed class BotAccompany : BotDeed
{
    /// <summary>The ledger's key.</summary>
    public const string Trade = "escort";

    /// <summary>
    /// What standing by a fighter is reckoned at per minute. Above looking for a fight and mending a scratch, below
    /// any trade that pays: a healer with a good mortar and herbs in its pack should still brew.
    /// </summary>
    public static double Prior { get; set; } = 45.0;

    /// <summary>The longest one stint of standing by lasts before the healer looks at its choices again. Five minutes.</summary>
    public static int LeaseMs { get; set; } = 300000;

    /// <summary>How near the fighter is near enough, in tiles. Well inside the reach a heal or a bandage needs.</summary>
    public static int Stay { get; set; } = 5;

    /// <summary>How long the fighter may be out of any fight before the stint is over. Twenty seconds.</summary>
    public static int GraceMs { get; set; } = 20000;

    /// <summary>How long a healer's claim on a fighter lasts without being renewed by the stint that made it.</summary>
    public static int ClaimMs { get; set; } = 15000;

    /// <summary>How often a stint re-asks whether the healer still has anything to heal with.</summary>
    public static int MeansEveryMs { get; set; } = 5000;

    /// <summary>
    /// How many times one stint may fall behind the fighter it is following before it admits it cannot keep up.
    ///
    /// <para>
    /// <b>Falling behind a fighter is what following one looks like.</b> A fighter chasing its quarry walks away from
    /// whoever walks after it, and the chase planner gives up after a dozen plans that did not close the distance.
    /// The first three stints on 14.09.2026 all ended that way inside ten seconds — "could not get nearer to Ulla" —
    /// because a deed that says nothing when its walk is refused is given up. Walking on is the whole of this stint's
    /// job, so it bends this many times and only then says it could not keep up.
    /// </para>
    /// </summary>
    public static int KeepUp { get; set; } = 6;

    /// <summary>
    /// How long a healer that could not keep up with a fighter is not offered that fighter again. Five minutes.
    ///
    /// <para>
    /// <b>Unpaid work learns nothing from failing, so its proposer has to remember for it.</b> On the evening of
    /// 14.09.2026 Otho the Healer was sent after Maeve 48 times and after Faron 16, every stint bending six times after a
    /// fighter it never closed on and ending "could not get nearer", and every failure put the same fighter back on offer
    /// at the next review, because nothing about the pair was written down. It is now, and it is read where the fighter
    /// is chosen.
    /// </para>
    /// </summary>
    public static int ShunMs { get; set; } = 300000;

    /// <summary>Beats spent standing within reach of a fighter.</summary>
    public static long Stood { get; private set; }

    /// <summary>Stints ended because the fighter's fight was over.</summary>
    public static long Over { get; private set; }

    /// <summary>Stints ended because the lease ran out.</summary>
    public static long Leased { get; private set; }

    /// <summary>Stints ended because the fighter died, was deleted or left the map.</summary>
    public static long Lost { get; private set; }

    /// <summary>Stints ended because the fighter turned to robbery, went red, or became somebody it is a crime to help. See <see cref="BotMend.Abetting"/>.</summary>
    public static long Turned { get; private set; }

    /// <summary>Stints ended because the healer had nothing left to heal with.</summary>
    public static long Spent { get; private set; }

    /// <summary>Times a healer fell behind its fighter and walked on after it instead of giving the stint up.</summary>
    public static long Lagged { get; private set; }

    /// <summary>Which healer is standing by which fighter, and when it last said so.</summary>
    private static readonly Dictionary<Serial, (Serial Healer, long Tick)> _escorted = [];

    /// <summary>Which healer lately could not keep up with which fighter, and when. See <see cref="ShunMs"/>.</summary>
    private static readonly Dictionary<(Serial Healer, Serial Fighter), long> _behind = [];

    /// <summary>Stints given up because the healer could not keep up, each of which set the pair aside.</summary>
    public static long Outrun { get; private set; }

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotAccompany));

    private readonly BotMobile _companion;

    private readonly Map _map;

    private readonly long _began;

    /// <summary>Gold a minute the fighter pays, or nought for the unpaid stint. See <see cref="BotRetainer"/>.</summary>
    private readonly int _wage;

    private long _quietTick;

    private long _meansTick;

    private long _paidTick;

    private int _earned;

    /// <summary>Whether the healer has come within the fighter's service once: the wage runs from then, not from the walk out.</summary>
    private bool _stoodOnce;

    private int _lagged;

    private bool _standing;

    /// <summary>Where the walk to the fighter is aimed: the fighter's tile as it was. See <see cref="Advance"/>.</summary>
    private Point3D _aim;

    /// <summary>Walks re-aimed at the fighter's new tile.</summary>
    public static long Reaimed { get; private set; }

    private string _escortedBy;

    public BotAccompany(BotMobile companion, Map map, int wage = 0)
    {
        _companion = companion;
        _map = map;
        _wage = Math.Max(0, wage);
        _began = Core.TickCount;
        _quietTick = _began;
        _paidTick = _began;

        // Seeded from a real tick and one interval back, so the first beat asks at once and the arithmetic never
        // leans on a zero that some hosts' tick counts pass straight through.
        _meansTick = _began - MeansEveryMs;
    }

    public override string Kind => Trade;

    /// <summary>Stands beside a fighter wherever it fights. See <see cref="BotDeed.Braves"/>.</summary>
    public override bool Braves => true;

    /// <summary>Seen through, and put down for a wound. See the class note.</summary>
    public override bool Steadfast => true;

    /// <summary>
    /// Unpaid in the ledger's sense whether or not a wage is paid. The wage is real coin in the healer's pack, but it is
    /// fixed and needs no learning — and a hired stint that learned would learn the wrong thing: measured 22:47–23:10 on
    /// 15.09.2026, Jorunn's paid stint settled at −110 coin (a levy in the middle of it), which as a lesson would have
    /// had the earnings veto refuse every hire after it. See <see cref="BotRetainer"/>.
    /// </summary>
    public override bool Unpaid => true;

    /// <summary>Standing by a fighter is standing still on purpose, and the lease is its own clock.</summary>
    public override bool Still => _standing;

    public override Map Map => _map;

    public override Point3D Where => _companion?.Location ?? Point3D.Zero;

    /// <summary>The prior, and the wage on top of it when there is one: the wage is gold a minute, the same unit.</summary>
    public override double Expects => Prior + _wage;

    public override double Minutes => LeaseMs / 60000.0;

    /// <summary>Healing, which is what the stint is for and what makes it a healer's own trade. See <c>BotCalling</c>.</summary>
    public override SkillName? Trains => SkillName.Healing;

    public override int Outlay => 0;

    /// <summary>Coin when a wage is paid, so a healer short of money is the one that takes the hire.</summary>
    public override double Coin => _wage > 0 ? 1.0 : 0.0;

    public override int Made => 0;

    public override string Stage =>
        (_standing ? $"standing by {_companion?.Name}" : $"on the way to stand by {_companion?.Name}")
        + (_wage > 0 ? $" in its pay at {_wage}gp a minute, {_earned}gp so far" : "");

    /// <summary>Whether this one of ours is in a fight right now.</summary>
    public static bool Engaged(BotMobile m) =>
        m is { Deleted: false, Alive: true }
        && (m.Combatant is { Deleted: false, Alive: true }
            || m.Squad is { Stance: BotSquadStance.Fighting }
            || m.Resolve?.Deed?.Kind is BotSlay.Trade or BotRescue.Trade or BotRally.Trade or BotQuarrel.Trade);

    /// <summary>
    /// Whether this healer has anything to heal with: cloth in the pack, or a heal in the book and the herbs for it.
    /// Mana is not asked: it comes back while the healer stands there. See <see cref="BotMend.Herbs"/>.
    /// </summary>
    public static bool Means(Mobile healer) =>
        BotMend.Cloth(healer) > 0 || BotMend.Herbs(healer, BotArsenal.SpellHeal);

    /// <summary>Whether another healer is already standing by this fighter.</summary>
    public static bool Taken(Mobile companion, Mobile healer)
    {
        if (companion == null || !_escorted.TryGetValue(companion.Serial, out var claim))
        {
            return false;
        }

        if (Core.TickCount - claim.Tick >= ClaimMs)
        {
            _escorted.Remove(companion.Serial);

            return false;
        }

        return claim.Healer != healer.Serial;
    }

    /// <summary>Whether this healer lately could not keep up with this fighter. See <see cref="ShunMs"/>.</summary>
    public static bool Behind(Mobile healer, Mobile companion)
    {
        if (healer == null || companion == null || !_behind.TryGetValue((healer.Serial, companion.Serial), out var tick))
        {
            return false;
        }

        if (Core.TickCount - tick >= ShunMs)
        {
            _behind.Remove((healer.Serial, companion.Serial));

            return false;
        }

        return true;
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        var name = _companion?.Name ?? "the fighter";

        if (_companion is not { Deleted: false, Alive: true } || _companion.Map != _map)
        {
            Lost++;

            return BotDoing.Done($"{name} is gone");
        }

        if (BotMend.ShunsOutlaws && BotMend.Abetting(body, _companion) is { } crime)
        {
            Turned++;

            return Settled($"{name} is {crime}, and standing by it would be abetting");
        }

        var now = Core.TickCount;

        if (now - _began >= LeaseMs)
        {
            Leased++;

            return Settled($"stood by {name} for {LeaseMs / 60000} minutes");
        }

        // The wage, a minute at a time, out of the fighter's pack. A fighter that cannot pay ends the stint: the healer
        // is not running a debt, and the fighter is not being followed for nothing under the name of a hire.
        // <b>In its service means within call, not within touch.</b> Measured 22:47–23:10 on 15.09.2026: 25 hires,
        // 20gp paid — a healer following a prowling fighter is nearly always a few tiles behind it, and a wage that
        // ran only while the healer stood within five tiles paid nothing for five minutes of keeping up. A minute
        // within BotRetainer.Near of the fighter is a minute served; a minute further off is neither paid nor held
        // against the fighter.
        var served = body.InRange(_companion, BotRetainer.Near);

        if (served && !_stoodOnce)
        {
            _stoodOnce = true;
            _paidTick = now;
        }

        if (_wage > 0 && _stoodOnce && now - _paidTick >= BotRetainer.PayEveryMs)
        {
            _paidTick = now;

            if (served)
            {
                if (!BotRetainer.Pay(_companion, body, _wage))
                {
                    return Settled($"{name} could not pay the next minute's wage");
                }

                _earned += _wage;
            }
            else
            {
                BotRetainer.Unserved++;
            }
        }

        // On a clock, because the herbs question builds a spell to read its reagents. See Means.
        if (now - _meansTick >= MeansEveryMs)
        {
            _meansTick = now;

            if (!Means(body))
            {
                Spent++;

                return BotDoing.Failed("nothing to heal with");
            }
        }

        _escorted[_companion.Serial] = (body.Serial, now);

        // A hired healer follows the fighter wherever it is out — hunting, prowling, in a company — and not only
        // while blows are being traded; the unpaid stint is over as soon as the fight is.
        var busy = _wage > 0 ? BotRetainer.Afield(_companion) : Engaged(_companion);
        var grace = _wage > 0 ? BotRetainer.GraceMs : GraceMs;

        if (busy)
        {
            _quietTick = now;
        }
        else if (now - _quietTick >= grace)
        {
            Over++;

            return Settled(_wage > 0 ? $"{name} went home" : $"{name}'s fight is over");
        }

        if (!body.InRange(_companion, Stay))
        {
            _standing = false;

            // <b>To where the fighter is, not after the fighter.</b> The third walk after a moving bot to fail the same way:
            // the house call's on the morning of 16.09.2026, the brawl's (build 83, "could not get nearer to Joss" three
            // robberies in three), and this one — 12 of build 94's escorts in its first half hour ended "could not get
            // nearer to Fendrel", "to Delwyn", "to Alden Ashdown", with the fighter walking in the open. An order after a
            // mobile changes with every step it takes, and the walker reads a changed order as a new route. The aim is the
            // fighter's tile, moved when the fighter has gone BotBrawl.Restride tiles from it, or when the healer has
            // reached it and the fighter is still not near — so arriving never becomes standing at an empty tile.
            if (_aim == Point3D.Zero || !Utility.InRange(_companion.Location, _aim, BotBrawl.Restride) || body.InRange(_aim, Stay - 1))
            {
                if (_aim != Point3D.Zero)
                {
                    Reaimed++;
                }

                _aim = _companion.Location;
            }

            return BotDoing.Walk(_map, _aim, BotArrival.Within(Stay - 1), $"to stand by {name}");
        }

        _standing = true;
        Stood++;

        return BotDoing.Work($"standing by {name}");
    }

    /// <summary>Done, and for a hired stint the log says what the fighter paid for. The counters are the caller's.</summary>
    private BotDoing Settled(string why)
    {
        if (_wage > 0)
        {
            logger.Information(
                "{Healer} stood by {Fighter} for {Minutes:F1} minutes at {Wage}gp a minute and was paid {Earned}gp — {Why}",
                _escortedBy ?? "a healer",
                _companion?.Name,
                (Core.TickCount - _began) / 60000.0,
                _wage,
                _earned,
                why
            );
        }

        return BotDoing.Done(why);
    }

    /// <summary>Which healer this is, for the log line at the end, and the hire's count.</summary>
    public override void Taken(IBotWilful bot)
    {
        _escortedBy = bot?.Self?.Name;

        if (_wage > 0)
        {
            BotRetainer.Began(bot?.Self, _companion, _wage);
        }
    }

    /// <summary>The walk after the fighter lost ground. Bent up to <see cref="KeepUp"/> times; see there for why.</summary>
    public override bool Bend(IBotWilful bot)
    {
        if (++_lagged > KeepUp)
        {
            if (_companion != null && bot?.Self is { } body)
            {
                _behind[(body.Serial, _companion.Serial)] = Core.TickCount;
                Outrun++;
            }

            return false;
        }

        Lagged++;
        _standing = false;

        return true;
    }

    /// <summary>Back from binding a wound: the quiet clock is from before it, and the healer is walking again.</summary>
    public override void Resumed(IBotWilful bot)
    {
        _quietTick = Core.TickCount;
        _standing = false;
    }

    public override void Drop(IBotWilful bot)
    {
        if (_companion != null && bot?.Self is { } body
            && _escorted.TryGetValue(_companion.Serial, out var claim) && claim.Healer == body.Serial)
        {
            _escorted.Remove(_companion.Serial);
        }
    }

    public static void Forget()
    {
        Stood = 0;
        Over = 0;
        Leased = 0;
        Lost = 0;
        Turned = 0;
        Spent = 0;
        Lagged = 0;
        Reaimed = 0;
        Outrun = 0;
        _escorted.Clear();
        _behind.Clear();
    }
}

/// <summary>
/// Offers a healer the nearest fighter of ours who is in a fight and has nobody standing by it.
///
/// <para>
/// <b>Healers only, and counted only for healers.</b> Every other bot is turned away in one comparison and is not
/// tallied, because a tally of seventy refusals a review per healer would bury the eleven questions that mean
/// something. The rest of the gates are the errand's own reasons for ending, asked first so that the offer is never
/// an errand that fails on its first beat.
/// </para>
/// </summary>
public sealed class BotAttendant : IBotProposer
{
    /// <summary>
    /// How far a healer looks for a fighter to stand by, in tiles.
    ///
    /// <para>
    /// Sixty since 14.09.2026, from forty: in the first six minutes of the change 407 of 409 askings found nobody of
    /// ours fighting within forty tiles, because healers live at their guild's seat and fighters go out from it to find
    /// something. Wider than that and the fight is over before the healer arrives, which the grace clock would then
    /// end on arrival.
    /// </para>
    /// </summary>
    public static int Reach { get; set; } = 60;

    public static long Asked { get; private set; }

    public static long Offered { get; private set; }

    public static long Held { get; private set; }

    public static long Unfit { get; private set; }

    public static long Pressed { get; private set; }

    public static long Bare { get; private set; }

    public static long Nobody { get; private set; }

    /// <summary>Fighters passed over because this healer lately could not keep up with them. Counted per fighter looked at.</summary>
    public static long Passed { get; private set; }

    public string Name => "Attendant";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } body || body.Class?.Role != BotRole.Medic)
        {
            return null;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        Asked++;

        if (body.Squad != null)
        {
            Held++;

            return null;
        }

        if (body.HitsMax <= 0 || body.Hits < body.HitsMax * BotHunter.FitAt)
        {
            Unfit++;

            return null;
        }

        if (BotThreat.Hunter(body, BotDefender.Reach) != null)
        {
            Pressed++;

            return null;
        }

        if (!BotAccompany.Means(body))
        {
            Bare++;

            return null;
        }

        // A fighter that is hiring first, at a wage, from further off; the unpaid stint by somebody already fighting
        // only when nobody is paying. See BotRetainer.
        var hirer = Hiring(body, map, out var wage);

        if (hirer != null)
        {
            Offered++;
            HiredOffered++;

            return new BotAccompany(hirer, map, wage);
        }

        var companion = Nearest(body, map);

        if (companion == null)
        {
            Nobody++;

            return null;
        }

        Offered++;

        return new BotAccompany(companion, map);
    }

    /// <summary>Stints offered with a wage in them.</summary>
    public static long HiredOffered { get; private set; }

    /// <summary>The nearest fighter of ours within <see cref="BotRetainer.Reach"/> that is hiring, and its wage, or null.</summary>
    private static BotMobile Hiring(BotMobile body, Map map, out int wage)
    {
        wage = 0;

        BotMobile best = null;
        var bestAway = double.MaxValue;

        foreach (var m in map.GetMobilesInRange<BotMobile>(body.Location, BotRetainer.Reach))
        {
            if (m == body || m.Class?.Role == BotRole.Medic || BotAccompany.Taken(m, body) || BotRegard.AtWar(body, m))
            {
                continue;
            }

            var offered = BotRetainer.Hiring(m);

            if (offered <= 0)
            {
                continue;
            }

            if (BotAccompany.Behind(body, m))
            {
                Passed++;

                continue;
            }

            var away = body.GetDistanceToSqrt(m);

            if (away >= bestAway)
            {
                continue;
            }

            // See BotMend.Abetting.
            if (BotMend.Abetting(body, m) != null)
            {
                continue;
            }

            if (BotReach.Ask(map, body.Location, m.Location, BotArrival.Within(BotAccompany.Stay)) == BotReachVerdict.Sealed)
            {
                continue;
            }

            best = m;
            bestAway = away;
            wage = offered;
        }

        return best;
    }

    private static BotMobile Nearest(BotMobile body, Map map)
    {
        BotMobile best = null;
        var bestAway = double.MaxValue;

        foreach (var m in map.GetMobilesInRange<BotMobile>(body.Location, Reach))
        {
            if (m == body || m.Class?.Role == BotRole.Medic || !BotAccompany.Engaged(m) || BotAccompany.Taken(m, body)
                || BotRegard.AtWar(body, m))
            {
                continue;
            }

            if (BotAccompany.Behind(body, m))
            {
                Passed++;

                continue;
            }

            var away = body.GetDistanceToSqrt(m);

            if (away >= bestAway)
            {
                continue;
            }

            // A robber is in a fight, and so is its victim: the victim is the one to stand by. See BotMend.Abetting.
            if (BotMend.Abetting(body, m) != null)
            {
                continue;
            }

            if (BotReach.Ask(map, body.Location, m.Location, BotArrival.Within(BotAccompany.Stay)) == BotReachVerdict.Sealed)
            {
                continue;
            }

            best = m;
            bestAway = away;
        }

        return best;
    }

    public static string Describe() =>
        Asked == 0
            ? "no healer has been offered anybody to stand by"
            : $"{Asked} times a healer was asked: {Offered} sent to stand by a fighter ({HiredOffered} of them hired), {Held} in a company, {Unfit} too hurt, {Pressed} with something on it, {Bare} with nothing to heal with, {Nobody} with nobody of ours fighting within {Reach} tiles; {BotAccompany.Stood} beats stood by, {BotAccompany.Lagged} times a healer fell behind and walked on, {BotAccompany.Reaimed} walks re-aimed at the fighter's new tile; stints ended {BotAccompany.Over} when the fight was over, {BotAccompany.Leased} at the end of the lease, {BotAccompany.Lost} when the fighter was gone, {BotAccompany.Spent} with nothing left to heal with, {BotAccompany.Outrun} when the healer could not keep up; {Passed} fighters passed over because their healer had lately fallen behind them";

    public static void Forget()
    {
        Asked = 0;
        Offered = 0;
        Held = 0;
        Unfit = 0;
        Pressed = 0;
        Bare = 0;
        Nobody = 0;
        Passed = 0;
        HiredOffered = 0;

        BotAccompany.Forget();
    }
}
