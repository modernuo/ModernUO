using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Getting a prisoner out of a camp and home again.
///
/// <para>
/// <b>Ordered by Patrick on 08.09.2026, and the engine does almost all of it.</b> Every one of this era's
/// camps — orc, brigand, ratman, lizardman — puts a <c>Noble</c> or a <c>SeekerOfAdventure</c> in the
/// middle of it with <c>IsPrisoner</c> set and <c>CantWalk</c> true, and has them yell for help whenever a
/// player walks past. Freeing one is not a lockpick or a key: it is
/// <c>BaseEscortable.AcceptEscorter</c>, which clears <c>CantWalk</c>, makes the bot its master and sets
/// it following. From then on the prisoner walks itself, and when it stands inside its destination region
/// the engine pays 500 to 1000 gold into the escorter's pack, awards ten fame and four hundred points of
/// Compassion, and deletes itself.
/// </para>
///
/// <para>
/// <b>So the whole of the work here is the walking, and the whole of the risk is the distance.</b> The
/// destination is a random town or dungeon region on Felucca, which on this map can be four hundred tiles
/// off — further than any road this shard will search for. A bot that accepts an escort it cannot complete
/// has taken a prisoner out of a cage to die of neglect in a field, so the distance is checked before the
/// offer is made and never after, and it is checked against the same number every other walk is:
/// <see cref="BotHunter.Walkable"/>.
/// </para>
///
/// <para>
/// <b>One at a time, and the engine enforces it.</b> <c>EscortDelay</c> is five minutes between escorts for
/// any one <c>PlayerMobile</c>, and bots are PlayerMobiles, so a population that all rushed the same camp
/// would simply be refused. Counted rather than avoided: <see cref="Refused"/> reading high next to a
/// healthy <see cref="Freed"/> is the population wanting more of this work than the rules allow.
/// </para>
/// </summary>
public sealed class BotFreedom : BotDeed
{
    /// <summary>
    /// The ledger key.
    ///
    /// Not "free": the debugger's hand already has a verb by that name — <c>free &lt;bot&gt;</c>, which makes a
    /// bot forget its plan — and a summary in which the same word means both a rescue and a shake-loose is a
    /// summary somebody reads wrong at two in the morning.
    /// </summary>
    public const string Trade = "liberate";

    /// <summary>
    /// What freeing somebody is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// The engine pays 500 to 1000 gold for a walk that takes a few minutes, which is several times what
    /// any trade on this island earns in the same time — so this is priced high on purpose and still
    /// honestly: it is one of the few things on the shard that brings coin in from outside the population's
    /// own pockets, which is the only kind of earning that grows the economy rather than moving it about.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 220.0;

    /// <summary>How long the walk home is reckoned to take. Long, because it is a walk across a map.</summary>
    public static double WorkMinutes { get; set; } = 6.0;

    /// <summary>How far around itself a bot hears somebody yelling from a cage.</summary>
    public static int Reach { get; set; } = 48;

    /// <summary>
    /// How far a prisoner may be from Britain and still be worth freeing.
    ///
    /// The population's own roam rather than the dart limit: this is one bot walking one prisoner home a few
    /// times an hour, not forty-seven bots throwing darts every beat, so the price rule that keeps the hunt
    /// cheap does not belong here. The same distinction BotScout.Range makes, and for the same reason.
    /// </summary>
    public static int Roam { get; set; } = 1000;

    /// <summary>How near a prisoner a bot has to be for the engine to accept it as an escort.</summary>
    public static int Touch { get; set; } = 3;

    /// <summary>The middle of what the engine pays, for the errand's own account of itself.</summary>
    public static int Reward { get; set; } = 750;

    /// <summary>
    /// Where every prisoner is taken, whatever town the engine picked for it.
    ///
    /// <para>
    /// <b>Patrick's order of 08.09.2026, and the measurement behind it.</b> The engine gives a prisoner a
    /// random town or dungeon region on Felucca, which on this map can be most of a continent away — and an
    /// escort that cannot be finished takes somebody out of a cage to die of neglect in a field. The first
    /// version refused those escorts instead, and the shard duly reported "5 passed over for living further
    /// from their own town than 500 tiles" with an orc camp standing 129 tiles from home and nobody freed.
    /// </para>
    ///
    /// <para>
    /// Refusing was the wrong half to fix. The population lives beside Britain, walks it daily and banks in
    /// it; a rescued prisoner delivered there is delivered somewhere real. So the destination is set rather
    /// than inspected, and the distance test below measures the walk that will actually happen.
    /// </para>
    /// </summary>
    public static string Town { get; set; } = "Britain";

    /// <summary>Where that town is, or nowhere if the map has no region by that name.</summary>
    private static Point3D Where_Town
    {
        get
        {
            var region = EscortDestinationInfo.Find(Town)?.Region;

            return region == null ? Point3D.Zero : region.GoLocation;
        }
    }

    /// <summary>Prisoners taken out of their cages. Named apart from <see cref="Taken(IBotWilful)"/>, the choosing of one.</summary>
    public static long Uncaged { get; private set; }

    /// <summary>Prisoners delivered.</summary>
    public static long Freed { get; private set; }

    /// <summary>Times the engine refused the escort — usually the five-minute rest between two of them.</summary>
    public static long Refused { get; private set; }

    /// <summary>Escorts that ended without a delivery: the prisoner died, wandered off, or gave up on the bot.</summary>
    public static long Lost { get; private set; }

    /// <summary>Prisoners passed over for living further from their own destination than a road is searched for.</summary>
    public static long TooFar { get; private set; }

    /// <summary>
    /// How far behind its escort a prisoner may fall before the bot turns back for it.
    ///
    /// <para>
    /// <b>Not one prisoner was walked home on 23 or 24.09.2026: 27 escorts were lost after the last delivery, at 04:43 on
    /// 22.09.</b> The engine lets a prisoner go when its escorter has been more than 30 tiles off for two minutes
    /// (<c>BaseEscortable.GetEscorter</c>, <c>AbandonDelay</c>), and a bot walked home by its own road at its own pace, a
    /// rider at up to ten tiles a second, while the prisoner followed by the engine's own search, which sees a box 38 tiles
    /// across (<c>BitmapAStarAlgorithm.AreaSize</c>) and loses a leader whose road bends round a wall or a wood outside it.
    /// Eleven failed "it is no longer following" at 2.1 minutes, the engine's two; twelve "the walk stopped closing 2 tiles
    /// short of" Britain's centre: the bot had arrived alone, with the prisoner still out in the fields. A player walking an
    /// escort looks over its shoulder, so past this many tiles the bot walks back to the prisoner until it is within
    /// <see cref="Rejoin"/>, then goes on.
    /// </para>
    /// </summary>
    public static int Lag { get; set; } = 10;

    /// <summary>How near a prisoner that fell behind must come again before the walk home goes on. See <see cref="Lag"/>.</summary>
    public static int Rejoin { get; set; } = 3;

    /// <summary>
    /// How long a prisoner may go without getting any nearer home before the escort is given up. A prisoner that cannot
    /// follow at all, hemmed in or with its short search beaten by the ground, would otherwise have its escort walk back
    /// and forth to it for ever.
    /// </summary>
    public static int StuckMs { get; set; } = 120000;

    /// <summary>Times an escort turned back for a prisoner that had fallen behind. See <see cref="Lag"/>.</summary>
    public static long TurnedBack { get; private set; }

    /// <summary>Escorts given up because the prisoner got no nearer home for <see cref="StuckMs"/>.</summary>
    public static long Stuck { get; private set; }

    private readonly Map _map;

    private readonly BaseEscortable _prisoner;

    private readonly Point3D _cage;

    private bool _taken;

    private bool _delivered;

    /// <summary>Whether the bot is on its way back to a prisoner that fell behind. See <see cref="Lag"/>.</summary>
    private bool _behind;

    /// <summary>The nearest the prisoner has come to home, and when. See <see cref="StuckMs"/>.</summary>
    private int _nearestHome = int.MaxValue;

    private long _nearestHomeTick;

    /// <summary>
    /// How long a bot's claim on a prisoner stands while it walks to the cage.
    ///
    /// <para>
    /// Long enough for the walk from anywhere within <see cref="Reach"/> and the engine's answer at the cage. It only
    /// has to cover the approach: once the escort is accepted the engine clears <c>CantWalk</c>, and
    /// <see cref="Nearest"/> stops seeing the prisoner at all.
    /// </para>
    /// </summary>
    public static int ClaimMs { get; set; } = 90000;

    /// <summary>Who has set out for which prisoner, and when. Keyed by the prisoner: a fact about somebody we do not own.</summary>
    private static readonly Dictionary<Serial, (Serial By, long Tick)> _claims = [];

    public BotFreedom(Map map, BaseEscortable prisoner)
    {
        _map = map;
        _prisoner = prisoner;
        _cage = prisoner?.Location ?? Point3D.Zero;
    }

    public override string Kind => Trade;

    /// <summary>
    /// The prisoner is this bot's from the moment it chooses the walk. See <see cref="BotDeed.Taken"/>.
    ///
    /// <para>
    /// <b>A whole population chose one prisoner in the same second.</b> At build 56's boot, 11:56:36 on 15.09.2026, Ida
    /// stood in a cage within earshot of the population's home, every idle bot was asked in the same step, and each was
    /// offered her at 187 to 220 a minute: 68 liberations taken in that one second, and all 68 failed — 31 "it would not
    /// come" at a prisoner already following somebody, 35 "the prisoner is gone", one lost on the way. The boot of 04:38 on
    /// 14.09 did the same, 54 taken in its first minute and 52 failed in its first two. Nothing was held between the choosing and the engine's acceptance a walk
    /// later. A claim made at commit is seen by the very next proposer asked, which is the cure build 46 found for lessons
    /// and companies.
    /// </para>
    /// </summary>
    public override void Taken(IBotWilful bot)
    {
        if (bot?.Self is { } body && _prisoner != null)
        {
            _claims[_prisoner.Serial] = (body.Serial, Core.TickCount);
        }
    }

    /// <summary>Put down for something else: the prisoner is free for another bot now rather than when the claim lapses.</summary>
    public override void Drop(IBotWilful bot)
    {
        base.Drop(bot);

        Release(bot?.Self, _prisoner);
    }

    /// <summary>Whether another bot has lately set out for this prisoner. A lapsed claim is forgotten here.</summary>
    public static bool Claimed(Mobile body, BaseEscortable prisoner)
    {
        if (body == null || prisoner == null || !_claims.TryGetValue(prisoner.Serial, out var claim))
        {
            return false;
        }

        if (Core.TickCount - claim.Tick >= ClaimMs)
        {
            _claims.Remove(prisoner.Serial);

            return false;
        }

        return claim.By != body.Serial;
    }

    /// <summary>This bot's claim on the prisoner, if it holds one, is given up.</summary>
    private static void Release(Mobile body, BaseEscortable prisoner)
    {
        if (body != null && prisoner != null && _claims.TryGetValue(prisoner.Serial, out var claim) && claim.By == body.Serial)
        {
            _claims.Remove(prisoner.Serial);
        }
    }

    /// <summary>Somebody held prisoner in a camp. See <see cref="BotDeed.Summons"/>.</summary>
    public override bool Summons => true;

    public override Map Map => _map;

    /// <summary>
    /// Where the errand is headed, which changes exactly once.
    ///
    /// <para>
    /// <b>And it must change only once.</b> A destination recomputed every beat resets the walk — see the
    /// rule about a walking order having to be stable — so this reads the cage until the prisoner is in
    /// hand and the town from then on, and never anything else.
    /// </para>
    /// </summary>
    public override Point3D Where => _taken ? Home() : _cage;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    /// <summary>
    /// What the engine paid, near enough. The exact figure is 500 to 1000 and it lands in the pack without
    /// telling anybody, so this reports the middle of the range; the true movement of the purse is in the
    /// GOLD column, which is measured rather than declared.
    /// </summary>
    public override int Made => _delivered ? Reward : 0;

    public override string Stage =>
        _delivered ? "delivered" : _taken ? $"walking {_prisoner?.Name} home" : $"after {_prisoner?.Name}";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        // Delivered: the engine pays, then deletes. So a prisoner that has gone while this bot was still its
        // escorter is the success case — and so is one still standing in the seconds before the deletion, which is
        // read below, where the escorter is.
        if (_prisoner == null || _prisoner.Deleted)
        {
            Release(body, _prisoner);

            if (_taken)
            {
                _delivered = true;
                Freed++;

                return BotDoing.Done("delivered, and paid for it");
            }

            return BotDoing.Failed("the prisoner is gone");
        }

        if (!_prisoner.Alive)
        {
            Lost++;

            return BotDoing.Failed("the prisoner is dead");
        }

        if (!_taken)
        {
            if (!body.InRange(_prisoner.Location, Touch))
            {
                return BotDoing.Walk(_map, _cage, BotArrival.Within(Touch), $"after {_prisoner.Name}");
            }

            // Britain, whatever the engine picked. Set before the escort is accepted, because AcceptEscorter
            // refuses outright when there is no destination and announces the one it has when there is. See
            // Town.
            if (_prisoner.Destination != Town)
            {
                _prisoner.Destination = Town;
            }

            // The engine's own answer, asked once. It refuses for reasons this file should not second-guess:
            // somebody already has this one, the bot escorted somebody else inside five minutes, the prisoner
            // has nowhere to go.
            if (!_prisoner.AcceptEscorter(body))
            {
                Refused++;
                Release(body, _prisoner);

                return BotDoing.Failed("it would not come");
            }

            _taken = true;
            _nearestHomeTick = Core.TickCount;
            Uncaged++;

            return BotDoing.Work("freeing it");
        }

        // Somebody else's now, or nobody's. Either way this errand is over and the bot should not go on
        // walking to a town for a prisoner that is not behind it.
        if (_prisoner.GetEscorter() != body)
        {
            // <b>Delivered, and the engine says so before it deletes.</b> BaseEscortable.CheckAtDestination pays, clears
            // the prisoner's destination, lets go of the escorter and only then starts the timer that deletes it — so for
            // those seconds the prisoner stands in the world following nobody and going nowhere, and this read every paid
            // escort as lost. Not one liberation finished on 14–15.09.2026; twelve failed "it is no longer following"
            // carrying 846 to 1036 coin, the engine's pay (Emeric, 936 at 12:46:10). A prisoner that is merely lost keeps
            // its destination — the engine's abandonment lets go of the escorter and leaves that alone — so a destination
            // gone is arrival and nothing else. GetDestination picks no new one once the deletion is under way.
            if (_prisoner.GetDestination() == null)
            {
                Release(body, _prisoner);
                _delivered = true;
                Freed++;

                return BotDoing.Done("delivered, and paid for it");
            }

            Lost++;

            return BotDoing.Failed("it is no longer following");
        }

        var home = Home();

        if (home == Point3D.Zero)
        {
            Lost++;

            return BotDoing.Failed("it no longer knows where it is going");
        }

        // The prisoner follows by itself, and arrival is the engine's to declare: it checks the region on the
        // prisoner's own think, pays, and deletes, which is caught at the top of this method. The prisoner has to be
        // there for that, and it keeps up only on open ground, so the walk is paced to it. See Lag.
        var left = Math.Max(Math.Abs(_prisoner.X - home.X), Math.Abs(_prisoner.Y - home.Y));

        if (left < _nearestHome)
        {
            _nearestHome = left;
            _nearestHomeTick = Core.TickCount;
        }
        else if (Core.TickCount - _nearestHomeTick >= StuckMs)
        {
            Stuck++;
            Lost++;

            return BotDoing.Failed($"{_prisoner.Name} got no nearer home in {StuckMs / 1000}s, {left} tiles off it");
        }

        var gap = Math.Max(Math.Abs(_prisoner.X - body.X), Math.Abs(_prisoner.Y - body.Y));

        if (_behind ? gap > Rejoin : gap > Lag)
        {
            if (!_behind)
            {
                _behind = true;
                TurnedBack++;
            }

            return BotDoing.Walk(_map, _prisoner, BotArrival.Within(Rejoin), $"going back for {_prisoner.Name}");
        }

        _behind = false;

        return BotDoing.Walk(_map, home, BotArrival.Within(2), $"walking {_prisoner.Name} home");
    }

    /// <summary>Where this prisoner is trying to get to, or nowhere.</summary>
    private Point3D Home()
    {
        var region = _prisoner?.GetDestination()?.Region;

        return region == null ? Point3D.Zero : region.GoLocation;
    }

    /// <summary>
    /// The nearest prisoner nobody is walking home yet, or null.
    ///
    /// <para>
    /// <c>CantWalk</c> is the test rather than <c>IsPrisoner</c> alone: the flag stays set for the whole of
    /// the escort, and the engine clears the cage the moment somebody accepts. So this asks "still in the
    /// cage", which is the question, rather than "was once in one".
    /// </para>
    /// </summary>
    /// <param name="spoken">Whether a prisoner still in its cage was passed over because another bot has set out for it.</param>
    public static BaseEscortable Nearest(Mobile body, int range, out bool spoken)
    {
        spoken = false;

        var map = body?.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        BaseEscortable best = null;
        var bestAway = double.MaxValue;

        foreach (var near in map.GetMobilesInRange<BaseEscortable>(body.Location, range))
        {
            if (near.Deleted || !near.Alive || !near.IsPrisoner || !near.CantWalk)
            {
                continue;
            }

            // Somebody is already on the way to this one. See Taken.
            if (Claimed(body, near))
            {
                spoken = true;

                continue;
            }

            var town = Where_Town;

            if (town == Point3D.Zero)
            {
                continue;
            }

            // <b>Refused before it is offered, never after.</b> An escort that cannot be finished leaves
            // somebody standing in a field who was at least safe in the cage. Measured from the prisoner,
            // because that is where the walk starts, and against Britain, because that is where it ends —
            // see Town. The engine's own random destination is not consulted: it is overwritten.
            if (!Utility.InRange(near.Location, town, Roam))
            {
                TooFar++;

                continue;
            }

            var away = body.GetDistanceToSqrt(near.Location);

            if (away >= bestAway)
            {
                continue;
            }

            best = near;
            bestAway = away;
        }

        return best;
    }

    public static void Forget()
    {
        _claims.Clear();
        Uncaged = 0;
        Freed = 0;
        Refused = 0;
        Lost = 0;
        TooFar = 0;
        TurnedBack = 0;
        Stuck = 0;
    }

    public static string Describe() =>
        $"{Uncaged} prisoners taken out of cages and {Freed} walked home, {Lost} lost on the way ({Stuck} of them got no nearer home "
        + $"in {StuckMs / 1000}s), {TurnedBack} times an escort turned back for one more than {Lag} tiles behind, "
        + $"{Refused} refused by the engine, {TooFar} passed over for living further than {Roam} tiles from {Town}";
}

/// <summary>
/// Offers a bot the job of walking a prisoner home.
///
/// <para>
/// Rationed to once a minute per bot like every other spatial sweep on this shard, and offered to anybody:
/// a prisoner does not care what trade its escort practises, and the walk is the work.
/// </para>
/// </summary>
public sealed class BotLiberator : IBotProposer
{
    public string Name => "Liberator";

    public BotStanding Rung => BotStanding.Free;

    public static long Asked { get; private set; }

    public static long Soon { get; private set; }

    public static long None { get; private set; }

    public static long Offered { get; private set; }

    /// <summary>Bots that heard only prisoners another bot had already set out for. See <see cref="BotFreedom.Taken(IBotWilful)"/>.</summary>
    public static long Spoken { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        Asked++;

        if (!BotNeeds.Due(body, "prisoner"))
        {
            Soon++;

            return null;
        }

        var prisoner = BotFreedom.Nearest(body, BotFreedom.Reach, out var spoken);

        if (prisoner == null)
        {
            if (spoken)
            {
                Spoken++;
            }
            else
            {
                None++;
            }

            return null;
        }

        Offered++;

        return new BotFreedom(map, prisoner);
    }

    public static string Describe() =>
        $"{Asked} asked to walk somebody home: {Offered} sent to a cage, {None} heard nobody, {Spoken} heard only prisoners another bot had set out for, {Soon} had listened too recently";

    public static void Reset()
    {
        Asked = 0;
        Soon = 0;
        None = 0;
        Offered = 0;
        Spoken = 0;
    }
}
