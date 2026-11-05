using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>One tavern while it is open: the cook keeping it, the inn it stands in, the table round it, and what it has sold.</summary>
public sealed class BotTavern
{
    public BotTavern(BotMobile keeper, BotInns.Inn inn, Map map, Point3D stand)
    {
        Keeper = keeper;
        Inn = inn;
        Map = map;
        Stand = stand;
        OpenedTick = Core.TickCount;
        NextLineTick = OpenedTick;
    }

    public BotMobile Keeper { get; }

    public BotInns.Inn Inn { get; }

    public Map Map { get; }

    public Point3D Stand { get; }

    public long OpenedTick { get; }

    /// <summary>Meals in the cook's pack at the last count.</summary>
    public int Meals { get; internal set; }

    public long CountedTick { get; internal set; }

    public int Sold { get; internal set; }

    public int Takings { get; internal set; }

    /// <summary>Guests who ate here, lodgers among them.</summary>
    public int Served { get; internal set; }

    public bool Shut { get; internal set; }

    public long NextLineTick { get; internal set; }

    public Mobile LastSpeaker { get; internal set; }

    /// <summary>The dice game running at the table, if one is (B5); one line a table-beat, in memory only.</summary>
    internal BotDice.Game Dice;

    public int Lines { get; internal set; }

    /// <summary>Who sits at the table, and when each was last seen sitting there.</summary>
    internal readonly Dictionary<Serial, (BotMobile Bot, long Seen, bool Greeted)> Table = [];

    public string Name => $"{Keeper?.Name}'s table at {Inn?.Name}";

    /// <summary>Guests seen at the table within the last five seconds.</summary>
    public int SeatedCount(long now)
    {
        var n = 0;

        foreach (var (_, seat) in Table)
        {
            if (now - seat.Seen <= 5000 && seat.Bot is { Deleted: false, Alive: true })
            {
                n++;
            }
        }

        return n;
    }
}

/// <summary>
/// The taverns: a cook keeping one at an inn, the meals it sells there, the guests at its table, and the hooks by which the rest's
/// clock, the inns and the lodging errand learn that a tavern is open (07.10.2026, trades17).
///
/// <para>
/// <b>A tavern is a fire indoors, with supper.</b> A guest who has bought and eaten a meal at the table (the cook's own cooking,
/// pack to pack, at the market's price for it: <see cref="BotHandover.Exchange"/>) sits there, and while it sits its play is counted
/// backwards at <see cref="RestShare"/> of the inn's rate — more than a campfire's one, because it has eaten (the meal itself doubles
/// what it regenerates, <c>BotMeal</c>) — and it regains <see cref="RegenPoints"/> of hits, stamina and mana every
/// <see cref="RegenEveryMs"/> on top. Round the table, with two or more seated, the campfire's talk is said (<c>BotCampTalk</c>).
/// </para>
///
/// <para>
/// <b>And the tired eat before they sleep.</b> A tired bot sent to an inn is sent to an inn with an open tavern when one is within
/// <see cref="Detour"/> tiles of the nearest inn (<see cref="InnFor"/>); arriving, it buys supper at the table before it pays for the
/// bed (<see cref="Supper"/>), and a night after supper buys <see cref="SupperBuff"/> times the rested regeneration on its return
/// (<see cref="SupperFactor"/>, read off the rest record's inn name, which is saved, so a restart neither loses it nor grants it).
/// </para>
/// </summary>
public static class BotTaverns
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTaverns));

    /// <summary>What the rest record carries after an inn's name when the bot ate at a tavern before it slept.</summary>
    public const string SupperMark = " after supper";

    // ---- Dials ------------------------------------------------------------------------------------------------------------

    /// <summary>The most taverns open on the island at once. Six, one to a town or two.</summary>
    public static int MostOpen { get; set; } = 6;

    /// <summary>How long a cook keeps its tavern, in minutes. Twenty: an evening's sitting and the walk of guests from the town.</summary>
    public static double ShiftMinutes { get; set; } = 20.0;

    /// <summary>How long after a shift the same cook may keep another. Twenty minutes: it has meat to cook.</summary>
    public static int RestMs { get; set; } = 1200000;

    /// <summary>The fewest meals a cook must hold, pack and bank box together, to be offered a tavern. Four.</summary>
    public static int LeastMeals { get; set; } = 4;

    /// <summary>How many meals a cook takes out of its bank box for a shift. Fifteen.</summary>
    public static int StockMeals { get; set; } = 15;

    /// <summary>How far a cook may be from an inn and still be offered a tavern there. A hundred and fifty tiles.</summary>
    public static int KeeperReach { get; set; } = 150;

    /// <summary>How close to an open tavern another may open. Forty tiles: nearer, a town has two taverns and half the guests each.</summary>
    public static int Spacing { get; set; } = 40;

    /// <summary>How far a guest may be from an open tavern and be offered a seat at it. A hundred tiles: across a town and out of its gate.</summary>
    public static int GuestReach { get; set; } = 100;

    /// <summary>The most guests at one table. Eight.</summary>
    public static int MostGuests { get; set; } = 8;

    /// <summary>How long a guest sits after supper, in minutes, a minute either way. Five.</summary>
    public static double GuestMinutes { get; set; } = 5.0;

    /// <summary>How long a guest may take to reach the table before it gives up. Four minutes.</summary>
    public static int WalkMs { get; set; } = 240000;

    /// <summary>
    /// The meal's price as a share of what the island has learned a meal of that kind sells for (<c>BotMarket.Worth</c>, the cook's
    /// own guess when nothing has sold). One: the market's price, served at the table.
    /// </summary>
    public static double Markup { get; set; } = 1.0;

    /// <summary>What a guest keeps in its purse after supper: a poor bot hunts rather than buys suppers. A hundred.</summary>
    public static int GuestKeeps { get; set; } = 100;

    /// <summary>
    /// What a minute at the table gives back of the evening, as a share of the inn's rate. One and a half: a campfire's is one, and
    /// the guest has eaten. See <see cref="Played"/>.
    /// </summary>
    public static double RestShare { get; set; } = 1.5;

    /// <summary>Points of hits, stamina and mana a seated guest regains every <see cref="RegenEveryMs"/>, over the meal's own doubling.</summary>
    public static int RegenPoints { get; set; } = 2;

    public static int RegenEveryMs { get; set; } = 4000;

    /// <summary>How many tiles further than the nearest inn a tired bot will walk to an inn with an open tavern. Sixty.</summary>
    public static int Detour { get; set; } = 60;

    /// <summary>How long a lodger walks to the cook for supper before it goes to bed without. A minute.</summary>
    public static int SupperWalkMs { get; set; } = 60000;

    /// <summary>A night after supper buys this many times the rested regeneration. One and a half.</summary>
    public static double SupperBuff { get; set; } = 1.5;

    /// <summary>How long after supper a bot leaving the world counts as having eaten before it slept. Thirty minutes.</summary>
    public static int SupperWindowMs { get; set; } = 1800000;

    /// <summary>How often the cook calls out at its table, in the local channel. A minute and a half.</summary>
    public static int CryMs { get; set; } = 90000;

    /// <summary>A line round the table every this many milliseconds, with a spread.</summary>
    public static int LineMs { get; set; } = 9000;

    // ---- Counters ---------------------------------------------------------------------------------------------------------

    public static long Opened { get; private set; }

    public static long Closed { get; private set; }

    public static long SoldOut { get; private set; }

    public static long TimeUp { get; private set; }

    public static long ClosedAway { get; private set; }

    public static long Meals { get; private set; }

    public static long Gold { get; private set; }

    /// <summary>Guests who sat at a table after supper.</summary>
    public static long Rests { get; private set; }

    /// <summary>Minutes of play given back at a table, and the minutes sat there.</summary>
    public static double GivenBack { get; private set; }

    public static double SatMinutes { get; private set; }

    public static long RegenGiven { get; private set; }

    /// <summary>Tired bots sent to an inn with an open tavern rather than the nearest inn.</summary>
    public static long Steered { get; private set; }

    /// <summary>Lodgers who ate at the table before bed, and who could not (no money, already fed, the walk too long).</summary>
    public static long Lodgers { get; private set; }

    public static long LodgersNot { get; private set; }

    /// <summary>Returns from a night after supper, given the larger buff.</summary>
    public static long SupperBuffs { get; private set; }

    public static long Lines { get; private set; }

    public static long Shifts { get; private set; }

    public static long Shown { get; private set; }

    public static long Taken { get; private set; }

    private static readonly List<BotTavern> _open = [];

    private static readonly Dictionary<Serial, BotTavern> _seated = [];

    private static readonly Dictionary<Serial, long> _supped = [];

    private static readonly Dictionary<Serial, long> _regenTick = [];

    private static readonly List<BotMobile> _talkers = [];

    private static readonly Type[] _kinds = [typeof(Ribs), typeof(LambLeg), typeof(CookedBird), typeof(ChickenLeg), typeof(FishSteak)];

    public static IReadOnlyList<BotTavern> Open => _open;

    /// <summary>The share of meals shown that a shift sells, measured; a half until five shifts have closed.</summary>
    public static double Share => Shifts < 5 || Shown <= 0 ? 0.5 : Math.Clamp((double)Taken / Shown, 0.05, 1.0);

    // ---- Meals ------------------------------------------------------------------------------------------------------------

    /// <summary>Meals in a container (the pack, or a bank box), by the cooking's own list (<c>BotOven.IsMeal</c>).</summary>
    public static int MealsIn(Container box)
    {
        if (box == null)
        {
            return 0;
        }

        var n = 0;

        for (var i = 0; i < _kinds.Length; i++)
        {
            n += box.GetAmount(_kinds[i]);
        }

        return n;
    }

    /// <summary>The kind of meal this cook holds most of in its pack, or null.</summary>
    public static Type MostHeld(Mobile cook, out int units)
    {
        units = 0;
        Type best = null;
        var pack = cook?.Backpack;

        if (pack == null)
        {
            return null;
        }

        for (var i = 0; i < _kinds.Length; i++)
        {
            var n = pack.GetAmount(_kinds[i]);

            if (n > units)
            {
                units = n;
                best = _kinds[i];
            }
        }

        return best;
    }

    /// <summary>The meal kinds, for taking them out of a bank box.</summary>
    public static IReadOnlyList<Type> Kinds => _kinds;

    /// <summary>What a meal of this kind costs at the table.</summary>
    public static int Price(Type kind) =>
        kind == null ? 0 : Math.Max(1, (int)Math.Round(BotMarket.Worth(kind, BotOven.Worth) * Math.Max(0.1, Markup)));

    /// <summary>What a tavern's shift is worth a minute to a cook holding this many meals of this price: the share a shift sells.</summary>
    public static double Claim(int meals, int price) =>
        Math.Clamp(Math.Min(meals, StockMeals) * Math.Max(1, price) * Share / Math.Max(1.0, ShiftMinutes), 4.0, 120.0);

    // ---- Opening and shutting ----------------------------------------------------------------------------------------------

    /// <summary>The tavern open at this inn, or null.</summary>
    public static BotTavern At(BotInns.Inn inn)
    {
        if (inn == null)
        {
            return null;
        }

        for (var i = 0; i < _open.Count; i++)
        {
            if (ReferenceEquals(_open[i].Inn, inn) && !_open[i].Shut)
            {
                return _open[i];
            }
        }

        return null;
    }

    /// <summary>Whether a tavern is open within <see cref="Spacing"/> of this place.</summary>
    public static bool Near(Point3D at)
    {
        for (var i = 0; i < _open.Count; i++)
        {
            if (!_open[i].Shut && Utility.InRange(_open[i].Stand, at, Spacing))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The nearest inn to this cook within <see cref="KeeperReach"/> with no tavern open at or near it, or null.</summary>
    public static BotInns.Inn FreeInn(Mobile cook)
    {
        BotInns.Inn best = null;
        var nearest = int.MaxValue;
        var inns = BotInns.All;

        for (var i = 0; i < inns.Count; i++)
        {
            var inn = inns[i];
            var away = Math.Max(Math.Abs(inn.Spot.X - cook.X), Math.Abs(inn.Spot.Y - cook.Y));

            if (away > KeeperReach || away >= nearest || Near(inn.Spot))
            {
                continue;
            }

            best = inn;
            nearest = away;
        }

        return best;
    }

    internal static BotTavern OpenAt(BotMobile keeper, BotInns.Inn inn, Map map)
    {
        if (keeper == null || inn == null || At(inn) != null || Near(inn.Spot) || _open.Count >= MostOpen)
        {
            return null;
        }

        var tavern = new BotTavern(keeper, inn, map, keeper.Location);

        Count(tavern, Core.TickCount);
        _open.Add(tavern);
        Opened++;

        logger.Information(
            "{Name} opened a tavern at {Inn} ({X}, {Y}) with {Meals} meals, {Kind} at {Price}gp: a guest within {Reach} tiles is asked to supper, a tired bot sleeps there after it",
            keeper.Name,
            inn.Name,
            keeper.X,
            keeper.Y,
            tavern.Meals,
            MostHeld(keeper, out _)?.Name ?? "nothing",
            Price(MostHeld(keeper, out _)),
            GuestReach
        );

        BotEvents.Post("tavern", keeper, $"opened a tavern at {inn.Name} with {tavern.Meals} meals");
        Say(keeper, "tavern:open", null, tavern, "trade");

        return tavern;
    }

    internal static void Close(BotTavern tavern, string why, bool soldOut, bool timeUp, int shown)
    {
        if (tavern == null || tavern.Shut)
        {
            return;
        }

        tavern.Shut = true;
        _open.Remove(tavern);

        foreach (var (serial, _) in tavern.Table)
        {
            _seated.Remove(serial);
        }

        tavern.Table.Clear();
        BotDice.Drop(tavern);
        Closed++;
        // Sell-through is measured over shifts that ran their course or sold out; a kitchen shut early says nothing about it.
        if (shown > 0)
        {
            Shifts++;
            Shown += shown;
            Taken += tavern.Sold;
        }

        if (soldOut)
        {
            SoldOut++;
        }
        else if (timeUp)
        {
            TimeUp++;
        }
        else
        {
            ClosedAway++;
        }

        var minutes = (Core.TickCount - tavern.OpenedTick) / 60000.0;

        logger.Information(
            "{Name} shut its tavern at {Inn} after {Minutes:F1} minutes — {Why}: {Sold} meals sold to {Served} guests for {Gold}gp, {Lines} lines said at the table",
            tavern.Keeper?.Name,
            tavern.Inn?.Name,
            minutes,
            why,
            tavern.Sold,
            tavern.Served,
            tavern.Takings,
            tavern.Lines
        );

        if (tavern.Keeper is { Deleted: false, Alive: true } keeper)
        {
            Say(keeper, "tavern:close", null, tavern);
        }
    }

    /// <summary>Counts the meals in the cook's pack. From the shift, every few seconds and after every sale.</summary>
    internal static void Count(BotTavern tavern, long now)
    {
        tavern.Meals = MealsIn(tavern.Keeper?.Backpack);
        tavern.CountedTick = now;
    }

    /// <summary>The open tavern nearest this guest within reach, with a seat, welcome and on joined ground; null with why not.</summary>
    public static BotTavern Nearest(BotMobile guest, long now, out string refusal)
    {
        refusal = "far";
        BotTavern best = null;
        var nearest = int.MaxValue;

        for (var i = 0; i < _open.Count; i++)
        {
            var tavern = _open[i];

            if (tavern.Shut || tavern.Map != guest.Map || ReferenceEquals(tavern.Keeper, guest))
            {
                continue;
            }

            var away = Math.Max(Math.Abs(tavern.Stand.X - guest.X), Math.Abs(tavern.Stand.Y - guest.Y));

            if (away > GuestReach || away >= nearest)
            {
                continue;
            }

            if (tavern.SeatedCount(now) >= MostGuests)
            {
                refusal = "full";

                continue;
            }

            if (!BotCamp.Welcome(guest, tavern.Keeper))
            {
                refusal = "unwelcome";

                continue;
            }

            if (!BotGates.Joined(guest.Map, guest.Location, tavern.Stand))
            {
                continue;
            }

            best = tavern;
            nearest = away;
        }

        return best;
    }

    // ---- The table ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// One meal from the cook's pack to the guest's, coin the other way, eaten at once. Returns the coin paid, nought when nothing
    /// changed hands; <paramref name="refused"/> says why.
    /// </summary>
    internal static int Serve(BotTavern tavern, BotMobile guest, out Type meal, out string refused)
    {
        meal = null;
        refused = null;

        if (tavern?.Keeper is not { Deleted: false, Alive: true } keeper || tavern.Shut)
        {
            refused = "the tavern was shut";

            return 0;
        }

        meal = MostHeld(keeper, out var units);

        if (meal == null || units <= 0)
        {
            refused = "the cook had no meal left";

            return 0;
        }

        var price = Price(meal);

        if (BotYield.Spendable(guest) < price)
        {
            refused = $"it could not pay {price}gp";

            return 0;
        }

        var given = BotHandover.Exchange(keeper, guest, meal, 1, price, false, $"supper at {tavern.Name}", out var paid, out refused);

        if (given <= 0)
        {
            return 0;
        }

        BotMeal.Keep(guest);

        // The town's cooks' charter takes its levy out of the meal's price, into the holder's chest (07.10.2026, charters20).
        BotCharters.After(keeper, tavern.Stand, meal, given, paid);

        // Supper eaten at a tavern: a bed taken within the window after it is a night after supper. See Supped.
        _supped[guest.Serial] = Core.TickCount;

        tavern.Sold += given;
        tavern.Takings += paid;
        tavern.Served++;
        Meals += given;
        Gold += paid;

        // A guest by the hour of the day, for the Day: line (PLAN-life A1).
        BotDay.Guest();

        Count(tavern, Core.TickCount);

        return paid;
    }

    /// <summary>A guest sat down at the table.</summary>
    internal static void Sit(BotTavern tavern, BotMobile guest, long now)
    {
        if (tavern == null || guest == null)
        {
            return;
        }

        tavern.Table[guest.Serial] = (guest, now, false);
        _seated[guest.Serial] = tavern;
        Rests++;
    }

    /// <summary>A guest is still at the table, this beat: kept seen, and regenerating over its meal.</summary>
    internal static void Seated(BotTavern tavern, BotMobile guest, long now)
    {
        if (tavern == null || guest == null || !tavern.Table.TryGetValue(guest.Serial, out var seat))
        {
            return;
        }

        tavern.Table[guest.Serial] = (guest, now, seat.Greeted);

        if (RegenPoints <= 0 || _regenTick.TryGetValue(guest.Serial, out var last) && now - last < RegenEveryMs)
        {
            return;
        }

        _regenTick[guest.Serial] = now;

        if (guest.Hits < guest.HitsMax)
        {
            guest.Hits = Math.Min(guest.HitsMax, guest.Hits + RegenPoints);
            RegenGiven += RegenPoints;
        }

        if (guest.Stam < guest.StamMax)
        {
            guest.Stam = Math.Min(guest.StamMax, guest.Stam + RegenPoints);
            RegenGiven += RegenPoints;
        }

        if (guest.Mana < guest.ManaMax)
        {
            guest.Mana = Math.Min(guest.ManaMax, guest.Mana + RegenPoints);
            RegenGiven += RegenPoints;
        }
    }

    /// <summary>A guest rose from the table, however its sitting ended.</summary>
    internal static void Rise(BotTavern tavern, BotMobile guest, double minutes)
    {
        if (guest == null)
        {
            return;
        }

        if (tavern != null && tavern.Table.Remove(guest.Serial))
        {
            SatMinutes += Math.Max(0.0, minutes);
        }

        _seated.Remove(guest.Serial);
        _regenTick.Remove(guest.Serial);
    }

    /// <summary>
    /// The rest's clock asks how much of this minute was play: for a guest seated at a table after supper, the minute counts
    /// backwards at <see cref="RestShare"/> of the inn's rate; null for everybody else, who are the campfire's to answer. One hook in
    /// <c>BotRest.Look</c>.
    /// </summary>
    public static double? Played(BotMobile bot, double minutes, double hours)
    {
        if (!TavernsOnAt(bot, out _) || bot.Tired || minutes <= 0.0)
        {
            return null;
        }

        var rest = Math.Max(0.1, (BotRest.LeastRestHours + BotRest.MostRestHours) / 2.0);
        var back = minutes * Math.Max(0.0, hours) / rest * Math.Max(0.0, RestShare);

        GivenBack += back;

        return -back;
    }

    private static bool TavernsOnAt(BotMobile bot, out BotTavern tavern)
    {
        tavern = null;

        if (bot == null || !_seated.TryGetValue(bot.Serial, out tavern) || tavern.Shut
            || !tavern.Table.TryGetValue(bot.Serial, out var seat))
        {
            return false;
        }

        return Core.TickCount - seat.Seen <= 5000;
    }

    // ---- The tired, the inn and the bed ------------------------------------------------------------------------------------

    /// <summary>
    /// The inn a tired bot should sleep at: one with an open tavern within <see cref="Detour"/> tiles further than the nearest inn
    /// within <paramref name="reach"/>, or null for the inns' own choice. One hook in <c>BotInns.ToInn</c>.
    /// </summary>
    public static BotInns.Inn InnFor(BotMobile bot, BotInns.Inn nearest, int reach)
    {
        if (!BotTrades.TavernsOn || bot == null || _open.Count == 0)
        {
            return null;
        }

        var limit = nearest == null ? reach : Math.Min(reach, Math.Max(Math.Abs(nearest.Spot.X - bot.X), Math.Abs(nearest.Spot.Y - bot.Y)) + Detour);
        BotTavern best = null;
        var closest = int.MaxValue;

        for (var i = 0; i < _open.Count; i++)
        {
            var tavern = _open[i];

            if (tavern.Shut || tavern.Map != bot.Map || ReferenceEquals(tavern.Keeper, bot) || !BotCamp.Welcome(bot, tavern.Keeper))
            {
                continue;
            }

            var away = Math.Max(Math.Abs(tavern.Stand.X - bot.X), Math.Abs(tavern.Stand.Y - bot.Y));

            if (away <= limit && away < closest)
            {
                best = tavern;
                closest = away;
            }
        }

        if (best == null || ReferenceEquals(best.Inn, nearest))
        {
            return null;
        }

        Steered++;

        return best.Inn;
    }

    /// <summary>The cook keeping a tavern at this inn, for the lodger's walk; null when none is open.</summary>
    public static BotMobile KeeperAt(BotInns.Inn inn) =>
        BotTrades.TavernsOn && At(inn) is { Keeper: { Deleted: false, Alive: true } keeper } ? keeper : null;

    /// <summary>
    /// A lodger at the cook's side buys supper before bed: one meal, eaten at once. From <c>BotLodge.Advance</c>. Returns whether
    /// it ate; a lodger already fed, short of the price or too poor to keep its purse after it, goes to bed without.
    /// </summary>
    public static bool Supper(BotMobile bot, BotInns.Inn inn)
    {
        var tavern = At(inn);

        if (tavern == null || bot == null)
        {
            return false;
        }

        if (BotMeal.IsFed(bot))
        {
            LodgersNot++;

            return false;
        }

        var paid = Serve(tavern, bot, out var meal, out var refused);

        if (paid <= 0)
        {
            LodgersNot++;

            logger.Information("{Name} went to bed at {Inn} without supper at {Tavern}: {Why}", bot.Name, inn?.Name, tavern.Name, refused);

            return false;
        }

        Lodgers++;

        logger.Information(
            "{Name} ate {Meal} at {Tavern} for {Paid}gp before bed, and will sleep the better for it",
            bot.Name,
            meal?.Name,
            tavern.Name,
            paid
        );

        Say(bot, "tavern:lodger", tavern.Keeper, tavern);

        return true;
    }

    /// <summary>A lodger who could not get to the cook in time and went to bed without.</summary>
    public static void NoSupper() => LodgersNot++;

    /// <summary>
    /// The name the rest record keeps for where a bot slept: the inn's, and <see cref="SupperMark"/> after it when the bot ate at a
    /// tavern within <see cref="SupperWindowMs"/>. One hook in <c>BotRest.Leave</c>.
    /// </summary>
    public static string Supped(BotMobile bot, string inn)
    {
        if (bot == null || inn == null || !_supped.Remove(bot.Serial, out var when) || Core.TickCount - when > SupperWindowMs)
        {
            return inn;
        }

        return inn + SupperMark;
    }

    /// <summary>The factor on a paid night's regeneration: <see cref="SupperBuff"/> after supper, one otherwise. One hook in <c>BotRest.Return</c>.</summary>
    public static double SupperFactor(string inn)
    {
        if (inn == null || !inn.EndsWith(SupperMark, StringComparison.Ordinal))
        {
            return 1.0;
        }

        SupperBuffs++;

        return Math.Max(1.0, SupperBuff);
    }

    // ---- Talk ----------------------------------------------------------------------------------------------------------------

    /// <summary>A line round each table with two or more seated, when one is due. From the module's clock.</summary>
    internal static void Talk(long now)
    {
        for (var i = 0; i < _open.Count; i++)
        {
            var tavern = _open[i];

            if (tavern.Shut || now - tavern.NextLineTick < 0)
            {
                continue;
            }

            tavern.NextLineTick = now + Math.Max(2000, LineMs) + Utility.Random(4000);

            _talkers.Clear();

            foreach (var (_, seat) in tavern.Table)
            {
                if (now - seat.Seen <= 5000 && seat.Bot is { Deleted: false, Alive: true })
                {
                    _talkers.Add(seat.Bot);
                }
            }

            if (_talkers.Count == 0)
            {
                continue;
            }

            // A game of dice at the table takes the beat when one is running or is called (B5).
            if (BotDice.Play(tavern, _talkers))
            {
                continue;
            }

            // A guest who did not speak last says something; another guest, else the cook, hears it.
            var first = Utility.Random(_talkers.Count);
            BotMobile speaker = null;

            for (var k = 0; k < _talkers.Count && speaker == null; k++)
            {
                var pick = _talkers[(first + k) % _talkers.Count];

                if (!ReferenceEquals(pick, tavern.LastSpeaker) || _talkers.Count == 1)
                {
                    speaker = pick;
                }
            }

            BotMobile listener = null;

            for (var k = 0; k < _talkers.Count && listener == null; k++)
            {
                if (!ReferenceEquals(_talkers[k], speaker))
                {
                    listener = _talkers[k];
                }
            }

            listener ??= tavern.Keeper;

            var fill = new Dictionary<string, string>();
            var topic = BotCampTalk.Topic(speaker, tavern.Map, tavern.Stand, fill, "tavern:small", out var told, _talkers);

            if (BotCampTalk.Say(speaker, topic, "tavern:small", listener, fill))
            {
                tavern.Lines++;
                tavern.LastSpeaker = speaker;
                Lines++;

                // A tale told at the table is remembered by everybody seated for it (PLAN-life A6).
                if (told != null)
                {
                    var place = BotBiography.Place(tavern.Map, tavern.Stand);

                    for (var k = 0; k < _talkers.Count; k++)
                    {
                        BotBiography.Heard(_talkers[k], speaker, told, place);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Says a tavern line, the inn, the meal and its price filled in: aloud at the table, or in the trade room for the opening, which
    /// is how a buyer across the town learns where supper is (the shops' own rule, <c>BotShopkeep.Cry</c>).
    /// </summary>
    internal static bool Say(Mobile who, string occasion, Mobile to, BotTavern tavern, string channel = "local")
    {
        if (who == null)
        {
            return false;
        }

        var meal = tavern?.Keeper == null ? null : MostHeld(tavern.Keeper, out _);
        var fill = new Dictionary<string, string>
        {
            ["inn"] = tavern?.Inn?.Name ?? "the inn",
            ["cook"] = tavern?.Keeper?.Name ?? "the cook",
            ["meal"] = meal == null ? "supper" : BotShopkeep.Words(meal),
            ["price"] = meal == null ? "a few" : Price(meal).ToString()
        };

        if (channel == "local")
        {
            return BotCampTalk.Say(who, occasion, null, to, fill);
        }

        fill["other"] = to?.Name ?? "friend";

        var line = BotVoice.Phrase(occasion, null, who, fill);

        return line != null && BotVoice.Say(who, channel, line, topic: "tavern");
    }

    /// <summary>The words, put into the voice's bank where it has none of its own.</summary>
    public static Dictionary<string, string[]> Phrases() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["took:tavern"] = ["Supper to serve tonight.", "Off to keep the tavern.", "The inn's kitchen is mine for the evening."],
            ["finished:tavern"] = ["That's the kitchen shut.", "A good night at the tavern.", "Washing up. Done for tonight."],
            ["took:supper"] = ["Supper at the inn, I think.", "I could eat. The tavern's open.", "A hot meal and a sit down."],
            ["finished:supper"] = ["Good supper, that. Back to it.", "Fed and rested. Thank you, cook.", "Right. On my way."],
            ["tavern:open"] = ["Supper's on at {inn}! {meal}, {price} gold.", "Kitchen's open at {inn}. Come and eat.", "Hot {meal} at {inn}, {price} gold a plate."],
            ["tavern:cry"] = ["Hot {meal}, {price} gold!", "Sit down, eat something.", "There's room at the table."],
            ["tavern:close"] = ["That's the kitchen closed. Safe roads.", "No more tonight, friends.", "Kitchen's shut. Thank you all."],
            ["tavern:greet"] = ["Evening, {other}. Something hot, please.", "A plate of whatever's good, {other}.", "I've walked a long way for this, {other}."],
            ["tavern:lodger"] = ["A bite to eat, then bed.", "Supper first, then sleep.", "Something warm before I turn in, {other}."],
            ["tavern:small"] = ["Good food, this.", "My feet thank me.", "Warm in here.", "Another day done.", "Pass the salt.", "Best meal I've had all week."],
            ["tavern:dice:call"] = ["Dice, {other}? {stake} gold a throw.", "Fancy a throw, {other}? {stake} gold.", "The bones, {other}. {stake} gold says I out-roll you."],
            ["tavern:dice:take"] = ["Done. {stake} gold it is.", "Roll them.", "You're on, {other}."],
            ["tavern:dice:poor"] = ["Not this purse, not tonight.", "Keep your dice; my pouch is light."],
            ["tavern:dice:roll"] = ["{rollA} against {rollB} — mine!", "{rollA} to your {rollB}. The table holds its breath.", "{rollA} and {rollB}. Luck's a fickle cook."],
            ["tavern:dice:tie"] = ["Even. Again — once.", "A push. One more throw settles it."],
            ["tavern:dice:paid"] = ["{stake} gold, as agreed. Well thrown.", "There — {stake}. The bones hate me tonight."],
            ["tavern:dice:void"] = ["Then we call it no game. Another night.", "No coin, no wager. Forgotten."],
            ["tavern:dice:watch"] = ["Ha! The bones choose.", "Well rolled.", "That's supper money changing hands."]
        };

    public static string Describe()
    {
        var now = Core.TickCount;
        var seated = 0;

        for (var i = 0; i < _open.Count; i++)
        {
            seated += _open[i].SeatedCount(now);
        }

        return $"taverns {_open.Count} open now ({seated} at table): {Opened} opened, {Closed} shut ({SoldOut} sold out, {TimeUp} ran their {ShiftMinutes:F0} minutes, {ClosedAway} shut when the cook was taken away); "
               + $"{Meals} meals sold for {Gold}gp, sell-through {Share:P0} over {Shifts} shifts; {Rests} guests sat at a table after supper ({SatMinutes:F0} minutes, {GivenBack:F0} minutes of play given back, {RegenGiven} points regained); "
               + $"{Steered} tired bots sent to an inn with a tavern, {Lodgers} ate before bed and {LodgersNot} went without, {SupperBuffs} woke to the supper's longer buff; {Lines} lines said at table; "
               + $"{BotDice.Describe()}; {BotTaverner.Describe()}; {BotSupperer.Describe()}";
    }

    public static void Forget()
    {
        for (var i = _open.Count - 1; i >= 0; i--)
        {
            _open[i].Shut = true;
        }

        _open.Clear();
        _seated.Clear();
        _supped.Clear();
        _regenTick.Clear();
        Opened = 0;
        Closed = 0;
        SoldOut = 0;
        TimeUp = 0;
        ClosedAway = 0;
        Meals = 0;
        Gold = 0;
        Rests = 0;
        GivenBack = 0;
        SatMinutes = 0;
        RegenGiven = 0;
        Steered = 0;
        Lodgers = 0;
        LodgersNot = 0;
        SupperBuffs = 0;
        Lines = 0;
        Shifts = 0;
        Shown = 0;
        Taken = 0;
        BotDice.Forget();
    }
}
