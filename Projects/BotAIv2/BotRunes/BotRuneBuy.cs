using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// A rune-mage buying blank runes where the engine sells them — a mage's shop, at fifteen gold — with its guild's money when
/// its own will not stretch to it.
///
/// <para>
/// <b>The blank is the currency of the whole mechanism.</b> A mark spends one, and so does every copy the library lends
/// (<see cref="BotRuneLibrary"/>), so a guild whose mages carry none marks nothing and spreads nothing. The purchase is
/// funded as a delve's supplies are (<see cref="BotProvision.Fund"/>): the mage's own purse, then the guild's chest, then
/// its guildmates. The runes bought are the mage's own — kept through death, weightless, never sold — or the unloading
/// would put them back on the market on the walk home.
/// </para>
/// </summary>
public sealed class BotRuneBuy : BotDeed
{
    public const string Trade = "blanks";

    /// <summary>What a trip for blanks is reckoned at a minute. Twelve, a restock's: an errand, not a trade.</summary>
    public static double Prior { get; set; } = 12.0;

    public static long Undertaken { get; private set; }

    public static long Bought { get; private set; }

    public static long Unfunded { get; private set; }

    public static long Refused { get; private set; }

    private BaseVendor _shop;

    private readonly int _want;

    private readonly int _price;

    public BotRuneBuy(BaseVendor shop, int want, int price)
    {
        _shop = shop;
        _want = want;
        _price = price;
    }

    public override string Kind => Trade;

    public override Map Map => _shop?.Map;

    public override Point3D Where => _shop?.Location ?? Point3D.Zero;

    public override double Expects => Prior;

    public override double Minutes => 1.0;

    public override bool Unpaid => true;

    public override bool AtCounter => true;

    public override double Coin => 0.0;

    public override bool Steadfast => true;

    public override string Stage => $"after {_want} blank runes at {_shop?.Name}";

    public override void Taken(IBotWilful bot) => Undertaken++;

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self as BotMobile;

        if (body is not { Deleted: false, Alive: true })
        {
            return BotDoing.Failed("no body");
        }

        if (_shop is not { Deleted: false } || _shop.Map == null || _shop.Map == Map.Internal)
        {
            return BotDoing.Failed("the shopkeeper is gone");
        }

        if (!body.InRange(_shop.Location, BotShops.CounterReach))
        {
            return BotDoing.Walk(_shop.Map, _shop, BotArrival.Within(BotShops.CounterReach), $"to {_shop.Name} for blank runes");
        }

        var cost = _want * _price;

        if (!BotProvision.Fund(body, cost))
        {
            Unfunded++;
            BotRuneBuyer.Rest(body);

            return BotDoing.Failed($"neither it nor its guild could find {cost}gp for {_want} blank runes");
        }

        var got = BotShops.Buy(bot, _shop, typeof(RecallRune), _want, out var refused);

        if (got <= 0)
        {
            Refused++;
            BotRuneBuyer.Rest(body);

            return BotDoing.Failed(refused ?? "the shop would not sell blank runes");
        }

        Bought += got;

        // The runes just bought are the mage's own, before the unloading can reach them.
        foreach (var rune in body.Backpack.FindItemsByType<RecallRune>())
        {
            if (rune is { Deleted: false, Marked: false } && !BotBinding.IsBound(rune, body.Bond))
            {
                BotRunes.Bind(body, rune);
            }
        }

        return BotDoing.Done($"{got} blank runes from {_shop.Name}");
    }

    public static void Forget()
    {
        Undertaken = 0;
        Bought = 0;
        Unfunded = 0;
        Refused = 0;
    }
}

/// <summary>Offers a rune-mage short of blanks a trip to the nearest shop that sells them. See <see cref="BotRuneBuy"/>.</summary>
public sealed class BotRuneBuyer : IBotProposer
{
    /// <summary>How long a mage that failed to buy blanks is left alone before it is offered the trip again. Ten minutes.</summary>
    public static int RestMs { get; set; } = 600000;

    public static long Asked { get; private set; }

    public static long Stocked { get; private set; }

    public static long NoSpell { get; private set; }

    public static long NoShop { get; private set; }

    public static long Resting { get; private set; }

    public static long Offered { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    public string Name => "Blanks";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>A trip that failed rests the offer for <see cref="RestMs"/>: an empty purse or an empty shelf is not mended by asking again.</summary>
    public static void Rest(Mobile body)
    {
        if (body != null)
        {
            _last[body.Serial] = Core.TickCount;
        }
    }

    private static bool Rested(Mobile body) =>
        _last.TryGetValue(body.Serial, out var when) && Core.TickCount - (when + RestMs) < 0;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;

        if (!BotRunes.Mage(body) || body is not BotMobile mage || mage.Guild == null)
        {
            return null;
        }

        Asked++;

        if (BotRunes.BlankCount(body) >= BotRunes.Blanks)
        {
            Stocked++;

            return null;
        }

        // A rune is for marking and for copies; a mage that can cast none of the three has no use for one yet.
        if (!BotRunes.Knows(body, BotRunes.Mark) && !BotRunes.Knows(body, BotRunes.Recall) && !BotRunes.Knows(body, BotRunes.Gate))
        {
            NoSpell++;

            return null;
        }

        if (Rested(body))
        {
            Resting++;

            return null;
        }

        var shop = BotShops.Nearest(bot, typeof(RecallRune));
        var price = shop == null ? 0 : BotShops.Price(shop, typeof(RecallRune));

        if (shop == null || price <= 0)
        {
            NoShop++;

            return null;
        }

        Offered++;

        return new BotRuneBuy(shop, BotRunes.Blanks - BotRunes.BlankCount(body), price);
    }

    public static string Describe() =>
        Asked == 0
            ? "no rune-mage has been asked about blanks"
            : $"{Asked} rune-mages asked about blanks: {Offered} sent to a shop, {Stocked} carried {BotRunes.Blanks} already, {NoSpell} could cast none of the three, {NoShop} knew no shop selling them, {Resting} within {RestMs / 60000} minutes of a failed trip; {BotRuneBuy.Undertaken} trips taken, {BotRuneBuy.Bought} blanks bought, {BotRuneBuy.Unfunded} the guild could not fund, {BotRuneBuy.Refused} refused at the counter";

    public static void Forget()
    {
        Asked = 0;
        Stocked = 0;
        NoSpell = 0;
        NoShop = 0;
        Resting = 0;
        Offered = 0;
        _last.Clear();
        BotRuneBuy.Forget();
    }
}
