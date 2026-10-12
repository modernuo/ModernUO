using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers the bot carrying a relic for its guild the walk home to hang it in the hall (07.10.2026, relics20). The road home is the
/// relic's dangerous hour: a thief may take it from the pack (<see cref="BotRelicSnatch"/>). Unpaid on purpose — what it brings is
/// the guild's, not the carrier's — so the appraisal must not refuse it for paying nothing (<see cref="BotDeed.Unpaid"/>); and a
/// relic left in a pack <see cref="BotRelics.CarryMostMs"/> is fetched home by the guild itself.
/// </summary>
public sealed class BotRelicBearer : IBotProposer
{
    public string Name => "RelicHome";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotRelics.Running || !BotRelicHomeward.Running || BotRelics.Carrying == 0)
        {
            return null;
        }

        var body = bot?.Self;

        if (body is not BotMobile { Deleted: false, Alive: true } || bot is IBotSquadMember { Squad: not null }
            || BotRelics.CarriedBy(body) is not { Guild: not null } relic)
        {
            return null;
        }

        var hall = BotEstate.Hall(BotGuilds.Find(relic.Guild));

        if (hall == null || hall.Map != body.Map || !BotGates.Joined(hall.Map, body.Location, hall.Location))
        {
            return null;
        }

        BotRelicHomeward.Offer();

        return new BotRelicHomeward(relic, hall.Map, hall.Location);
    }
}

/// <summary>Walking a relic home and setting it up in the guild's hall.</summary>
public sealed class BotRelicHomeward : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRelicHomeward));

    public const string Trade = "relic-home";

    public static bool Running { get; set; } = true;

    /// <summary>What the walk home is reckoned at per minute: the guild's errand, unpaid, priced to be taken.</summary>
    public static double Prior { get; set; } = 50.0;

    /// <summary>How near the hall's door counts as home.</summary>
    public static int Reach { get; set; } = 8;

    public static long Offered { get; private set; }

    public static long Walked { get; private set; }

    public static long Home { get; private set; }

    public static long Lost { get; private set; }

    internal static void Offer() => Offered++;

    private readonly BotRelic _relic;

    private readonly Map _map;

    private readonly Point3D _hall;

    public BotRelicHomeward(BotRelic relic, Map map, Point3D hall)
    {
        _relic = relic;
        _map = map;
        _hall = hall;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _hall;

    public override double Expects => Prior;

    public override double Minutes => 4.0;

    public override bool Unpaid => true;

    public override bool Steadfast => true;

    public override string Stage => $"carrying {_relic.Name} home to the hall of {_relic.Guild}";

    public override void Taken(IBotWilful bot)
    {
        Walked++;

        if (bot?.Self is { } body)
        {
            BotVoice.Say(body, "guild", $"I have {_relic.Name}. Bringing it home — keep the road clear.", true);
        }
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        if (_relic.Place != BotRelicPlace.Carried || _relic.Bot != body.Serial)
        {
            Lost++;

            return BotDoing.Failed(_relic.Place == BotRelicPlace.Stolen ? $"{_relic.Name} was stolen on the road" : $"{_relic.Name} is not in my pack any more");
        }

        if (!body.InRange(_hall, Reach))
        {
            return BotDoing.Walk(_map, _hall, BotArrival.Within(Reach - 2), $"carrying {_relic.Name} home");
        }

        var guild = _relic.Guild;

        BotRelics.Hand(_relic, guild, _relic.Former);
        Home++;

        BotRelics.Tell(
            $"{body.Name} set {_relic.Name} up in the hall of {guild}",
            "{Name} carried {Relic} home and set it up {Where}",
            body.Name,
            _relic.Name,
            _relic.Where
        );

        BotVoice.Say(body, "guild", $"{_relic.Name} stands in our hall. Let every guild know it.", true);
        BotEvents.Post("relic", body, $"set {_relic.Name} up in the hall of {guild}", "home");

        return BotDoing.Done($"set {_relic.Name} up in the hall of {guild}");
    }

    public override void Drop(IBotWilful bot) =>
        logger.Debug("{Name} put down the walk home with {Relic}", bot?.Self?.Name, _relic.Name);

    public static string Describe() => !Running ? "off" : $"{Offered} walks home offered, {Walked} taken, {Home} relics set up in a hall, {Lost} lost on the road";

    public static void Forget()
    {
        Offered = Walked = Home = Lost = 0;
    }
}
