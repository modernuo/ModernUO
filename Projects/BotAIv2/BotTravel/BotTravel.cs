using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A walk to another town, for its own sake.
///
/// <para>
/// <b>Patrick's order of 29.09.2026: the bots should scatter over the world by themselves, travel, meet new horizons,
/// build roads to other towns and share what they find.</b> A bot with nothing pressing walks to a town it has not seen
/// — the roads nobody has walked first (<see cref="BotRoadbook"/>) — and on arriving tells its guild what the road was
/// like, in the quadrant map's own words: safe, positive, neutral, unsafe, dangerous. Then it is a bot in Trinsic with
/// Trinsic's shops, mines and creatures round it, and every ordinary errand works there as it did at home, because the
/// population's ground now includes every reachable town (<c>BotPopulation.Within</c>); when nothing there is worth
/// doing, the walk home is offered as it always was.
/// </para>
///
/// <para>
/// <b>Cheap on purpose, and steadfast.</b> A few coins a minute, so any real work beats it; held once taken, so a
/// traveller does not turn back for every stall on the road. Counted as coin, like flight, so a bot born poor may go.
/// </para>
/// </summary>
public sealed class BotTravel : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTravel));

    public const string Trade = "travel";

    /// <summary>What a journey is worth a minute. Eight: above the walk home's five, below any trade.</summary>
    public static double Prior { get; set; } = 8.0;

    /// <summary>How long a traveller walks before it gives up. Twenty-five minutes: Yew is some fifteen hundred steps from Britain.</summary>
    public static int MarchMs { get; set; } = 1500000;

    /// <summary>How near the square counts as arriving.</summary>
    public static int Arrival { get; set; } = 10;

    public static long Journeys { get; private set; }

    public static long Arrived { get; private set; }

    public static long Unreached { get; private set; }

    public static long Told { get; private set; }

    private readonly Map _map;

    private readonly BotTowns.Town _town;

    private readonly string _from;

    private long _began;

    /// <summary>The traveller's footprints every <c>BotRoadbook.TrackStep</c> tiles, offered to the roadbook on arriving.</summary>
    private readonly List<Point3D> _track = [];

    /// <summary>Whether the walk jumped — a gate, a death — and is not a road.</summary>
    private bool _broken;

    /// <summary>The recall that may carry a mage there instead of the road. See <see cref="BotRecall"/>.</summary>
    private BotRecall _recall;

    public BotTravel(Map map, BotTowns.Town town, string from)
    {
        _map = map;
        _town = town;
        _from = from;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _town?.Square ?? Point3D.Zero;

    public override double Expects => Prior;

    public override double Minutes => 10.0;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>Counted as coin, and it produces none — flight's lie, for flight's reason: a bot born poor must still be able to go.</summary>
    public override double Coin => 1.0;

    public override int Made => 0;

    public override bool Steadfast => true;

    public override string Stage => $"on the road from {_from} to {_town?.Name}";

    public override void Taken(IBotWilful bot)
    {
        _began = Core.TickCount;
        Journeys++;
        BotTraveller.Went(bot?.Self);
    }

    public override void Resumed(IBotWilful bot) => _began = Core.TickCount;

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal || _town == null || !body.Alive)
        {
            return BotDoing.Failed("no body, or nowhere to go");
        }

        Track(body.Location);

        if (body.InRange(_town.Square, Arrival))
        {
            return Arrive(body);
        }

        if (Core.TickCount - _began >= MarchMs)
        {
            Unreached++;

            return BotDoing.Failed($"could not reach {_town.Name} from {_from} in {MarchMs / 60000} minutes");
        }

        // Runes (29.09.2026): a mage holding a rune for the town recalls instead of walking; the road is then not walked. See BotRecall.
        if (BotRecall.Instead(bot, _map, _town.Square, Arrival, _town.Name, ref _recall) is { } cast)
        {
            return cast;
        }

        return BotDoing.Walk(_map, _town.Square, BotArrival.Within(Arrival), $"to {_town.Name} by the road");
    }

    /// <summary>In the square: the road is walked, and the guild is told what it was like.</summary>
    private BotDoing Arrive(Mobile body)
    {
        Arrived++;
        _town.Arrivals++;

        var minutes = (Core.TickCount - _began) / 60000.0;
        // A recall is not a road walked: the book counts only feet (BotRecall.Moved). Runes (29.09.2026).
        var road = _recall is { Moved: true } ? BotRoadbook.Between(_from, _town.Name) : BotRoadbook.Walked(_from, _town.Name, _broken ? null : _track);
        var safety = road != null ? BotRoadbook.Safety(road) : BotQuad.Fresh;
        var band = BotQuad.Band(safety);

        if (body is BotMobile { Guild: Guild guild } && BotVoice.Enabled)
        {
            var occasion = safety >= BotQuad.Positive ? "road:safe" : safety <= BotQuad.Unsafe ? "road:unsafe" : "road:quiet";
            var fill = new Dictionary<string, string>
            {
                ["place"] = _town.Name,
                ["from"] = _from,
                ["reason"] = band,
                ["minutes"] = minutes.ToString("F0"),
                ["guild"] = guild.Name
            };

            var line = BotVoice.Phrase(occasion, "road:quiet", body, fill);

            if (line != null && BotVoice.Say(body, "guild", line, force: true))
            {
                Told++;
            }
        }

        logger.Information(
            "{Name} the {Class} has walked from {From} to {Town} in {Minutes:F1} minutes; the road reads {Band} ({Safety:F2}) and has been walked {Walked} times",
            body.Name,
            (body as BotMobile)?.Class?.Name,
            _from,
            _town.Name,
            minutes,
            band,
            safety,
            road?.Walked ?? 0
        );

        return BotDoing.Done($"in {_town.Name} after {minutes:F0} minutes on the road from {_from}, which reads {band}");
    }

    /// <summary>A footprint, every <c>BotRoadbook.TrackStep</c> tiles; a jump further than a walk breaks the track.</summary>
    private void Track(Point3D at)
    {
        if (_track.Count == 0)
        {
            _track.Add(at);

            return;
        }

        var last = _track[^1];
        var away = Math.Max(Math.Abs(at.X - last.X), Math.Abs(at.Y - last.Y));

        if (away > BotRoadbook.TrackJump)
        {
            _broken = true;
        }

        if (away >= BotRoadbook.TrackStep)
        {
            _track.Add(at);
        }
    }

    public static string Describe() =>
        Journeys == 0
            ? "nobody has travelled to another town"
            : $"{Journeys} journeys to another town, {Arrived} arrived, {Unreached} gave up on the road, {Told} told their guild what the road was like";

    public static void Forget()
    {
        Journeys = 0;
        Arrived = 0;
        Unreached = 0;
        Told = 0;
    }
}

/// <summary>Offers a bot with nothing pressing a town it has not seen. See <see cref="BotTravel"/>.</summary>
public sealed class BotTraveller : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTraveller));

    /// <summary>How long one bot waits between journeys. Two hours.</summary>
    public static int EveryMs { get; set; } = 7200000;

    /// <summary>The furthest town offered, as the crow flies.</summary>
    public static int Furthest { get; set; } = 1800;

    /// <summary>How much likelier a road nobody has walked is than one walked once. The weight is 1 / (1 + walked / this).</summary>
    public static double Curiosity { get; set; } = 2.0;

    /// <summary>
    /// The least of its class's targets a bot must have reached before it is offered a journey. Fifteen hundredths: in the
    /// first ten minutes of the wiped world (29.09.2026, 01:35) twelve newcomers died on the road — Elenwe on the way to
    /// Trinsic among them — of the 58 deaths in all. A novice sees the next town when it can walk the road.
    /// </summary>
    public static double LeastProgress { get; set; } = 0.15;

    /// <summary>
    /// How much stronger than the worst the road asks (<see cref="BotRoadbook.Asks"/>) a traveller must be. Half again, the
    /// tolerance the shard uses for every fight it chooses. Five travellers died on the roads in the first twenty minutes of
    /// build 279 with the progress gate alone: the roads round Britain run past trolls, reapers and gazers.
    /// </summary>
    public static double Odds { get; set; } = 1.5;

    public static long Green { get; private set; }

    public static long Outmatched { get; private set; }

    public static long Asked { get; private set; }

    public static long TooSoon { get; private set; }

    public static long Unfit { get; private set; }

    public static long Unsupplied { get; private set; }

    public static long Nowhere { get; private set; }

    public static long Offered { get; private set; }

    /// <summary>Traders standing in the capital not offered a journey away from it. See <see cref="BotCapital.Trader"/>.</summary>
    public static long KeptAtCapital { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    private static bool _said;

    public string Name => "Traveller";

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

        if (map == null || map == Map.Internal || !body.Alive || body is not BotMobile who || who.Tired)
        {
            return null;
        }

        if (bot is IBotSquadMember { Squad: not null } || BotDelveParty.Delving(who) || BotDungeon.Under(body.Location))
        {
            return null;
        }

        if (!BotTowns.Ensure(map) || BotTowns.Reachable < 2)
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

        var here = BotTowns.Nearest(body.Location);
        var from = here?.Name ?? "the wild";

        // <b>A trader is at the centre of trade already, and stays (30.09.2026, Patrick's evening order: «the traders must
        // put Britain first as the centre of trade»).</b> A bot holding a shop's worth of what the island asks for, standing
        // in the capital, is where its customers and the capital's counters are; a journey for its own sake would carry the
        // shop away from them. See BotCapital.Trader.
        var capital = BotCapital.Town;
        var trader = capital != null && BotCapital.Trader(who);

        if (trader && here == capital)
        {
            KeptAtCapital++;

            return null;
        }

        // The sea (BotSea, Patrick's point 7 of 29.09.2026): a town no road reaches is sailed to, by chance against the
        // road's towns, and a bot on a land no road joins to home travels by sea or not at all. Not decided: the roads, as before.
        if (BotSea.Travel(bot, here, from, out var voyage))
        {
            Offered += voyage != null ? 1 : 0;

            return voyage;
        }

        var towns = BotTowns.All;
        var total = 0.0;
        var power = BotThreat.Power(body);
        var outmatched = 0;
        Span<double> weights = stackalloc double[towns.Count];

        for (var i = 0; i < towns.Count; i++)
        {
            var town = towns[i];

            weights[i] = 0.0;

            if (!town.FromHome || town == here || !Utility.InRange(town.Square, body.Location, Furthest))
            {
                continue;
            }

            var road = BotRoadbook.Between(from, town.Name);
            var walked = road?.Walked ?? 0;

            if (road is { Steps: < 0 })
            {
                continue;
            }

            // The road is measured against the walker: what the worst of its ground asks, against the bot's own strength.
            if (road != null && BotRoadbook.Asks(road) * Odds > power)
            {
                outmatched++;

                continue;
            }

            weights[i] = 1.0 / (1.0 + walked / Math.Max(0.1, Curiosity));
            total += weights[i];
        }

        if (total <= 0.0)
        {
            if (outmatched > 0)
            {
                Outmatched++;
            }
            else
            {
                Nowhere++;
            }

            return null;
        }

        BotTowns.Town picked = null;

        // <b>And a trader elsewhere takes its goods to the capital (30.09.2026).</b> The journey is offered at the same moments and
        // behind the same gates as any other — a trade far enough along, fit, supplied, two hours since the last, a road not past
        // its strength (Odds) and within Furthest — so no bot takes a road it would not have taken; only where it leads changes.
        // On arriving it is within BotShopkeeper.ShiftReach of the capital's bank, so its shift is offered there, and its loads go
        // to the capital's counters as the nearest. A trader whose road to the capital is not on offer travels as before.
        if (trader)
        {
            for (var i = 0; i < towns.Count; i++)
            {
                if (towns[i] == capital && weights[i] > 0.0)
                {
                    picked = capital;
                }
            }

            BotCapital.Journey(picked != null);
        }

        var roll = Utility.RandomDouble() * total;

        for (var i = 0; i < towns.Count && picked == null; i++)
        {
            roll -= weights[i];

            if (weights[i] > 0.0 && roll <= 0.0)
            {
                picked = towns[i];
            }
        }

        picked ??= towns[towns.Count - 1];
        Offered++;

        if (!_said)
        {
            _said = true;

            logger.Information("{Name} the {Class} is the first offered a journey: from {From} to {Town}", body.Name, who.Class?.Name, from, picked.Name);
        }

        return new BotTravel(map, picked, from);
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been asked to travel"
            : $"{Asked} asked to travel: {Offered} offered a town, {Nowhere} with nowhere new within {Furthest} tiles, {Outmatched} too weak for every road left (×{Odds:F1} what the worst of it asks), {Green} too new to the trade ({LeastProgress:P0} of it), {TooSoon} within {EveryMs / 3600000}h of their last, {Unfit} hurt, {Unsupplied} without supplies, {KeptAtCapital} traders kept in the capital and {BotCapital.Journeys} sent to it ({BotCapital.Unroaded} with no road to it on offer); {BotTravel.Describe()}";

    public static void Forget()
    {
        _last.Clear();
        _said = false;
        Asked = 0;
        Green = 0;
        Outmatched = 0;
        TooSoon = 0;
        Unfit = 0;
        Unsupplied = 0;
        Nowhere = 0;
        Offered = 0;
        KeptAtCapital = 0;
        BotTravel.Forget();
    }
}
