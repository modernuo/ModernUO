namespace Server.BotAI.V2;

/// <summary>
/// Offers a trader or a crafter within <see cref="BotFair.Reach"/> of an open fair the walk to it (07.10.2026): once a fair, while
/// at least half an hour of it is left, when the way is not sealed and the straight road does not ask more than the bot brings
/// (<c>BotDanger.Road</c>, the walk's own question). Priced at <see cref="BotFair.Prior"/> until the ledger knows what fairs pay.
/// </summary>
public sealed class BotFairCall : IBotProposer
{
    public string Name => "Fair";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>The least of the fair that must be left for the walk to be offered.</summary>
    public static int LeastLeftMs { get; set; } = 1800000;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotSeasons.Running || !BotFair.Open || bot?.Self is not BotMobile body || body.Map != BotPopulation.Home)
        {
            return null;
        }

        if (!BotTrading.Trader(body) && !BotCraftFirst.Crafter(bot) || BotFair.Came(body))
        {
            return null;
        }

        var town = BotFair.Town;

        if (BotFair.ClosesTick - Core.TickCount < LeastLeftMs || !body.InRange(town.Square, BotFair.Reach) || town.Bounds.Contains(body.Location))
        {
            return null;
        }

        var map = body.Map;

        if (!BotGates.Joined(map, body.Location, town.Square)
            || BotReach.Ask(map, body.Location, town.Square, BotArrival.Within(6)) == BotReachVerdict.Sealed
            || BotDanger.Road(body, map, body.Location, town.Square, BotQuad.Strength(body), out _, out _, out _, BotFairgoer.Trade))
        {
            return null;
        }

        BotFair.Offer();

        return new BotFairgoer(town);
    }
}

/// <summary>
/// Going to the fair: the walk to its town's square, a lottery ticket on arrival, and a few minutes there among the stalls and the
/// cheaper counters, then done. What it pays is whatever the bot made of being there — the ledger's <c>fair</c> row — and the lottery,
/// should its ticket be drawn.
/// </summary>
public sealed class BotFairgoer : BotDeed
{
    public const string Trade = "fair";

    private readonly BotTowns.Town _town;

    private readonly Map _map;

    private long _arrivedTick;

    private bool _arrived;

    public BotFairgoer(BotTowns.Town town)
    {
        _town = town;
        _map = BotPopulation.Home;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _town.Square;

    public override double Expects => BotFair.Prior;

    public override double Minutes => BotFair.StayMs / 60000.0;

    public override string Stage => _arrived ? $"at the fair in {_town.Name}" : $"walking to the fair in {_town.Name}";

    public override void Taken(IBotWilful bot) => BotFair.Going(bot?.Self);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        if (!BotFair.Open || !ReferenceEquals(BotFair.Town, _town))
        {
            if (_arrived)
            {
                return BotDoing.Done($"the fair in {_town.Name} closed while I was there");
            }

            BotFair.Late();

            return BotDoing.Failed($"the fair in {_town.Name} closed before I got there");
        }

        if (!_arrived)
        {
            if (!body.InRange(_town.Square, 6))
            {
                return BotDoing.Walk(_map, _town.Square, BotArrival.Within(6), $"walking to the fair in {_town.Name}");
            }

            _arrived = true;
            _arrivedTick = Core.TickCount;
            BotFair.Reached(body);
        }

        if (Core.TickCount - _arrivedTick >= BotFair.StayMs)
        {
            return BotDoing.Done($"spent {BotFair.StayMs / 60000} minutes at the fair in {_town.Name}");
        }

        return BotDoing.Work($"at the fair in {_town.Name}");
    }
}
