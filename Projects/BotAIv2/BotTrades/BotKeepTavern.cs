using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// A cook's shift keeping a tavern: its meals out of the bank box if the pack is short of them, the walk to the inn, the table
/// opened, supper served to whoever the auction sends, and the kitchen shut when the meals or the shift run out (07.10.2026,
/// trades17).
///
/// <para>
/// <b>The shop's shape, at an inn.</b> Held from the first step against the next offer (<see cref="BotDeed.Steadfast"/>) because
/// guests are walking to it; standing still is the work once it is open (<see cref="BotDeed.Still"/>), and the shift's own clock
/// ends it; put down for what will not wait, the tavern is shut while the cook is away and opened again when it is back. Measured
/// as coin: the meals were the cook's before the shift and the coin is what the shift turned them into (<c>BotKeepShop</c>'s
/// arithmetic), so the ledger corrects the claim by what this cook's taverns really took.
/// </para>
/// </summary>
public sealed class BotKeepTavern : BotDeed
{
    public const string Trade = "tavern";

    private readonly Map _map;

    private readonly BotInns.Inn _inn;

    private readonly Point3D _bank;

    private readonly double _claim;

    private BotTavern _tavern;

    private bool _fetched;

    private int _shown = -1;

    private double _openBefore;

    private long _openedTick;

    private long _cryTick;

    private int _sold;

    private int _takings;

    private int _served;

    public BotKeepTavern(Map map, BotInns.Inn inn, Point3D bank, double claim)
    {
        _map = map;
        _inn = inn;
        _bank = bank;
        _claim = Math.Max(0.01, claim);
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _inn?.Spot ?? Point3D.Zero;

    public override double Expects => _claim;

    public override double Minutes => BotTaverns.ShiftMinutes;

    public override double Coin => 1.0;

    public override bool Still => _tavern != null;

    public override bool Committed => _tavern != null;

    public override bool Steadfast => true;

    public override double HoldsFor => _tavern != null ? Math.Max(1.0, BotTaverns.ShiftMinutes - OpenMinutes(Core.TickCount)) + 1.0 : 0.0;

    public override bool Hurries => _tavern == null;

    public override string Stage =>
        _tavern != null
            ? $"keeping {_tavern.Name}: {_tavern.Meals} meals left, {Sold} sold to {Served} for {Takings}gp"
            : !_fetched && _bank != Point3D.Zero
                ? $"fetching its meals from the bank at ({_bank.X}, {_bank.Y}) to keep a tavern at {_inn?.Name}"
                : $"going to {_inn?.Name} to keep a tavern";

    private int Sold => _sold + (_tavern?.Sold ?? 0);

    private int Takings => _takings + (_tavern?.Takings ?? 0);

    private int Served => _served + (_tavern?.Served ?? 0);

    private double OpenMinutes(long now) => _openBefore + (_tavern != null ? (now - _openedTick) / 60000.0 : 0.0);

    public override void Taken(IBotWilful bot)
    {
        if (bot?.Self is { } body)
        {
            BotTaverner.Went(body);
        }
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || _map == null || _inn == null || body.Map != _map)
        {
            return BotDoing.Failed("no body, or no inn on this map");
        }

        var now = Core.TickCount;

        if (!BotTrades.TavernsOn)
        {
            var note = Summary(now, "the taverns were shut by order");

            Shut(now, "the taverns were shut by order", false, false);

            return BotDoing.Done(note);
        }

        if (_tavern == null)
        {
            return Opening(body, now);
        }

        if (_tavern.Shut)
        {
            Carry(now);
            _tavern = null;

            return Opening(body, now);
        }

        if (now - _tavern.CountedTick >= 3000)
        {
            BotTaverns.Count(_tavern, now);
        }

        if (_tavern.Meals <= 0)
        {
            var note = Summary(now, "sold out");

            Shut(now, "sold out", true, false);

            return BotDoing.Done(note);
        }

        if (OpenMinutes(now) >= BotTaverns.ShiftMinutes)
        {
            var note = Summary(now, "the evening is over");

            Shut(now, "the evening is over", false, true);

            return BotDoing.Done(note);
        }

        if (!body.InRange(_tavern.Stand, 3))
        {
            return BotDoing.Walk(_map, _tavern.Stand, BotArrival.Within(1), $"back to its table at {_inn.Name}");
        }

        if (now - _cryTick >= BotTaverns.CryMs)
        {
            _cryTick = now;
            BotTaverns.Say(body, "tavern:cry", null, _tavern);
        }

        return BotDoing.Work(Stage);
    }

    private BotDoing Opening(BotMobile body, long now)
    {
        var pack = BotTaverns.MealsIn(body.Backpack);

        // <b>The meals out of the box first.</b> A cook banks what it cooks with everything else it carries (BotUnload), and the
        // inn has no bank to reach into: the box is opened at the bank on the way, as a keeper of a shop by the bank would.
        if (!_fetched && pack < BotTaverns.StockMeals && _bank != Point3D.Zero && (body.FindBankNoCreate()?.Items.Count ?? 0) > 0)
        {
            if (!BotMarket.AtBank(body))
            {
                return BotDoing.Walk(_map, _bank, BotArrival.Within(2), $"to the bank for the meals it cooked, to keep a tavern at {_inn.Name}");
            }

            var kinds = BotTaverns.Kinds;
            var took = 0;

            for (var i = 0; i < kinds.Count && pack + took < BotTaverns.StockMeals; i++)
            {
                took += BotMarket.FromBank(body, kinds[i], BotTaverns.StockMeals - pack - took);
            }

            _fetched = true;
            pack += took;
        }

        _fetched = true;

        if (pack <= 0)
        {
            return BotDoing.Failed($"no meals to serve at {_inn.Name}");
        }

        if (!body.InRange(_inn.Spot, 1))
        {
            return BotDoing.Walk(_map, _inn.Spot, BotArrival.Within(1), $"to {_inn.Name} to keep a tavern");
        }

        if (BotTaverns.At(_inn) is { } other && !ReferenceEquals(other.Keeper, body))
        {
            return BotDoing.Failed($"{other.Keeper?.Name} opened a tavern at {_inn.Name} first");
        }

        _tavern = BotTaverns.OpenAt(body, _inn, _map);

        if (_tavern == null)
        {
            return BotDoing.Failed($"no tavern could open at {_inn.Name}: another is open near it, or {BotTaverns.MostOpen} are open on the island");
        }

        _openedTick = now;
        _cryTick = now;

        if (_shown < 0)
        {
            _shown = _tavern.Meals;
        }

        return BotDoing.Work(Stage);
    }

    private string Summary(long now, string why)
    {
        var minutes = OpenMinutes(now);

        return Sold > 0
            ? $"{why}: served {Sold} meals to {Served} guests for {Takings}gp in {minutes:F1} minutes at {_inn?.Name}"
            : $"{why}: kept a tavern {minutes:F1} minutes at {_inn?.Name} and nobody came to eat";
    }

    private void Carry(long now)
    {
        if (_tavern == null)
        {
            return;
        }

        _openBefore += (now - _openedTick) / 60000.0;
        _sold += _tavern.Sold;
        _takings += _tavern.Takings;
        _served += _tavern.Served;
    }

    private void Shut(long now, string why, bool soldOut, bool timeUp)
    {
        if (_tavern == null)
        {
            return;
        }

        var tavern = _tavern;

        Carry(now);
        _tavern = null;

        BotTaverns.Close(tavern, why, soldOut, timeUp, soldOut || timeUp ? Math.Max(0, _shown) : 0);
    }

    public override bool Bend(IBotWilful bot)
    {
        bot?.Resolve?.Ledger?.Beware(Trade, _map, _inn?.Spot ?? Point3D.Zero);

        return false;
    }

    public override void Paused(IBotWilful bot) => Shut(Core.TickCount, "the cook was called away", false, false);

    public override void Drop(IBotWilful bot) => Shut(Core.TickCount, "the shift was given up", false, false);
}

/// <summary>
/// Offers a cook holding meals a tavern's shift at the nearest inn with none open near it (07.10.2026, trades17). The skillet
/// decides who cooks (<c>BotOven.Kit</c>), as it does for the cooking itself; the meals in the pack and the bank box decide
/// whether there is a supper to serve. Every gate counted.
/// </summary>
public sealed class BotTaverner : IBotProposer
{
    public string Name => "Taverner";

    public BotStanding Rung => BotStanding.Free;

    public static long Looks { get; private set; }

    public static long NoSkillet { get; private set; }

    public static long Rested { get; private set; }

    public static long Full { get; private set; }

    public static long Thin { get; private set; }

    public static long Outlawed { get; private set; }

    public static long NoInn { get; private set; }

    public static long Offered { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    /// <summary>A shift was taken: this cook rests from taverns for <see cref="BotTaverns.RestMs"/>.</summary>
    internal static void Went(Mobile cook)
    {
        if (cook != null)
        {
            _last[cook.Serial] = Core.TickCount;
        }
    }

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotTrades.TavernsOn || bot?.Self is not BotMobile body || !body.Alive || body.Tired || body.Squad != null)
        {
            return null;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        // Keeping one already.
        if (body.Resolve?.Deed is BotKeepTavern)
        {
            return null;
        }

        Looks++;

        if (BotOven.Kit(body) == null)
        {
            NoSkillet++;

            return null;
        }

        var now = Core.TickCount;

        if (_last.TryGetValue(body.Serial, out var when) && now - when < BotTaverns.RestMs)
        {
            Rested++;

            return null;
        }

        if (BotTaverns.Open.Count >= BotTaverns.MostOpen)
        {
            Full++;

            return null;
        }

        var box = body.FindBankNoCreate();
        var meals = BotTaverns.MealsIn(body.Backpack) + BotTaverns.MealsIn(box);

        if (meals < BotTaverns.LeastMeals)
        {
            Thin++;

            return null;
        }

        // A red name in a town is a fight with the guards, not a supper.
        if (body.Kills >= 5 || body.Criminal || BotUnderworld.Member(body))
        {
            Outlawed++;

            return null;
        }

        BotInns.Survey(map);

        var inn = BotTaverns.FreeInn(body);

        if (inn == null || !BotGates.Joined(map, body.Location, inn.Spot))
        {
            NoInn++;

            return null;
        }

        var kind = BotTaverns.MostHeld(body, out _) ?? typeof(Server.Items.Ribs);
        var bank = BotTaverns.MealsIn(body.Backpack) < BotTaverns.StockMeals && BotTaverns.MealsIn(box) > 0
            ? BotShopkeep.BankNear(map, inn.Spot)
            : Point3D.Zero;

        Offered++;

        return new BotKeepTavern(map, inn, bank, BotTaverns.Claim(meals, BotTaverns.Price(kind)));
    }

    public static string Describe() =>
        Looks == 0
            ? "nobody has been looked at for a tavern"
            : $"{Looks} looks for a tavern: {Offered} offered, {NoSkillet} had no skillet, {Thin} held under {BotTaverns.LeastMeals} meals, "
              + $"{Rested} were resting from one, {Full} found {BotTaverns.MostOpen} open, {Outlawed} were outlaws, {NoInn} had no free inn within {BotTaverns.KeeperReach} tiles";

    public static void Forget()
    {
        _last.Clear();
        Looks = 0;
        NoSkillet = 0;
        Rested = 0;
        Full = 0;
        Thin = 0;
        Outlawed = 0;
        NoInn = 0;
        Offered = 0;
    }
}
