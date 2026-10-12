using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Multis;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// Houses of the population's own: a bot that has done well buys one, spends its leisure there, and keeps its belongings in a
/// chest by the wall.
///
/// <para>
/// <b>Patrick's order of 26.09.2026, the fourth of the night:</b> "well-to-do bots may buy houses of their own, of different
/// kinds; they can pass the time in them, their mood rises there, they can have chests for their belongings. It is a status —
/// a sign that the bot has achieved a great deal." So a house is bought, not issued: by a bot whose pack and account cover
/// the price with <see cref="Keep"/> to spare, the largest of the four hall sizes it can afford (<see cref="BotHallKind"/>, at
/// the prices below — a house of one's own is dearer than a guild's first hall because one bot pays for it); the time is
/// <see cref="BotRepose"/>, the mood is boredom falling while it is there, and the chest is a wooden chest locked down inside.
/// </para>
///
/// <para>
/// <b>The world is the register, as it is for the halls.</b> A house is known by the name on its sign — "Nessa's house" — and
/// found again at every boot (<see cref="Adopt"/>); nothing else is saved. The engine keeps the rest: a bot owns its house,
/// and a bot deleted takes a house with no co-owners with it (<c>BaseHouse.HandleDeletion</c>), which is right — a wiped
/// island keeps nobody's house, and <c>do reset</c> razes them as it razes the halls.
/// </para>
/// </summary>
public static class BotAbode
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotAbode));

    /// <summary>Whether bots buy houses and go home to them at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>What each size costs one bot.</summary>
    public static int SmallPrice { get; set; } = 10000;

    public static int PatioPrice { get; set; } = 25000;

    public static int LargePatioPrice { get; set; } = 50000;

    public static int MarblePrice { get; set; } = 80000;

    /// <summary>What a buyer keeps after paying: a house is status, not ruin.</summary>
    public static int Keep { get; set; } = 2000;

    /// <summary>The most houses of the population's own on the island. Ground near the towns is scarce, and halls need it too.</summary>
    public static int Most { get; set; } = 20;

    /// <summary>How near another bot's house one may stand. The halls keep their own, wider distance (<c>BotPlot.Apart</c>).</summary>
    public static int Apart { get; set; } = 16;

    public const string Suffix = "'s house";

    public static long Bought { get; private set; }

    public static long Adopted { get; private set; }

    public static long Razed { get; private set; }

    public static long Visits { get; private set; }

    public static long Stowed { get; private set; }

    private static readonly Dictionary<string, BaseHouse> _houses = new(StringComparer.Ordinal);

    public static IEnumerable<BaseHouse> Houses => _houses.Values;

    public static int Count => _houses.Count;

    /// <summary>This bot's house, or null.</summary>
    public static BaseHouse Of(Mobile bot)
    {
        if (bot?.Name == null || !_houses.TryGetValue(bot.Name, out var house))
        {
            return null;
        }

        if (house is { Deleted: false })
        {
            return house;
        }

        _houses.Remove(bot.Name);

        return null;
    }

    /// <summary>What a house of this size costs one bot.</summary>
    public static int Price(BotHallKind kind) =>
        kind.Size switch
        {
            1 => SmallPrice,
            2 => PatioPrice,
            3 => LargePatioPrice,
            _ => MarblePrice
        };

    /// <summary>The largest size this much money buys with <see cref="Keep"/> to spare, or null.</summary>
    public static BotHallKind Affordable(int wealth)
    {
        BotHallKind best = null;

        for (var size = 1; size <= 4; size++)
        {
            var kind = BotHallKind.Sized(size);

            if (Price(kind) + Keep <= wealth)
            {
                best = kind;
            }
        }

        return best;
    }

    public static string SignFor(string name) => $"{name}{Suffix}";

    /// <summary>A house just bought.</summary>
    public static void Register(Mobile owner, BaseHouse house)
    {
        if (owner?.Name == null || house == null)
        {
            return;
        }

        _houses[owner.Name] = house;
        Bought++;
    }

    public static void Visited() => Visits++;

    public static void Stow(int many) => Stowed += Math.Max(0, many);

    /// <summary>
    /// Finds every house of the population's own by its sign and gives it back to its bot: owner, doors unlocked, decay
    /// refreshed. At boot, after the population is raised; a house whose bot is gone is left to the engine's decay.
    /// </summary>
    public static void Adopt()
    {
        if (!Running)
        {
            return;
        }

        foreach (var house in BaseHouse.AllHouses)
        {
            var sign = house?.Sign?.Name;

            if (house == null || house.Deleted || sign == null || !sign.EndsWith(Suffix, StringComparison.Ordinal))
            {
                continue;
            }

            var name = sign[..^Suffix.Length];
            var bot = Find(name);

            if (bot == null)
            {
                continue;
            }

            if (house.Owner != bot)
            {
                house.Owner = bot;
            }

            BotEstate.Unlock(house);
            house.RefreshDecay();
            _houses[name] = house;
            Adopted++;
        }

        if (_houses.Count > 0)
        {
            logger.Information("Houses of the population's own taken back from the world: {Count} ({Who})", _houses.Count, string.Join(", ", _houses.Keys));
        }
    }

    private static BotMobile Find(string name)
    {
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && bot.Name == name)
            {
                return bot;
            }
        }

        var away = BotPopulation.Away;

        for (var i = 0; i < away.Count; i++)
        {
            if (away[i] is { Deleted: false } bot && bot.Name == name)
            {
                return bot;
            }
        }

        return null;
    }

    /// <summary>The chest inside a house of one's own, or null.</summary>
    public static Container Chest(BaseHouse house)
    {
        var downs = house?.LockDowns;

        if (downs == null)
        {
            return null;
        }

        foreach (var item in downs)
        {
            if (item is WoodenChest { Deleted: false } chest)
            {
                return chest;
            }
        }

        return null;
    }

    /// <summary>Takes every house of the population's own off the island. For a wipe (<c>do reset</c>).</summary>
    public static int Raze()
    {
        var gone = 0;

        foreach (var house in _houses.Values)
        {
            if (house is { Deleted: false })
            {
                house.Delete();
                gone++;
            }
        }

        _houses.Clear();
        Razed += gone;

        return gone;
    }

    public static string Describe()
    {
        if (!Running)
        {
            return "no bot buys a house";
        }

        using var sb = ValueStringBuilder.Create(256);

        sb.Append($"houses of the population's own: {_houses.Count} standing, {Bought} bought, {Adopted} taken back at a boot, {Visits} visits home, {Stowed} things put away in a chest there");

        var first = true;

        foreach (var (name, house) in _houses)
        {
            sb.Append(first ? " (" : ", ");
            first = false;
            sb.Append($"{name}: {BotHallKind.Of(house)} at {house.X},{house.Y}");
        }

        if (!first)
        {
            sb.Append(')');
        }

        return sb.ToString();
    }

    /// <summary>A world reload: the houses stand; the register is rebuilt from their signs at the next boot.</summary>
    public static void Forget()
    {
        _houses.Clear();
        Bought = 0;
        Adopted = 0;
        Visits = 0;
        Stowed = 0;
    }
}

/// <summary>
/// Offers a well-to-do bot the purchase of a house of its own: the largest size its pack and account cover with
/// <see cref="BotAbode.Keep"/> to spare, on ground found from its guild's seat. See <see cref="BotAbode"/>.
/// </summary>
public sealed class BotAbodeBuyer : IBotProposer
{
    /// <summary>How often one bot is looked at for a house. Its wealth moves over hours.</summary>
    public static int LookMs { get; set; } = 600000;

    /// <summary>How often the island's ground is searched for a house at all: the search is the estate's dearest question.</summary>
    public static int SearchMs { get; set; } = 20000;

    public static long Asked { get; private set; }

    public static long Offered { get; private set; }

    public static long Poor { get; private set; }

    public static long Full { get; private set; }

    public static long Groundless { get; private set; }

    private static readonly Dictionary<Serial, long> _looked = [];

    private static long _searchedTick;

    private static bool _searched;

    public string Name => "house-buyer";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotAbode.Running || bot?.Self is not BotMobile { Deleted: false, Alive: true } body || body.Map == null
            || body.Map == Map.Internal || body.Class is not { } klass || klass.Unpaid)
        {
            return null;
        }

        // A bandit has a hideout, and a house with its name on the sign is a house the Baron knows where to find.
        if (BotUnderworld.Outlawed(body) || BotUnderworld.Member(body) || BotAbode.Of(body) != null)
        {
            return null;
        }

        var now = Core.TickCount;

        if (_looked.TryGetValue(body.Serial, out var looked) && now - looked < LookMs)
        {
            return null;
        }

        _looked[body.Serial] = now;
        Asked++;

        if (BotAbode.Count >= BotAbode.Most)
        {
            Full++;

            return null;
        }

        var wealth = BotYield.Spendable(body);
        var kind = BotAbode.Affordable(wealth);

        if (kind == null)
        {
            Poor++;

            return null;
        }

        if (_searched && now - _searchedTick < SearchMs)
        {
            // Asked again at its next look rather than now; the throttle is the island's, not this bot's.
            _looked.Remove(body.Serial);

            return null;
        }

        _searched = true;
        _searchedTick = now;

        // Round where the bot lives (29.09.2026): its guild's hall or seat, or its town. See BotPopulation.HomeOf.
        var from = BotPopulation.HomeOf(body);

        if (!BotPlot.FindHome(body, from, kind.Multi, out var plot))
        {
            Groundless++;

            return null;
        }

        Offered++;

        return new BotAbodeBuy(body.Map, kind, plot);
    }

    public static string Describe() =>
        Asked == 0
            ? "no bot has been looked at for a house"
            : $"{Asked} bots looked at for a house of their own: {Offered} offered one, {Poor} could not afford even a small house with {BotAbode.Keep}gp to spare, {Full} found the island's quota full, {Groundless} found no ground; {BotAbodeBuy.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        Offered = 0;
        Poor = 0;
        Full = 0;
        Groundless = 0;
        _looked.Clear();
        _searched = false;
        BotAbodeBuy.Forget();
    }
}

/// <summary>
/// A bot buys its house: walks to the ground, proves it with the engine, pays out of its pack and then its account, and puts
/// the house up with its name on the sign and a chest inside. See <see cref="BotAbode"/>.
/// </summary>
public sealed class BotAbodeBuy : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotAbodeBuy));

    public const string Trade = "house";

    public static double Prior { get; set; } = 350.0;

    public static double WorkMinutes { get; set; } = 2.0;

    public static int Reach { get; set; } = 5;

    public static long Done { get; private set; }

    public static long Refused { get; private set; }

    public static long Short { get; private set; }

    private readonly Map _map;

    private readonly BotHallKind _kind;

    private readonly Point3D _plot;

    public BotAbodeBuy(Map map, BotHallKind kind, Point3D plot)
    {
        _map = map;
        _kind = kind;
        _plot = plot;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _plot;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override bool Unpaid => true;

    public override string Stage => $"buying {_kind} of its own at {_plot.X},{_plot.Y}";

    public override bool Bend(IBotWilful bot) => false;

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null)
        {
            return BotDoing.Failed("no body");
        }

        if (BotAbode.Of(body) != null)
        {
            return BotDoing.Done("it has a house already");
        }

        if (!body.InRange(_plot, Reach))
        {
            return BotDoing.Walk(_map, _plot, BotArrival.Within(Reach), $"to the ground for a house at {_plot.X},{_plot.Y}");
        }

        // Not on the ground of a guild neither the bot's own nor allied. See BotBounds.
        if (BotBounds.Refuse(BotBounds.Placing.Abode, (body.Guild as Server.Guilds.Guild)?.Name, _map, _plot, _kind.Multi, out var foreign))
        {
            BotPlot.Spend();

            return BotDoing.Failed(foreign);
        }

        var result = HousePlacement.Check(body, _kind.Multi, _plot, out var toMove, Direction.South);

        if (result != HousePlacementResult.Valid)
        {
            BotPlot.Spend();
            Refused++;

            return BotDoing.Failed($"the ground would not take {_kind}: {result}");
        }

        for (var i = 0; i < toMove.Count; i++)
        {
            if (toMove[i] is Item)
            {
                Refused++;

                return BotDoing.Failed("there are things lying on the ground");
            }
        }

        var price = BotAbode.Price(_kind);
        var wealth = BotYield.Spendable(body);

        if (wealth < price + BotAbode.Keep || !Pay(body, price))
        {
            Short++;

            return BotDoing.Failed($"{price}gp for {_kind} is more than it can spare now");
        }

        BotPlot.Spend();

        var house = _kind.Make(body);

        if (house == null || house.Deleted)
        {
            Banker.Deposit(body, price);
            Refused++;

            return BotDoing.Failed($"{_kind} would not go up");
        }

        house.Price = price;
        house.MoveToWorld(_plot, _map);

        for (var i = 0; i < toMove.Count; i++)
        {
            if (toMove[i] is Mobile mobile)
            {
                mobile.Location = house.BanLocation;
            }
        }

        if (house.Sign != null)
        {
            house.Sign.Name = BotAbode.SignFor(body.Name);
        }

        BotEstate.Unlock(house);
        house.RefreshDecay();

        var chest = new WoodenChest();
        var spot = BotFittings.Spot(house);

        chest.MoveToWorld(spot == Point3D.Zero ? house.BanLocation : spot, _map);

        if (!house.LockDown(body, chest))
        {
            chest.Movable = false;
        }

        BotYield.Aside(body, price);
        BotAbode.Register(body, house);
        Done++;

        logger.Information(
            "{Name} the {Class} has bought {Kind} of its own at {X},{Y} for {Price}gp — {Wealth}gp free before, {Left}gp left",
            body.Name,
            (body as BotMobile)?.Class?.Name,
            _kind,
            _plot.X,
            _plot.Y,
            price,
            wealth,
            BotYield.Wealth(body)
        );

        BotVoice.Aloud(body, "A roof of my own, at last.");

        return BotDoing.Done($"bought {_kind} at {_plot.X},{_plot.Y} for {price}gp");
    }

    /// <summary>The price out of the pack first and the account after, all of it or none.</summary>
    private static bool Pay(Mobile body, int price)
    {
        var pack = body.Backpack;

        if (pack == null)
        {
            return false;
        }

        var carried = pack.GetAmount(typeof(Gold));
        var fromPack = Math.Min(carried, price);
        var fromBank = price - fromPack;

        if (fromPack > 0 && !pack.ConsumeTotal(typeof(Gold), fromPack))
        {
            return false;
        }

        if (fromBank > 0 && !Banker.Withdraw(body, fromBank))
        {
            if (fromPack > 0)
            {
                pack.DropItem(new Gold(fromPack));
            }

            return false;
        }

        return true;
    }

    public static string Describe() =>
        Done + Refused + Short == 0
            ? "no house bought"
            : $"{Done} houses bought, {Refused} refused by the ground, {Short} short of the price at the last moment";

    public static void Forget()
    {
        Done = 0;
        Refused = 0;
        Short = 0;
    }
}
