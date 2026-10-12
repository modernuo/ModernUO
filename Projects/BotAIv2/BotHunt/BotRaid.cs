using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The Baron's raid on The Shadow's hideout, once a caught thief has given it away: the Baron and a posse march there, search
/// it with Detect Hidden, and set on every member of The Shadow they find; then the hideout is burned and The Shadow finds
/// another.
///
/// <para>
/// <b>Patrick's order of 17.09.2026: the price of The Shadow's trade is prison and pursuit, and if the Baron learns where the
/// hideout is — a caught thief gives it away — he raids it.</b> A member found there is made wanted on the Baron's word
/// (<see cref="BotOutlaw.Want"/>), which is what makes setting on it lawful in the engine's reckoning, and a member that falls
/// to the raid falls to a patrol and is caught (<see cref="BotOutlaw.Fell"/>). A hidden member is found only by a search, or
/// by stepping out.
/// </para>
/// </summary>
public sealed class BotRaid : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRaid));

    public const string Trade = "raid";

    /// <summary>How long the raid searches the hideout once it is there. Two minutes: a dozen searches.</summary>
    public static int SearchMs { get; set; } = 120000;

    /// <summary>How far round the hideout the raid looks for members of The Shadow.</summary>
    public static int Around { get; set; } = 14;

    /// <summary>Raids raised by the Baron.</summary>
    public static long Raised { get; private set; }

    /// <summary>Fighters pressed into raids.</summary>
    public static long Hands { get; private set; }

    /// <summary>Members of The Shadow found at a hideout by a raid.</summary>
    public static long Found { get; private set; }

    private readonly Map _map;

    private readonly Point3D _at;

    private readonly SkillName _trains;

    private readonly bool _leads;

    private BotBrawl _fight;

    private bool _arrived;

    private long _arrivedTick;

    private int _found;

    /// <param name="map">Where.</param>
    /// <param name="at">The hideout.</param>
    /// <param name="trains">The skill the fighting trains.</param>
    /// <param name="leads">Whether this is the Baron's own raid, which presses the posse and ends the raid for the hideout.</param>
    public BotRaid(Map map, Point3D at, SkillName trains, bool leads)
    {
        _map = map;
        _at = at;
        _trains = trains;
        _leads = leads;
    }

    public override string Kind => Trade;

    public override bool Committed => true;

    public override bool Steadfast => true;

    public override bool Braves => true;

    public override bool Unpaid => true;

    public override Map Map => _map;

    public override Point3D Where => _at;

    public override double Expects => BotBrawl.Prior;

    public override double Minutes => 8.0;

    public override SkillName? Trains => _trains;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override int Made => 0;

    public override string Stage =>
        _fight != null ? $"raid: {_fight.Stage}" : _arrived ? "searching The Shadow's hideout" : "marching on The Shadow's hideout";

    /// <summary>The Baron's raid presses the nearest fit fighters into it, as a patrol does.</summary>
    public override void Taken(IBotWilful bot)
    {
        var body = bot?.Self;

        if (!_leads || body?.Map is not { } map || map == Map.Internal)
        {
            return;
        }

        BotUnderworld.RaidBegun();
        Raised++;

        List<BotMobile> near = [];

        foreach (var other in map.GetMobilesInRange<BotMobile>(body.Location, BotManhunt.Reach))
        {
            if (other == body || !other.Alive || other.Squad != null || other.Class is not { } klass
                || klass.Role is BotRole.Producer or BotRole.Medic || klass.Unpaid || BotOutlaw.Outlaw(other) || BotOutlaw.Jailed(other)
                || BotDuel.Duelling(other) || other.Resolve?.Deed is BotBrawl or BotRaid || BotUnderworld.Member(other))
            {
                continue;
            }

            near.Add(other);
        }

        near.Sort((x, y) => x.GetDistanceToSqrt(body).CompareTo(y.GetDistanceToSqrt(body)));

        var pressed = 0;

        for (var i = 0; i < near.Count && pressed < BotManhunt.Posse; i++)
        {
            var hand = near[i];

            if (BotWill.Press(hand, new BotRaid(_map, _at, hand.Bond?.Weapon?.Skill ?? SkillName.Wrestling, false), "the Baron's raid on The Shadow's hideout"))
            {
                pressed++;
            }
        }

        Hands += pressed;

        logger.Information(
            "{Baron} raises a raid of {Count} on The Shadow's hideout at ({X}, {Y})",
            body.Name,
            pressed,
            _at.X,
            _at.Y
        );
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || body.Map != _map)
        {
            return BotDoing.Failed("no body");
        }

        if (_fight != null)
        {
            var fighting = _fight.Advance(bot);

            if (fighting.Kind is not (BotDoingKind.Done or BotDoingKind.Failed))
            {
                return fighting;
            }

            _fight = null;
        }

        if (!body.InRange(_at, 6))
        {
            return BotDoing.Walk(_map, _at, BotArrival.Within(5), "to The Shadow's hideout");
        }

        var now = Core.TickCount;

        if (!_arrived)
        {
            _arrived = true;
            _arrivedTick = now;
        }

        BotShadow.Search(body);

        foreach (var m in _map.GetMobilesInRange<BotMobile>(_at, Around))
        {
            if (m == body || !m.Alive || m.Hidden || !BotUnderworld.Member(m) || BotOutlaw.Jailed(m))
            {
                continue;
            }

            BotOutlaw.Want(m, body.Name, "being found at The Shadow's hideout");
            _found++;
            Found++;
            _fight = new BotBrawl(m, BotBrawl.Manhunt, _trains, BotManhunt.Over);

            return BotDoing.Work($"setting on {m.Name} at the hideout");
        }

        if (now - _arrivedTick >= SearchMs)
        {
            if (_leads)
            {
                BotUnderworld.RaidEnded(body.Name, _found);
            }

            return BotDoing.Done($"searched The Shadow's hideout and found {_found}");
        }

        return BotDoing.Work("searching The Shadow's hideout");
    }

    public override bool Bend(IBotWilful bot) => _fight?.Bend(bot) ?? false;

    public override void Drop(IBotWilful bot) => _fight?.Drop(bot);

    public static string Describe() => $"{Raised} raids on The Shadow's hideout, {Hands} fighters pressed into them, {Found} members found there";

    public static void Forget()
    {
        Raised = 0;
        Hands = 0;
        Found = 0;
    }
}
