using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Hiring a merchant for the guild's hall: buy the contract in town, walk it home, and set the shopkeeper up.
///
/// <para>
/// <b>Patrick's order of 09.09.2026, and he had the engine right.</b> A contract of employment is sold over a
/// counter by bankers, barkeepers and innkeepers — <c>SBBanker</c>, <c>SBBarkeeper</c>, <c>SBInnKeeper</c> —
/// at 1,252gp. So the price is the engine's own and not ours, which is the rule everywhere on this shard
/// except the workbenches, where no engine price exists at all.
/// </para>
///
/// <para>
/// <b>The engine's own conditions are met rather than worked around.</b> <c>ContractOfEmployment</c> asks for
/// four things — the contract in the pack, a house at the bot's feet, the bot a friend of it, and the house
/// public — and a guild hall satisfies every one: our halls are public and make every member a co-owner,
/// which <c>BaseHouse.IsFriend</c> accepts. The one thing this does differently from a player double-clicking
/// the contract is that it walks the bot into the room first, because "a house at the bot's feet" means the
/// bot has to be standing in it.
/// </para>
///
/// <para>
/// Who may use it is the hall's question and already answered: a merchant stands inside a guild hall, and
/// <see cref="BotEstate.MayUse"/> refuses anything in there to anybody outside the guild — the same rule,
/// word for word, as the one that keeps a forge private.
/// </para>
/// </summary>
public sealed class BotHire : BotDeed
{
    /// <summary>The ledger key.</summary>
    public const string Trade = "hire";

    /// <summary>What hiring is reckoned at per minute before experience corrects it.</summary>
    public static double Prior { get; set; } = 300.0;

    /// <summary>How long the hiring itself takes once the bot is standing in the hall.</summary>
    public static double WorkMinutes { get; set; } = 1.0;

    /// <summary>How near the shopkeeper the bot must be to buy. The engine's own counter reach.</summary>
    public static int Reach => BotShops.CounterReach;

    private readonly Guilds.Guild _guild;

    private readonly BaseHouse _hall;

    private readonly BaseVendor _shop;

    private readonly int _price;

    private bool _bought;

    private bool _hired;

    public BotHire(Guilds.Guild guild, BaseHouse hall, BaseVendor shop, int price)
    {
        _guild = guild;
        _hall = hall;
        _shop = shop;
        _price = price;
    }

    public override string Kind => Trade;

    public override Map Map => _hall?.Map;

    public override Point3D Where => _bought ? _hall?.Location ?? Point3D.Zero : _shop?.Location ?? Point3D.Zero;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    /// <summary>Nothing out of this bot's own pocket: the guild is buying. See <c>BotHall.Outlay</c>.</summary>
    public override int Outlay => 0;

    /// <summary>The contract is bought over a counter; once it is in the pack the walk to the hall needs no coin. See <see cref="BotDeed.AtCounter"/>.</summary>
    public override bool AtCounter => _shop != null && !_bought;

    public override double Coin => 0.0;

    /// <summary>Not about money, and not to be refused for failing to earn any.</summary>
    public override bool Unpaid => true;

    /// <summary>
    /// What the bot's own purse put into this, handed straight back as takings.
    ///
    /// <para>
    /// <b>Every errand a guild pays for was teaching the shard that guild errands lose money.</b> Takings are
    /// <c>coin + made</c>, coin is measured as the change in the bot's own purse, and these errands are paid
    /// for by a levy that comes partly out of that purse — so the ledger recorded "raised The Lantern for
    /// 5000gp: -20 in 0.4 min (-50/min)", "set an oven up: -33", "hired a merchant: -148", "left 20 ash on
    /// the counter: -60". <c>BotCommons.Corrected</c> then drags the trade's whole estimate towards those
    /// numbers, which is why <c>BotHall.Prior</c> carries a note about having to be raised from ninety to
    /// four hundred before anybody would take one.
    /// </para>
    ///
    /// <para>
    /// The fix is the one <c>BotRestock</c> has always used and says in as many words: goods are worth what
    /// they cost. A hall, a bench, a shopkeeper and a shelf of reagents are all worth what was paid for them,
    /// and the bot's share of the price is a contribution to something the guild now owns rather than money
    /// that left the world. So the errand comes out at about nothing a minute — never punished, never
    /// preferred over work that actually produces something, which is the right place for it.
    /// </para>
    /// </summary>
    public override int Made => _mine;

    /// <summary>Coin out of this bot's own purse, measured across the payment rather than assumed.</summary>
    private int _mine;

    /// <summary>
    /// How many beats a leg of this errand may go without getting nearer before it is given up.
    ///
    /// <para>
    /// <b>An errand that only ever answers "walk" is immortal, and this one was.</b> Measured 09.09.2026:
    /// Quill held a hiring errand for eleven minutes, reported every time as "walking to somewhere 1 tile off",
    /// and never finished, failed or dropped it — because the bot had been taken onto the Bound rung by a
    /// company on the way, and a bound bot's auction is switched off, so nothing was ever going to replace
    /// the errand it was holding. The whole time it held its guild's supplier claim, so no other member could
    /// be sent either. See <c>bot-frozen-work-family</c>: work that answers Work, or the same walk, for ever
    /// is work that never ends.
    /// </para>
    ///
    /// <para>
    /// Measured as progress rather than as attempts, which is the distinction <c>BotDig.TrekLimit</c> pays
    /// for at length: a long walk across the island is legitimate, and what is not legitimate is a walk that
    /// stops getting closer.
    /// </para>
    ///
    /// <para>
    /// <b>Two hundred, and forty was wrong by five times over — the same units mistake this file's neighbour
    /// was opened to fix.</b> A bot is beaten once per step, not once per tick, so forty beats is eight to
    /// sixteen seconds of not gaining ground: an ordinary detour round a building. Measured within ten
    /// minutes of it going in — five supply runs failed with "the walk to Delano stopped closing" while the
    /// shops were perfectly reachable. Two hundred is <c>BotDig.TrekLimit</c>, which carries the reasoning
    /// and the measurement, and taking its number rather than choosing a second one is the point.
    /// </para>
    /// </summary>
    public static int TrekLimit { get; set; } = 200;

    /// <summary>The nearest this errand has got to what it is currently walking at.</summary>
    private int _nearest = int.MaxValue;

    private int _stalled;

    private int _routeStamp = -1;

    /// <summary>Whether the walk is still closing. Resets when the errand changes what it is walking at.</summary>
    private bool Closing(Mobile body, Point3D at)
    {
        // By the road the bot is walking, and started again with each route it draws. See BotJourney.RoadLeft.
        var gap = System.Math.Max(System.Math.Abs(body.X - at.X), System.Math.Abs(body.Y - at.Y));

        if (body is BotMobile { Journey: { } journey })
        {
            gap = journey.RoadLeft(body.Location, at);

            if (_routeStamp != journey.RouteStamp)
            {
                _routeStamp = journey.RouteStamp;
                _nearest = int.MaxValue;
                _stalled = 0;
            }
        }

        if (gap < _nearest)
        {
            _nearest = gap;
            _stalled = 0;

            return true;
        }

        // A held walk is not a walk that stopped closing (12.10.2026, w7): a buyer stood for its carrier (BotRendezvous.Stand)
        // or a trader for its guards stands by somebody else's word, and BotWill's own watch skips those beats too.
        if (body is BotMobile { Journey.Held: true })
        {
            return true;
        }

        return ++_stalled < TrekLimit;
    }

    /// <summary>A new leg, so the old leg's best distance means nothing. Called when the target changes.</summary>
    private void Fresh()
    {
        _nearest = int.MaxValue;
        _stalled = 0;
    }


    public override string Stage =>
        _hired ? $"hired a merchant for {_guild?.Name}"
            : _bought ? $"carrying a contract to the hall of {_guild?.Name}"
            : $"to {_shop?.Name} for a contract of employment";

    public override bool Bend(IBotWilful bot) => false;

    public override void Drop(IBotWilful bot) => BotHirer.Release(_guild);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _guild == null || _hall is not { Deleted: false })
        {
            return BotDoing.Failed("the hall is gone");
        }

        BotHirer.Hold(_guild);

        if (BotEstate.Merchants(_hall) >= BotEstate.MostMerchants)
        {
            return BotDoing.Failed($"{_guild.Name} has all the merchants it may have");
        }

        var pack = body.Backpack;

        if (pack == null)
        {
            return BotDoing.Failed("no pack to carry a contract in");
        }

        var contract = pack.FindItemByType<ContractOfEmployment>();

        if (contract == null)
        {
            if (_shop is not { Deleted: false } || _shop.Map == null)
            {
                return BotDoing.Failed("the shopkeeper is gone");
            }

            if (!body.InRange(_shop.Location, Reach))
            {
                if (!Closing(body, _shop.Location))
                {
                    bot?.Resolve?.Ledger?.Beware(BotShops.ShopKind, _shop.Map, _shop.Location);

                    return BotDoing.Failed($"the walk to {_shop.Name} stopped closing {_nearest} tiles short");
                }

                return BotDoing.Walk(_shop.Map, _shop, BotArrival.Within(Reach), $"to {_shop.Name} for a contract");
            }

            // <b>The guild pays by the estate's levy, the same collection its gate asked (06.10.2026, work15).</b> The
            // hirer offers when BotEstate.Fund says the guild can pay: the chest plus every member's spare above the keep.
            // This paid through BotGuilds.Stand, which never touches the chest and takes only a quarter of each mate's
            // spare: two thresholds on one shelf. On 04.10 "could not raise 1252gp for a contract" 660 times against 2
            // hires; at 00:46 on 06.10 the hirer offered 137 times and was held off for want of money 0 times while
            // every one of them failed at the counter. Levy draws the chest first and then the members, exactly what
            // Fund adds up, and puts it in the buyer's account because that is where a shopkeeper takes it from.
            var mine = BotYield.Wealth(body);
            var paid = new List<BotEstate.Contribution>();
            var got = BotEstate.Levy(_guild, _price, paid, body);

            if (got < _price)
            {
                BotEstate.Refund(paid, body);

                // A guild that could not pay is not asked again at once. See BotHirer.Rest.
                BotHirer.Rest(_guild);

                return BotDoing.Failed($"{_guild.Name} could only raise {got} of {_price}gp for a contract");
            }

            var chest = got;

            for (var i = 0; i < paid.Count; i++)
            {
                chest -= paid[i].Paid;
            }

            Banker.Deposit(body, got);

            if (BotShops.Buy(bot, _shop, typeof(ContractOfEmployment), 1, out var refused) <= 0)
            {
                // Nothing bought: every coin goes back where it came from, the chest's share to the chest.
                if (Banker.Withdraw(body, got))
                {
                    BotEstate.Refund(paid, body);
                    BotChest.Return(_guild.Name, chest);
                }

                BotHirer.Rest(_guild);

                return BotDoing.Failed(refused ?? "the shopkeeper would not sell a contract");
            }

            // This bot's own share of the price, handed back as takings. See Made.
            _mine += System.Math.Max(0, mine - BotYield.Wealth(body));

            _bought = true;

            // A different place to be walking at. See Closing.
            Fresh();

            return BotDoing.Work("a contract of employment bought");
        }

        _bought = true;

        // <b>Inside the hall, not beside it.</b> The engine finds the house under the bot's own feet, so a
        // merchant cannot be set up from the doorstep however plainly the hall is in front of it.
        // <b>In the room, not merely in the multi.</b> FindHouseAt says yes on the front steps, so asking it
        // alone put the first merchant this shard hired in the doorway — the porch trap, a second time. What
        // counts is the tile the flood reaches. See BotFittings.InRoom.
        if (!BotFittings.InRoom(_hall, body.Location))
        {
            var spot = BotFittings.Spot(_hall);

            if (spot == Point3D.Zero)
            {
                return BotDoing.Failed($"there is nowhere in the hall of {_guild.Name} to stand a merchant");
            }

            if (!Closing(body, spot))
            {
                return BotDoing.Failed($"the walk into the hall of {_guild.Name} stopped closing {_nearest} tiles short");
            }

            return BotDoing.Walk(_hall.Map, spot, BotArrival.Exactly, $"into the hall of {_guild.Name}");
        }

        if (!_hall.Public || !_hall.CanPlaceNewVendor())
        {
            return BotDoing.Failed("the hall will not take a merchant");
        }

        // <b>At the far end of the room, not under the bot's feet — the doorway trap, a third time.</b> The walk
        // above aims at the tile furthest from the door, but it is abandoned the moment the bot stands anywhere
        // in the room, and the first tile of any room is the one behind the door. So every merchant on this
        // shard was set down there. In a small hall that tile is the only way to the door, a merchant cannot
        // be asked to step aside, and the engine lets a player shove past one only at full stamina: on
        // 21.09.2026 Edda walked into The Hammer's hall to use its forge and could not get out again — the
        // planner's own road began at (1754, 1484), which is where Renaldo stood — failing seven errands in ten
        // minutes and striking three seams off the island's map as unreachable from inside a house.
        var stand = BotFittings.Spot(_hall);

        if (stand == Point3D.Zero)
        {
            return BotDoing.Failed($"there is nowhere in the hall of {_guild.Name} to stand a merchant");
        }

        BaseHouse.IsThereVendor(stand, body.Map, out var standing, out var rental);

        if (standing || rental)
        {
            return BotDoing.Failed("something is already standing on that spot");
        }

        var merchant = new PlayerVendor(body, _hall)
        {
            Direction = body.Direction & Direction.Mask
        };

        merchant.MoveToWorld(stand, body.Map);
        merchant.SayTo(body, 503246); // Ah! it feels good to be working again.

        contract.Delete();

        BotEstate.Hired(_hall, merchant, _price);
        _hired = true;

        return BotDoing.Done($"a merchant stands in the hall of {_guild.Name}, {_price}gp for the contract");
    }
}
