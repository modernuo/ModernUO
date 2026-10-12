using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a fighter of an invaded town the town's call: to raise the militia and march on the place (07.10.2026). One captain at
/// a time; another is offered when the call came to nothing (<see cref="BotInvasion.CallRetryMs"/>). Only to a fighter in no
/// company, within <see cref="BotInvasion.TownReach"/> of the town's square, whose way to the place is not sealed, and only while
/// at least <see cref="BotInvasion.LeastMilitia"/> free fighters stand near the town — counted once every half minute, not per
/// bot — so a captain is not handed a call it cannot raise a company for.
/// </summary>
public sealed class BotMilitiaCall : IBotProposer
{
    public string Name => "Militia";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>How often the free fighters round the invaded town are counted for the offer.</summary>
    public static int CountEveryMs { get; set; } = 30000;

    private static long _countedTick;

    private static bool _counted;

    private static int _free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotSeasons.Running || !BotInvasion.Running || BotInvasion.Calling is not { } raid)
        {
            return null;
        }

        var body = bot?.Self;

        if (body == null || body.Map != raid.Map || bot is not IBotSquadMember { Squad: null } || !BotSeasons.Fighter(body)
            || !body.InRange(raid.Town.Square, BotInvasion.TownReach))
        {
            return null;
        }

        var now = Core.TickCount;

        if (!BotInvasion.Offers(raid, now))
        {
            return null;
        }

        if (!_counted || now - _countedTick >= CountEveryMs)
        {
            _counted = true;
            _countedTick = now;
            _free = Free(raid);
        }

        if (_free < BotInvasion.LeastMilitia)
        {
            return null;
        }

        if (!BotGates.Joined(raid.Map, body.Location, raid.Site)
            || BotReach.Ask(raid.Map, body.Location, raid.Site, BotArrival.Within(BotPeril.Side / 3)) == BotReachVerdict.Sealed)
        {
            return null;
        }

        BotInvasion.Offer();

        return new BotMilitia(raid);
    }

    private static int Free(BotInvasion.Raid raid)
    {
        var free = 0;
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var b = bots[i];

            if (BotSeasons.Fighter(b) && b.Map == raid.Map && b is IBotSquadMember { Squad: null } && b.InRange(raid.Town.Square, BotInvasion.TownReach))
            {
                free++;
            }
        }

        return free;
    }
}

/// <summary>
/// The militia: a town's call answered by a captain who raises a company and marches on the invaded road (07.10.2026). The work
/// itself is the shard's patrol (<see cref="BotSweep"/>) — the call to the free fighters round the captain, the gathering, the
/// march, holding the ground and fighting there as a company — so every rule a company keeps holds here too. This adds the town:
/// its name on the work (<c>militia</c> in the ledger, so what a militia pays is learned apart from a sweep), the company handed to
/// the invasion to be watched for arrival, and an end when the raid is over and nothing is in front of the company.
/// </summary>
public sealed class BotMilitia : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMilitia));

    public const string Trade = "militia";

    private readonly BotInvasion.Raid _raid;

    private readonly BotSweep _sweep;

    public BotMilitia(BotInvasion.Raid raid)
    {
        _raid = raid;
        _sweep = new BotSweep(raid.Map, raid.Site, BotPeril.Reading(raid.Map, raid.Site));
    }

    public override string Kind => Trade;

    public override Map Map => _sweep.Map;

    public override Point3D Where => _sweep.Where;

    public override double Expects => BotInvasion.MilitiaPrior;

    public override double Minutes => _sweep.Minutes;

    public override SkillName? Trains => null;

    public override bool Braves => true;

    public override bool Alongside => true;

    public override string Stage => $"{_raid.Town?.Name}'s militia: {_sweep.Stage}";

    public override void Taken(IBotWilful bot) => BotInvasion.Took(_raid, bot?.Self);

    public override BotDoing Advance(IBotWilful bot)
    {
        var member = bot as IBotSquadMember;

        // The raid is over and nothing stands in front of the company: the town's call is answered.
        if (_raid.Over && member?.Squad is not { Stance: BotSquadStance.Fighting })
        {
            _sweep.Drop(bot);

            var done = BotDoing.Done($"the invasion of {_raid.Town?.Name} is over");

            BotInvasion.Ended(_raid, bot?.Self, done);

            return done;
        }

        var doing = _sweep.Advance(bot);

        if (member?.Squad is { } squad)
        {
            BotInvasion.Mustered(_raid, squad);
        }

        if (doing.Kind is BotDoingKind.Done or BotDoingKind.Failed)
        {
            BotInvasion.Ended(_raid, bot?.Self, doing);
        }

        return doing;
    }

    public override bool Bend(IBotWilful bot) => _sweep.Bend(bot);

    public override void Drop(IBotWilful bot)
    {
        _sweep.Drop(bot);

        logger.Information("{Name} let {Town}'s militia go", bot?.Self?.Name, _raid.Town?.Name);
    }
}
