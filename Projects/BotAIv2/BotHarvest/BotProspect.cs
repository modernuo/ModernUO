using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A walk out past the last swept ground, so that there is rock on the board somewhere nobody has been.
///
/// <para>
/// <b>The island's ore is finite and the map of it is a map of where bots have already stood.</b>
/// <c>BotGround</c> says so at length and names the consequence a closed circle: the good rock is in the
/// mountains, no bot has a reason to walk to the mountains until rock is recorded there, and nothing records
/// rock there because no bot has been. It broke that circle once, at boot, by naming <c>BotGround.Lode</c>
/// from outside — one hand-picked cave, swept before anybody asked.
/// </para>
///
/// <para>
/// One cave is a day's mining. On 05.09.2026 the seam list ran 509 → 482 → 362 → 237 → 84 in two hours and
/// twenty minutes, with 445 of them struck off as barren and 1496 smiths short of metal behind it. The
/// answer cannot be a wider sweep — <c>Survey</c> only ever runs where a bot is standing, so a bigger radius
/// finds more of the same neighbourhood. It has to be a bot that goes somewhere else on purpose.
/// </para>
///
/// <para>
/// <b>So this errand carries no tool and digs nothing.</b> It walks to a point past the frontier and its
/// arrival is the whole of the work: <c>BotGround.Survey</c> fires wherever a miner stands, so a miner that
/// stands somewhere new has surveyed somewhere new, and every other miner on the shard reads the seams it
/// found off the same board. One bot's walk pays for the whole population's next day of ore.
/// </para>
/// </summary>
public sealed class BotProspect : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotProspect));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "prospect";

    /// <summary>
    /// What prospecting is reckoned at before the ledger knows better.
    ///
    /// <para>
    /// Low, and deliberately below every craft and every dig: this is what a miner does when there is
    /// nothing left to dig, not something it does instead of digging. The ledger will mark it up on its own
    /// if the walks keep ending in rock, because <c>Made</c> below counts the seams found.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 30.0;

    /// <summary>How long one is expected to take. A long walk and a look round.</summary>
    public static double WorkMinutes { get; set; } = 4.0;

    private readonly Map _map;

    private readonly Point3D _where;

    private readonly int _knew;

    private int _found;

    private bool _looked;

    public BotProspect(Map map, Point3D where, int knew)
    {
        _map = map;
        _where = where;
        _knew = knew;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>Nothing is swung, so nothing is trained. The walk is the work.</summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>Nothing here is coin, and what it produces is not goods either — it is a place to work.</summary>
    public override double Coin => 0.0;

    /// <summary>
    /// Seams put on the board, valued at what the ore in one is worth.
    ///
    /// So that the ledger can learn that a walk into the mountains pays, which is the only thing that will
    /// make the second one happen without being told.
    /// </summary>
    public override int Made => _found * BotOre.WorthSmelting;

    public override string Stage =>
        _looked
            ? $"prospected ({_found} new seams)"
            : $"prospecting out towards ({_where.X}, {_where.Y})";

    /// <summary>The ground out there turned out to be unreachable. Written under the ground's own name.</summary>
    public override bool Bend(IBotWilful bot)
    {
        bot?.Resolve?.Ledger?.Beware(Trade, _map, _where);

        return false;
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal || !body.Alive)
        {
            return BotDoing.Failed("no body");
        }

        if (!body.InRange(_where, BotGround.Reach / 2))
        {
            return BotDoing.Walk(_map, _where, BotArrival.Within(BotGround.Reach / 2), $"prospecting towards ({_where.X}, {_where.Y})");
        }

        // Standing where nobody has stood. The sweep is the errand, and it refuses politely if this ground
        // turns out to have been swept after all — which is why the count below is taken from the board
        // rather than from what Survey says it did.
        BotGround.Survey(_map, body.Location);

        // The sweep is walked a slice a tick on the population's clock (BotGround.Sweep, 01.10.2026, build 382): the prospector
        // stands where it is until the ground round it has been read, a few seconds, and counts what came of it then.
        if (BotGround.Sweeping(_map, body.Location))
        {
            return BotDoing.Work("waiting for the ground here to be swept");
        }

        _looked = true;
        _found = System.Math.Max(0, BotGround.Seams.Count - _knew);

        if (_found > 0)
        {
            BotGround.Found(_found);

            logger.Information(
                "{Name} prospected out to ({X}, {Y}) and put {Found} new seams on the board, which now holds {All}",
                body.Name,
                body.X,
                body.Y,
                _found,
                BotGround.Seams
            );

            return BotDoing.Done($"{_found} new seams out at ({body.X}, {body.Y})");
        }

        BotGround.FoundNothing();

        return BotDoing.Done($"nothing in the ground out at ({body.X}, {body.Y})");
    }
}
