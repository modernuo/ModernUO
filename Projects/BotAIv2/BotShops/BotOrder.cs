using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Saying aloud what the bot wants and what it will pay: a want on the market, spoken in the trade chat.
///
/// <para>
/// <b>Ordering only, and it holds no gold any more (01.10.2026).</b> An order used to put its money down in escrow on the
/// auction's board and wait for a supplier to fill it from anywhere, the goods then fetched by a reflex. Since Patrick's order of
/// 30.09 every order is hand to hand: this says the want (<see cref="BotMarket.Ask"/>), the buyer keeps its gold, and the thing
/// arrives with a carrier who walks it over and is paid at the meeting (<see cref="BotDeliver"/>), or the buyer walks to a shop
/// that holds it (<see cref="BotPatron"/>). The buyer must be able to pay what it asks for when it asks — the market refuses
/// a want the purse could not honour.
/// </para>
/// </summary>
public sealed class BotOrder : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotOrder));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "order";

    private readonly Map _map;

    private readonly Point3D _where;

    private readonly IBotWilful _buyer;

    private readonly System.Type _kind;

    private readonly int _offer;

    private readonly int _units;

    private bool _posted;

    /// <summary>
    /// An order, or nothing at all when the market has no room for one.
    ///
    /// <para>
    /// <b>The gate is here, in the choosing, and it used to be on the errand's first beat.</b> Every proposer that reaches for an
    /// order returns whatever this hands back, so a null is simply a candidate passed over and costs nothing; the errand failing
    /// instead is offered again on the very next decision — 176 failures in a half-hour window on 04.09.2026, 85 of them one bot
    /// asking for one scroll.
    /// </para>
    /// </summary>
    public static BotOrder For(Map map, Point3D where, IBotWilful buyer, System.Type kind, int offer, int units = 1) =>
        BotMarket.Full || !BotMarket.Running ? null : new BotOrder(map, where, buyer, kind, offer, units);

    public BotOrder(Map map, Point3D where, IBotWilful buyer, System.Type kind, int offer, int units = 1)
    {
        _map = map;
        _where = where;
        _buyer = buyer;
        _kind = kind;
        _offer = offer;
        _units = System.Math.Max(1, units);
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => BotUpkeep.Prior;

    /// <summary>Short. It is the paperwork around somebody else's work.</summary>
    public override double Minutes => 0.5;

    /// <summary>Said from where the bot stands and over in a beat, so never a reason to put work down. See <see cref="BotDeed.Paperwork"/>.</summary>
    public override bool Paperwork => true;

    /// <summary>
    /// What the want will cost when it is answered: the buyer must be able to pay it, or the market will not take the want. Nothing
    /// leaves the purse now — it is paid at the meeting — so this is the whole of what "can it afford to ask" means.
    /// </summary>
    public override int Outlay => _offer * System.Math.Max(1, _units);

    /// <summary>
    /// Nothing: no coin moves and nothing is made. The escrow this used to declare as made (so that the ledger would not read an
    /// order as a loss) is gone with the escrow; saying a want costs nothing and teaches the ledger that it costs nothing.
    /// </summary>
    public override int Made => 0;

    public override string Stage => $"ordering {_units} {_kind.Name}";

    /// <summary>What is being ordered, how many, and at what, for whoever wants to say so out loud.</summary>
    public System.Type Wanted => _kind;

    public int Units => _units;

    public int Price => _offer;

    /// <summary>The part of a refusal that is the reservation's (02.10.2026, build 391): money its other wants already hold.</summary>
    private static string Held(Mobile body)
    {
        var held = BotMarket.Reserved(body);

        return held > 0 ? $", with {held}gp of its {BotYield.Wealth(body)}gp held for its other wants" : "";
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        if (_posted)
        {
            // The want stands on its own from here: it raises its own offer and a carrier finds the buyer wherever it is.
            return BotDoing.Done($"{_kind.Name} is asked for");
        }

        _posted = true;

        var want = BotMarket.Ask(_buyer ?? bot, _kind, System.Math.Max(1, _units), _offer);

        if (want == null)
        {
            return BotDoing.Failed(
                BotMarket.Full
                    ? $"the market already holds {BotMarket.MaxWants} wants"
                    : $"could not pay for {System.Math.Max(1, _units)} {_kind.Name} at {_offer}gp if they came{Held(body)}"
            );
        }

        logger.Information(
            "{Name} has asked the island for {Units} {Item} at {Offer}gp each",
            body.Name,
            System.Math.Max(1, _units),
            _kind.Name,
            want.Offer
        );

        return BotDoing.Done($"{System.Math.Max(1, _units)} {_kind.Name} asked for at {want.Offer}gp each");
    }
}
