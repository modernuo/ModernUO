using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// One fighter's walk into a dungeon by its mouth, alone.
///
/// <para>
/// <b>Patrick's order of 29.09.2026: "travel to such caves as a party, or alone, or alone and gather a party
/// inside".</b> The delve is a guild's undertaking and needs five; this is the other half of the order. A fighter strong
/// enough for a dungeon's worst inhabitant walks to the cave mouth the gates know (<see cref="BotGates"/>), steps
/// through, and the errand is done: from there it is a bot at work underground like any other — the hunt offers it what
/// lives in the next room, a corpse is gone through, a band forms round it when another bot is near (<c>BotBand</c>),
/// and when nothing is worth doing the walk home is offered and the journey takes it out by the mouth it came in by
/// (<c>BotHomeward</c>). Nothing here fights; the point of the errand is to be somewhere that offers something, which
/// is the same point the walk home has, pointed the other way.
/// </para>
///
/// <para>
/// <b>Where is the mouth, not the room.</b> The appraisal prices an errand by the walk to its place, and a room four
/// thousand tiles off in the dungeon block would price every venture at nothing. The gate the route begins with is on
/// the island, and it is what the walk actually costs.
/// </para>
/// </summary>
public sealed class BotVenture : BotDeed
{
    public const string Trade = "venture";

    /// <summary>What a venture is worth a minute, against the hunt's thirty: less than a fight and more than a stroll.</summary>
    public static double Prior { get; set; } = 12.0;

    /// <summary>How long the walk is reckoned at. Ten minutes; the march has its own cap.</summary>
    public static double WorkMinutes { get; set; } = 10.0;

    /// <summary>How long a venturer walks toward the mouth before giving up.</summary>
    public static int MarchMs { get; set; } = 900000;

    public static long Ventured { get; private set; }

    public static long Entered { get; private set; }

    public static long Unreached { get; private set; }

    private readonly Map _map;

    private readonly BotDungeon.Deep _deep;

    private readonly Point3D _room;

    private readonly Point3D _mouth;

    private long _began;

    public BotVenture(Map map, BotDungeon.Deep deep, Point3D room, Point3D mouth)
    {
        _map = map;
        _deep = deep;
        _room = room;
        _mouth = mouth;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _mouth;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>Counted as coin, and it produces none — the same lie flight tells, for the same reason: a bot born poor must still be able to go.</summary>
    public override double Coin => 1.0;

    public override int Made => 0;

    public override bool Braves => true;

    /// <summary>Held against ordinary offers on the way: a walk to a cave that turns aside for every stall is no journey.</summary>
    public override bool Steadfast => true;

    public override string Stage => $"to the mouth of {_deep?.Name} at ({_mouth.X}, {_mouth.Y})";

    public override void Taken(IBotWilful bot)
    {
        _began = Core.TickCount;
        Ventured++;
        BotVenturer.Went(bot?.Self);
    }

    public override void Resumed(IBotWilful bot) => _began = Core.TickCount;

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal || _deep == null || !body.Alive)
        {
            return BotDoing.Failed("no body, or nowhere to go");
        }

        if (_deep.Holds(body.Location))
        {
            Entered++;

            return BotDoing.Done($"in {_deep.Name} by its mouth; what lives here is the hunt's business now");
        }

        if (Core.TickCount - _began >= MarchMs)
        {
            Unreached++;

            return BotDoing.Failed($"could not reach the mouth of {_deep.Name} at ({_mouth.X}, {_mouth.Y}) in {MarchMs / 60000} minutes");
        }

        return BotDoing.Walk(_map, _room, BotArrival.Within(3), $"into {_deep.Name} by its mouth");
    }

    public static string Describe() =>
        Ventured == 0
            ? "nobody has ventured into a dungeon alone"
            : $"{Ventured} ventures alone into a dungeon, {Entered} through the mouth, {Unreached} gave up on the way";

    public static void Forget()
    {
        Ventured = 0;
        Entered = 0;
        Unreached = 0;
    }
}

/// <summary>
/// Offers a fighter with nothing better to do a dungeon it can walk into and survive alone. See <see cref="BotVenture"/>.
/// </summary>
public sealed class BotVenturer : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotVenturer));

    /// <summary>How much stronger than a dungeon's worst inhabitant a bot must be to go in alone. Half again, as the delve asks of a party.</summary>
    public static double Odds { get; set; } = 1.5;

    /// <summary>How long one bot waits between ventures. An hour.</summary>
    public static int EveryMs { get; set; } = 3600000;

    /// <summary>The least of its class's targets a bot must have reached before it goes into a cave alone. Three tenths.</summary>
    public static double LeastProgress { get; set; } = 0.3;

    public static long Green { get; private set; }

    public static long Asked { get; private set; }

    public static long Unfit { get; private set; }

    public static long Unsupplied { get; private set; }

    public static long TooSoon { get; private set; }

    public static long Outmatched { get; private set; }

    public static long Mouthless { get; private set; }

    public static long Offered { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    private static bool _said;

    public string Name => "Venturer";

    public BotStanding Rung => BotStanding.Free;

    public static void Went(Mobile who)
    {
        if (who != null)
        {
            _last[who.Serial] = Core.TickCount;
        }
    }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive || body is not BotMobile who)
        {
            return null;
        }

        if (who.Class is not { } klass || klass.Role == BotRole.Producer || who is not IBotAlly { AbleToFight: true })
        {
            return null;
        }

        if (bot is IBotSquadMember { Squad: not null } || BotDelveParty.Delving(who) || BotDungeon.Under(body.Location))
        {
            return null;
        }

        if (!BotGates.Ready)
        {
            return null;
        }

        Asked++;

        if (who.Progress < LeastProgress)
        {
            Green++;

            return null;
        }

        if (_last.TryGetValue(who.Serial, out var when) && Core.TickCount - (when + EveryMs) < 0)
        {
            TooSoon++;

            return null;
        }

        if (body.HitsMax <= 0 || body.Hits < body.HitsMax * BotHunter.FitAt)
        {
            Unfit++;

            return null;
        }

        if (!BotProvision.Fit(body, out _))
        {
            Unsupplied++;

            return null;
        }

        BotDungeon.Survey(map);

        var power = BotThreat.Power(body);
        BotDungeon.Deep best = null;

        for (var i = 0; i < BotDungeon.All.Count; i++)
        {
            var deep = BotDungeon.All[i];

            if (!deep.Ready || power < deep.Worst * Odds)
            {
                continue;
            }

            if (best == null || deep.Power > best.Power)
            {
                best = deep;
            }
        }

        if (best == null)
        {
            Outmatched++;

            return null;
        }

        var hall = BotHalls.Largest(best);
        var room = hall != null ? BotHalls.Room(hall, 0) : BotDungeon.Room(map, best, 0);

        if (room == Point3D.Zero || !BotGates.Next(map, body.Location, room, out var gate))
        {
            Mouthless++;

            return null;
        }

        Offered++;

        if (!_said)
        {
            _said = true;

            logger.Information(
                "{Name} the {Class} ({Power} of strength) is the first offered a venture alone: {Deep} (worst {Worst}) by {Gate}",
                body.Name,
                klass.Name,
                power.ToString("F0"),
                best.Name,
                best.Worst.ToString("F0"),
                gate
            );
        }

        return new BotVenture(map, best, room, gate.From);
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody fit has been asked to venture alone"
            : $"{Asked} asked to venture alone: {Offered} offered a dungeon, {Outmatched} too weak for the worst of any, {Green} too new to the trade ({LeastProgress:P0} of it), {Mouthless} with no mouth to walk to, {TooSoon} within the hour of their last, {Unfit} hurt, {Unsupplied} without supplies; {BotVenture.Describe()}";

    public static void Forget()
    {
        _last.Clear();
        _said = false;
        Asked = 0;
        Green = 0;
        Unfit = 0;
        Unsupplied = 0;
        TooSoon = 0;
        Outmatched = 0;
        Mouthless = 0;
        Offered = 0;
        BotVenture.Forget();
    }
}
