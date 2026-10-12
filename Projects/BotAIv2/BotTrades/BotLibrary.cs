using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>One library while it is open: the scribe keeping it, its guild, where it stands, its spare scrolls and what it has sold.</summary>
public sealed class BotLibrary
{
    public BotLibrary(BotMobile keeper, Guild guild, Map map, Point3D stand)
    {
        Keeper = keeper;
        Guild = guild;
        Map = map;
        Stand = stand;
        OpenedTick = Core.TickCount;
    }

    public BotMobile Keeper { get; }

    public Guild Guild { get; }

    public Map Map { get; }

    public Point3D Stand { get; }

    public long OpenedTick { get; }

    public bool Shut { get; internal set; }

    /// <summary>The keeper's spare scrolls by kind at the last count: what is over its own keep list, in its pack.</summary>
    internal readonly Dictionary<Type, int> Stock = [];

    public long CountedTick { get; internal set; }

    public int Units { get; internal set; }

    public int Sold { get; internal set; }

    public int Takings { get; internal set; }

    public int Served { get; internal set; }

    public string Name => $"{Keeper?.Name}'s library at {Guild?.Name}'s hall";
}

/// <summary>
/// The libraries: a guild's scribe keeping a shift by its guild's seat, selling the scrolls it wrote to the casters of its guild and
/// its allies whose books are short of them, at the market's price (07.10.2026, life18b).
///
/// <para>
/// <b>Patrick, 07.10.2026: "a scribe-librarian (sells scrolls by the guild stone)"; that morning: "we do it".</b> What exists: scribes
/// wrote 5,880 practice scrolls in the nine hours of build 416 and keep the spare ones in their boxes; casters short of a spell took
/// 1,397 errands for one, 686 of them a want said aloud and waited on (<c>BotAcquire</c>, <c>BotSeeker</c>). The library puts the two
/// together where the guild lives: the scribe brings its spare scrolls of the kinds its guild's and allies' casters lack, and a caster
/// short of one within <see cref="ReaderReach"/> buys it there, hand to hand (<see cref="BotHandover.Exchange"/>), and writes it into its
/// book at once; a want it had standing for it is withdrawn and its money freed. Nothing is minted: the scroll was the scribe's, the
/// coin was the caster's.
/// </para>
/// </summary>
public static class BotLibraries
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotLibraries));

    // ---- Dials ------------------------------------------------------------------------------------------------------------

    public static bool On => BotTrades.LibrariesOn;

    /// <summary>How long a scribe keeps its library, in minutes. Twenty.</summary>
    public static double ShiftMinutes { get; set; } = 20.0;

    /// <summary>How long after a shift the same scribe may keep another. Twenty minutes.</summary>
    public static int RestMs { get; set; } = 1200000;

    /// <summary>How far a scribe may be from its guild's seat and be offered the shift. A hundred and fifty tiles.</summary>
    public static int KeeperReach { get; set; } = 150;

    /// <summary>The Inscription a bot needs to keep a library: a scribe by trade. Fifty.</summary>
    public static double LeastInscribe { get; set; } = 50.0;

    /// <summary>The fewest spare scrolls of kinds its guild's and allies' casters lack a scribe must hold to open. Three.</summary>
    public static int LeastScrolls { get; set; } = 3;

    /// <summary>The most scrolls taken out of the box for a shift. Thirty.</summary>
    public static int StockScrolls { get; set; } = 30;

    /// <summary>How far a caster may be from a library and be sent to it. A hundred and fifty tiles.</summary>
    public static int ReaderReach { get; set; } = 150;

    /// <summary>The price over the market's (<see cref="BotMarket.Worth"/>). One: the market's price.</summary>
    public static double Markup { get; set; } = 1.0;

    /// <summary>The share of what is in demand reckoned to sell in a shift until the ledger knows better. A half.</summary>
    public static double SellThrough { get; set; } = 0.5;

    /// <summary>How often the casters' wants are counted again, guild by guild. A minute: a walk of the population list.</summary>
    public static int DemandMs { get; set; } = 60000;

    /// <summary>How often an open library counts its scrolls again.</summary>
    public static int CountMs { get; set; } = 15000;

    /// <summary>How often a scribe's pack and box are read for a library. A minute.</summary>
    public static int LookMs { get; set; } = 60000;

    // ---- Counters ---------------------------------------------------------------------------------------------------------

    public static long Opened { get; private set; }

    public static long Closed { get; private set; }

    public static long SoldOut { get; private set; }

    public static long Sold { get; private set; }

    public static long Gold { get; private set; }

    public static long Learned { get; private set; }

    public static long Unwritten { get; private set; }

    public static long WantsAnswered { get; private set; }

    public static long Fetched { get; private set; }

    private static readonly List<BotLibrary> _open = [];

    private static readonly Dictionary<Guild, Dictionary<Type, int>> _demand = [];

    private static long _demandTick;

    public static IReadOnlyList<BotLibrary> Open => _open;

    // ---- Places and prices ------------------------------------------------------------------------------------------------

    public static BotLibrary Of(Guild guild)
    {
        if (guild == null)
        {
            return null;
        }

        for (var i = 0; i < _open.Count; i++)
        {
            if (!_open[i].Shut && ReferenceEquals(_open[i].Guild, guild))
            {
                return _open[i];
            }
        }

        return null;
    }

    /// <summary>Whether a bot of <paramref name="guild"/> buys at this guild's library: its own, or an ally's.</summary>
    public static bool Serves(Guild library, Guild guild) =>
        library != null && guild != null && (ReferenceEquals(library, guild) || library.IsAlly(guild));

    /// <summary>What one scroll of this kind sells for: the market's price, the engine's shelf price for its circle until it has one.</summary>
    public static int Price(Type kind)
    {
        var spell = BotGrimoire.SpellOf(kind);
        var shelf = BotGrimoire.ShopPrice(spell >= 0 ? BotGrimoire.Circle(spell) : 1);

        return Math.Max(1, (int)Math.Round(BotMarket.Worth(kind, shelf) * Math.Max(0.1, Markup)));
    }

    /// <summary>
    /// A stand by the seat: a floor two to four tiles off it, so the scribe and the guild's healer do not stand on one tile; the seat
    /// itself when none is found.
    /// </summary>
    internal static Point3D StandBy(Map map, Point3D seat)
    {
        for (var r = 2; r <= 4; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                    {
                        continue;
                    }

                    if (BotStep.Settle(map, seat.X + dx, seat.Y + dy, out var z) && Math.Abs(z - seat.Z) <= 10)
                    {
                        return new Point3D(seat.X + dx, seat.Y + dy, z);
                    }
                }
            }
        }

        return seat;
    }

    // ---- What the casters lack --------------------------------------------------------------------------------------------

    /// <summary>
    /// The scroll each caster's book needs next, counted by guild: the gap <c>BotSeeker</c> works on, so a library stocks what the
    /// casters are already trying to buy. One walk of the population list every <see cref="DemandMs"/>.
    /// </summary>
    private static void Count(long now)
    {
        if (now - _demandTick < DemandMs && _demand.Count > 0)
        {
            return;
        }

        _demandTick = now;

        foreach (var (_, kinds) in _demand)
        {
            kinds.Clear();
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false } || bot.Guild is not Guild guild || !Reading(bot, out var kind))
            {
                continue;
            }

            if (!_demand.TryGetValue(guild, out var kinds))
            {
                kinds = [];
                _demand[guild] = kinds;
            }

            kinds[kind] = kinds.GetValueOrDefault(kind) + 1;
        }
    }

    /// <summary>The scroll this bot's book needs next and it does not hold, or false. The resurrection scroll kept to read is not one.</summary>
    internal static bool Reading(Mobile bot, out Type kind)
    {
        kind = null;

        if (BotGrimoire.Book(bot) == null)
        {
            return false;
        }

        var spell = BotGrimoire.Missing(bot);

        if (spell < 0 || spell == BotRaise.Spell && BotRaise.KeepsScroll(bot))
        {
            return false;
        }

        kind = BotGrimoire.ScrollFor(spell);

        return kind != null && BotQuill.Held(bot, kind) <= 0;
    }

    /// <summary>How many casters of this guild and its allies lack this kind, at the last count.</summary>
    public static int Demand(Guild guild, Type kind)
    {
        Count(Core.TickCount);

        var n = 0;

        foreach (var (other, kinds) in _demand)
        {
            if (Serves(guild, other) && kinds.TryGetValue(kind, out var lacking))
            {
                n += lacking;
            }
        }

        return n;
    }

    /// <summary>
    /// The spare scrolls in a container whose kinds a caster of this guild or an ally lacks, by kind, with the keeper's own keep taken off
    /// (<c>BotUnload.Keeps</c>: the book's first copy, a caster's attack scrolls). Returns the units, each capped at what is lacked.
    /// </summary>
    internal static int Spare(Container from, Guild guild, Dictionary<Type, int> keep, Dictionary<Type, int> into)
    {
        into.Clear();

        if (from == null)
        {
            return 0;
        }

        var items = from.Items;

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is SpellScroll { Deleted: false, Movable: true } scroll && scroll.GetType() != typeof(SpellScroll))
            {
                var kind = scroll.GetType();

                into[kind] = into.GetValueOrDefault(kind) + Math.Max(1, scroll.Amount);
            }
        }

        var units = 0;

        _kinds.Clear();
        _kinds.AddRange(into.Keys);

        for (var i = 0; i < _kinds.Count; i++)
        {
            var kind = _kinds[i];
            var spare = into[kind] - (keep != null ? keep.GetValueOrDefault(kind) : 0);
            var lacked = guild != null ? Demand(guild, kind) : spare;

            spare = Math.Min(spare, lacked);

            if (spare <= 0)
            {
                into.Remove(kind);

                continue;
            }

            into[kind] = spare;
            units += spare;
        }

        return units;
    }

    private static readonly List<Type> _kinds = [];

    private static readonly Dictionary<Type, int> _scratch = [];

    /// <summary>Counts an open library's scrolls again: what the keeper's pack holds over its keep (every kind it holds, not only what is lacked).</summary>
    internal static void Recount(BotLibrary library, long now)
    {
        library.Stock.Clear();
        library.CountedTick = now;
        library.Units = 0;

        var keeper = library.Keeper;
        var keep = BotUnload.Keeps(keeper);
        var items = keeper?.Backpack?.Items;

        if (items == null)
        {
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is SpellScroll { Deleted: false, Movable: true } scroll && scroll.GetType() != typeof(SpellScroll))
            {
                var kind = scroll.GetType();

                library.Stock[kind] = library.Stock.GetValueOrDefault(kind) + Math.Max(1, scroll.Amount);
            }
        }

        _kinds.Clear();
        _kinds.AddRange(library.Stock.Keys);

        for (var i = 0; i < _kinds.Count; i++)
        {
            var left = library.Stock[_kinds[i]] - keep.GetValueOrDefault(_kinds[i]);

            if (left <= 0)
            {
                library.Stock.Remove(_kinds[i]);
            }
            else
            {
                library.Stock[_kinds[i]] = left;
                library.Units += left;
            }
        }
    }

    /// <summary>What a shift is reckoned at a minute: the lacked scrolls in hand at the market's price, times the sell-through, over the shift.</summary>
    internal static double Claim(Dictionary<Type, int> spare)
    {
        var worth = 0.0;

        foreach (var (kind, units) in spare)
        {
            worth += units * (double)Price(kind);
        }

        return Math.Clamp(worth * SellThrough / Math.Max(1.0, ShiftMinutes), 4.0, 120.0);
    }

    // ---- Opening and shutting -----------------------------------------------------------------------------------------------

    internal static BotLibrary OpenAt(BotMobile keeper, Guild guild, Map map, Point3D stand)
    {
        if (keeper == null || guild == null || Of(guild) != null)
        {
            return null;
        }

        var library = new BotLibrary(keeper, guild, map, stand);

        Recount(library, Core.TickCount);

        if (library.Units <= 0)
        {
            return null;
        }

        _open.Add(library);
        Opened++;

        logger.Information(
            "{Name} opened a library at {Guild}'s hall ({X}, {Y}) with {Units} scrolls of {Kinds} kinds: a caster of the guild or its allies within {Reach} tiles short of one buys it there at the market's price",
            keeper.Name,
            guild.Name,
            stand.X,
            stand.Y,
            library.Units,
            library.Stock.Count,
            ReaderReach
        );

        BotEvents.Post("library", keeper, $"opened a library at {guild.Name}'s hall with {library.Units} scrolls");

        var fill = new Dictionary<string, string> { ["guild"] = guild.Name, ["scrolls"] = Listing(library, 3) };
        var line = BotVoice.Phrase("library:open", null, keeper, fill);

        if (line != null)
        {
            BotVoice.Say(keeper, "trade", line, topic: "library");
        }

        return library;
    }

    /// <summary>The kinds most held, in words, for the cry.</summary>
    private static string Listing(BotLibrary library, int most)
    {
        _ranked.Clear();

        foreach (var (kind, units) in library.Stock)
        {
            _ranked.Add((kind, units));
        }

        _ranked.Sort(static (a, b) => b.Units.CompareTo(a.Units));

        var said = new List<string>();

        for (var i = 0; i < _ranked.Count && i < most; i++)
        {
            said.Add($"{BotShopkeep.Words(_ranked[i].Kind)} at {Price(_ranked[i].Kind)}gp");
        }

        return said.Count == 0 ? "scrolls" : string.Join(", ", said);
    }

    private static readonly List<(Type Kind, int Units)> _ranked = [];

    internal static void Close(BotLibrary library, string why, bool soldOut)
    {
        if (library == null || library.Shut)
        {
            return;
        }

        library.Shut = true;
        _open.Remove(library);
        Closed++;

        if (soldOut)
        {
            SoldOut++;
        }

        logger.Information(
            "{Name} shut {Library} after {Minutes:F1} minutes — {Why}: {Sold} scrolls sold to {Served} casters for {Gold}gp",
            library.Keeper?.Name,
            library.Name,
            (Core.TickCount - library.OpenedTick) / 60000.0,
            why,
            library.Sold,
            library.Served,
            library.Takings
        );
    }

    // ---- Readers ----------------------------------------------------------------------------------------------------------

    /// <summary>The nearest open library of this caster's guild or an ally's holding <paramref name="kind"/>, within reach; null with why not.</summary>
    public static BotLibrary For(BotMobile reader, Type kind, out string refusal)
    {
        refusal = "none";

        if (reader?.Guild is not Guild guild)
        {
            return null;
        }

        BotLibrary best = null;
        var nearest = int.MaxValue;

        for (var i = 0; i < _open.Count; i++)
        {
            var library = _open[i];

            if (library.Shut || library.Map != reader.Map || ReferenceEquals(library.Keeper, reader) || !Serves(library.Guild, guild))
            {
                continue;
            }

            if (library.Stock.GetValueOrDefault(kind) <= 0)
            {
                if (refusal == "none")
                {
                    refusal = "lacks";
                }

                continue;
            }

            var away = Math.Max(Math.Abs(library.Stand.X - reader.X), Math.Abs(library.Stand.Y - reader.Y));

            if (away > ReaderReach || away >= nearest || !BotGates.Joined(reader.Map, reader.Location, library.Stand))
            {
                refusal = "far";

                continue;
            }

            best = library;
            nearest = away;
        }

        return best;
    }

    /// <summary>One scroll from the scribe's pack to the caster's, coin the other way. Returns the coin paid; <paramref name="refused"/> says why not.</summary>
    internal static int Serve(BotLibrary library, BotMobile reader, Type kind, out string refused)
    {
        refused = null;

        if (library?.Keeper is not { Deleted: false, Alive: true } keeper || library.Shut)
        {
            refused = "the library was shut";

            return 0;
        }

        var price = Price(kind);
        var given = BotHandover.Exchange(keeper, reader, kind, 1, price, false, $"a scroll at {library.Name}", out var paid, out refused);

        if (given <= 0)
        {
            return 0;
        }

        library.Sold += given;
        library.Takings += paid;
        library.Served++;
        Sold += given;
        Gold += paid;

        var left = library.Stock.GetValueOrDefault(kind) - given;

        if (left > 0)
        {
            library.Stock[kind] = left;
        }
        else
        {
            library.Stock.Remove(kind);
        }

        library.Units = Math.Max(0, library.Units - given);

        logger.Information(
            "{Keeper} sold {Reader} the {Class} a {Kind} at {Library} for {Gold}gp",
            keeper.Name,
            reader.Name,
            reader.Class?.Name,
            kind.Name,
            library.Name,
            paid
        );

        return paid;
    }

    internal static void NoteLearned(bool learned, bool answered)
    {
        if (learned)
        {
            Learned++;
        }
        else
        {
            Unwritten++;
        }

        if (answered)
        {
            WantsAnswered++;
        }
    }

    internal static void NoteFetched(int units) => Fetched += Math.Max(0, units);

    // ---- Words ------------------------------------------------------------------------------------------------------------

    public static Dictionary<string, string[]> Phrases() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["took:library"] = ["I'll keep the library at the hall for a while.", "Scrolls to sell. To the hall."],
            ["finished:library"] = ["That's the library closed.", "Enough bookkeeping for one shift."],
            ["took:library visit"] = ["The library has what my book lacks.", "Off to the hall for a scroll."],
            ["library:open"] = ["{guild}: scrolls at the hall — {scrolls}.", "Library open at the hall: {scrolls}. Guild and friends only."],
            ["library:thanks"] = ["Into the book it goes. Thank you, {other}.", "Just the page I was missing.", "{fee} gold well spent."]
        };

    public static string Describe() =>
        $"libraries {_open.Count} open now: {Opened} opened, {Closed} shut ({SoldOut} sold out); {Sold} scrolls sold for {Gold}gp, {Learned} written into a book there and then ({Unwritten} not), "
        + $"{WantsAnswered} standing wants answered at a library; {Fetched} scrolls taken out of a scribe's box for a shift; {BotLibrarian.Describe()}; {BotReader.Describe()}";

    public static void Forget()
    {
        for (var i = 0; i < _open.Count; i++)
        {
            _open[i].Shut = true;
        }

        _open.Clear();
        _demand.Clear();
        _demandTick = 0;
        Opened = 0;
        Closed = 0;
        SoldOut = 0;
        Sold = 0;
        Gold = 0;
        Learned = 0;
        Unwritten = 0;
        WantsAnswered = 0;
        Fetched = 0;
    }
}

/// <summary>
/// A scribe's shift keeping its guild's library: its spare scrolls out of the bank box, the walk to the seat, the scrolls sold to whoever
/// the auction sends, and the shelf shut when the shift or the scrolls run out (07.10.2026, life18b). The tavern's shape; measured as coin.
/// </summary>
public sealed class BotKeepLibrary : BotDeed
{
    public const string Trade = "library";

    private readonly Map _map;

    private readonly Guild _guild;

    private readonly Point3D _stand;

    private readonly Point3D _bank;

    private readonly double _claim;

    private BotLibrary _library;

    private bool _fetched;

    private double _openBefore;

    private long _openedTick;

    private int _sold;

    private int _takings;

    public BotKeepLibrary(Map map, Guild guild, Point3D stand, Point3D bank, double claim)
    {
        _map = map;
        _guild = guild;
        _stand = stand;
        _bank = bank;
        _claim = Math.Max(0.01, claim);
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _stand;

    public override double Expects => _claim;

    public override double Minutes => BotLibraries.ShiftMinutes;

    public override double Coin => 1.0;

    public override bool Still => _library != null;

    public override bool Committed => _library != null;

    public override bool Steadfast => true;

    public override double HoldsFor => _library != null ? Math.Max(1.0, BotLibraries.ShiftMinutes - OpenMinutes(Core.TickCount)) + 1.0 : 0.0;

    public override string Stage =>
        _library != null
            ? $"keeping {_library.Name}: {_library.Units} scrolls, {Sold} sold for {Takings}gp"
            : !_fetched && _bank != Point3D.Zero
                ? $"fetching its scrolls from the bank to keep {_guild?.Name}'s library"
                : $"going to {_guild?.Name}'s hall to keep its library";

    private int Sold => _sold + (_library?.Sold ?? 0);

    private int Takings => _takings + (_library?.Takings ?? 0);

    private double OpenMinutes(long now) => _openBefore + (_library != null ? (now - _openedTick) / 60000.0 : 0.0);

    public override void Taken(IBotWilful bot)
    {
        if (bot?.Self is { } body)
        {
            BotLibrarian.Went(body);
        }
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || !body.Alive || _map == null || body.Map != _map)
        {
            return BotDoing.Failed("no body, or not on the hall's map");
        }

        var now = Core.TickCount;

        if (!BotLibraries.On)
        {
            return End(now, "the libraries were shut by order", false);
        }

        if (!ReferenceEquals(body.Guild, _guild))
        {
            return End(now, "it is no longer of the guild", false);
        }

        if (_library is { Shut: true })
        {
            Carry(now);
            _library = null;
        }

        if (_library == null)
        {
            return Opening(bot, body, now);
        }

        if (now - _library.CountedTick >= BotLibraries.CountMs)
        {
            BotLibraries.Recount(_library, now);
        }

        if (_library.Units <= 0)
        {
            return End(now, "sold out", true);
        }

        if (OpenMinutes(now) >= BotLibraries.ShiftMinutes)
        {
            return End(now, "the shift is over", false);
        }

        if (!body.InRange(_library.Stand, 3))
        {
            return BotDoing.Walk(_map, _library.Stand, BotArrival.Within(1), $"back to {_library.Name}");
        }

        return BotDoing.Work(Stage);
    }

    private BotDoing Opening(IBotWilful bot, BotMobile body, long now)
    {
        // <b>Its scrolls out of the box first.</b> A scribe banks what it writes (BotUnload); the hall has no bank to reach into.
        if (!_fetched && _bank != Point3D.Zero && body.FindBankNoCreate() is { } box && box.Items.Count > 0)
        {
            if (!BotMarket.AtBank(body))
            {
                return BotDoing.Walk(_map, _bank, BotArrival.Within(2), $"to the bank for its scrolls, to keep {_guild?.Name}'s library");
            }

            BotLibraries.Spare(box, _guild, null, _wanted);

            var room = BotLibraries.StockScrolls;
            var took = 0;

            foreach (var (kind, units) in _wanted)
            {
                if (took >= room)
                {
                    break;
                }

                took += BotMarket.FromBank(bot, kind, Math.Min(units, room - took));
            }

            BotLibraries.NoteFetched(took);
        }

        _fetched = true;

        if (!body.InRange(_stand, 1))
        {
            return BotDoing.Walk(_map, _stand, BotArrival.Within(1), $"to {_guild?.Name}'s hall to keep its library");
        }

        if (BotLibraries.Of(_guild) is { } other && !ReferenceEquals(other.Keeper, body))
        {
            return BotDoing.Failed($"{other.Keeper?.Name} keeps {_guild?.Name}'s library already");
        }

        _library = BotLibraries.OpenAt(body, _guild, _map, body.Location);

        if (_library == null)
        {
            return BotDoing.Failed($"no scrolls to sell at {_guild?.Name}'s hall");
        }

        _openedTick = now;

        return BotDoing.Work(Stage);
    }

    private readonly Dictionary<Type, int> _wanted = [];

    private void Carry(long now)
    {
        if (_library == null)
        {
            return;
        }

        _openBefore += (now - _openedTick) / 60000.0;
        _sold += _library.Sold;
        _takings += _library.Takings;
    }

    private BotDoing End(long now, string why, bool soldOut)
    {
        var library = _library;
        var minutes = OpenMinutes(now);

        Shut(now, why, soldOut);

        return library == null
            ? BotDoing.Done(why)
            : BotDoing.Done($"{why}: sold {Sold} scrolls for {Takings}gp in {minutes:F1} minutes at {_guild?.Name}'s hall");
    }

    private void Shut(long now, string why, bool soldOut)
    {
        if (_library == null)
        {
            return;
        }

        var library = _library;

        Carry(now);
        _library = null;
        BotLibraries.Close(library, why, soldOut);
    }

    public override void Paused(IBotWilful bot) => Shut(Core.TickCount, "the scribe was called away", false);

    public override void Drop(IBotWilful bot) => Shut(Core.TickCount, "the shift was given up", false);
}

/// <summary>
/// Offers a guild's scribe — Inscription <see cref="BotLibraries.LeastInscribe"/> and up — the shift at its guild's library when it holds
/// spare scrolls its guild's or allies' casters lack and the guild has none open (07.10.2026, life18b). Every gate counted.
/// </summary>
public sealed class BotLibrarian : IBotProposer
{
    public string Name => "Librarian";

    public BotStanding Rung => BotStanding.Free;

    public static long Looks { get; private set; }

    public static long NoGuild { get; private set; }

    public static long Rested { get; private set; }

    public static long Kept { get; private set; }

    public static long Far { get; private set; }

    public static long Thin { get; private set; }

    public static long Offered { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    private static readonly Dictionary<Serial, long> _looked = [];

    private static readonly Dictionary<Type, int> _pack = [];

    private static readonly Dictionary<Type, int> _box = [];

    internal static void Went(Mobile scribe)
    {
        if (scribe != null)
        {
            _last[scribe.Serial] = Core.TickCount;
        }
    }

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotLibraries.On || bot?.Self is not BotMobile body || !body.Alive || body.Tired || body.Squad != null)
        {
            return null;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal || body.Resolve?.Deed is BotKeepLibrary)
        {
            return null;
        }

        if (body.Skills[SkillName.Inscribe].Value < BotLibraries.LeastInscribe)
        {
            return null;
        }

        Looks++;

        if (body.Guild is not Guild guild || BotUnderworld.Member(body))
        {
            NoGuild++;

            return null;
        }

        var now = Core.TickCount;

        if (_last.TryGetValue(body.Serial, out var when) && now - when < BotLibraries.RestMs)
        {
            Rested++;

            return null;
        }

        if (BotLibraries.Of(guild) != null)
        {
            Kept++;

            return null;
        }

        var seat = BotSeat.Of(guild);

        if (seat == Point3D.Zero || !Utility.InRange(body.Location, seat, BotLibraries.KeeperReach) || !BotGates.Joined(map, body.Location, seat))
        {
            Far++;

            return null;
        }

        // The pack and the box are read at most once a minute for one scribe.
        if (_looked.TryGetValue(body.Serial, out var at) && now - at < BotLibraries.LookMs)
        {
            return null;
        }

        if (_looked.Count > 1024)
        {
            _looked.Clear();
        }

        _looked[body.Serial] = now;

        var keep = BotUnload.Keeps(bot);
        var inPack = BotLibraries.Spare(body.Backpack, guild, keep, _pack);
        var inBox = BotLibraries.Spare(body.FindBankNoCreate(), guild, null, _box);

        if (inPack + inBox < BotLibraries.LeastScrolls)
        {
            Thin++;

            return null;
        }

        foreach (var (kind, units) in _box)
        {
            _pack[kind] = _pack.GetValueOrDefault(kind) + units;
        }

        var bank = inBox > 0 ? BotShopkeep.BankNear(map, seat) : Point3D.Zero;

        Offered++;

        return new BotKeepLibrary(map, guild, BotLibraries.StandBy(map, seat), bank, BotLibraries.Claim(_pack));
    }

    public static string Describe() =>
        Looks == 0
            ? "no scribe looked at for a library"
            : $"{Looks} scribes looked at for a library: {Offered} offered, {NoGuild} had no guild, {Rested} were resting from one, {Kept} found their guild's open, "
              + $"{Far} were over {BotLibraries.KeeperReach} tiles from the seat, {Thin} held under {BotLibraries.LeastScrolls} spare scrolls its casters lack";

    public static void Forget()
    {
        _last.Clear();
        _looked.Clear();
        Looks = 0;
        NoGuild = 0;
        Rested = 0;
        Kept = 0;
        Far = 0;
        Thin = 0;
        Offered = 0;
    }
}

/// <summary>
/// A caster's walk to its guild's or an ally's library for the scroll its book needs next: bought hand to hand at the market's price and
/// written into the book there and then (07.10.2026, life18b). Unpaid: the auction prices it at its claim, and <see cref="BotReader"/>
/// asks every reason it would fail first.
/// </summary>
public sealed class BotReadAt : BotDeed
{
    public const string Trade = "library visit";

    private readonly BotLibrary _library;

    private readonly Type _kind;

    private readonly int _spell;

    private readonly int _price;

    private readonly double _claim;

    private long _began;

    private int _paid;

    public BotReadAt(BotLibrary library, Type kind, int spell, int price, double claim)
    {
        _library = library;
        _kind = kind;
        _spell = spell;
        _price = price;
        _claim = claim;
    }

    public override string Kind => Trade;

    public override Map Map => _library?.Map;

    public override Point3D Where => _library?.Stand ?? Point3D.Zero;

    public override double Expects => _claim;

    public override double Minutes => 1.0;

    public override int Outlay => _price;

    public override double Coin => 0.0;

    public override int Made => _paid;

    public override bool Unpaid => true;

    public override bool Steadfast => true;

    public override string Stage => $"going to {_library?.Name} for {_kind?.Name}";

    public override bool Repeats(BotDeed other) => other is BotReadAt read && ReferenceEquals(read._library, _library) && read._kind == _kind;

    public override void Taken(IBotWilful bot)
    {
        _began = Core.TickCount;

        if (bot?.Self is { } body)
        {
            BotReader.Went(body);
        }
    }

    public override void Resumed(IBotWilful bot) => _began = Core.TickCount;

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || !body.Alive || _library == null || body.Map != _library.Map || _kind == null)
        {
            return BotDoing.Failed("no body, or no library on this map");
        }

        if (BotGrimoire.Holds(body, _spell))
        {
            return BotDoing.Done($"already knows {_kind.Name}");
        }

        var keeper = _library.Keeper;

        if (_library.Shut || keeper is not { Deleted: false, Alive: true })
        {
            BotReader.NoteShut();

            return BotDoing.Failed($"{_library.Name} shut before it got there");
        }

        if (!body.InRange(keeper, BotHandover.MeetTiles))
        {
            if (Core.TickCount - _began >= BotReader.WalkMs)
            {
                BotReader.NoteNeverGot();

                return BotDoing.Failed($"could not get to {_library.Name} in {BotReader.WalkMs / 60000} minutes");
            }

            return BotDoing.Walk(_library.Map, keeper, BotArrival.Within(BotHandover.MeetTiles), $"to {_library.Name} for {_kind.Name}");
        }

        _paid = BotLibraries.Serve(_library, body, _kind, out var refused);

        if (_paid <= 0)
        {
            BotReader.NoteUnserved();

            return BotDoing.Failed($"no {_kind.Name} at {_library.Name}: {refused}");
        }

        var learned = false;
        string why = null;
        var scrolls = BotQuill.Gather(body, _kind);

        for (var i = 0; i < scrolls.Count && !learned; i++)
        {
            learned = BotGrimoire.Write(body, scrolls[i], out why);
        }

        // The spell is in the book: a want still standing for it is asking for nothing, and its money comes back.
        var answered = learned && BotMarket.Wanted(bot, _kind) != null;

        if (learned)
        {
            BotMarket.Withdrawn(bot, _kind, BotWithdraw.Have);
        }

        BotLibraries.NoteLearned(learned, answered);

        var fill = new Dictionary<string, string> { ["fee"] = _paid.ToString() };

        BotCampTalk.Say(body, "library:thanks", null, keeper, fill);

        return BotDoing.Done(
            learned
                ? $"bought {_kind.Name} at {_library.Name} for {_paid}gp and wrote it into its book"
                : $"bought {_kind.Name} at {_library.Name} for {_paid}gp; the book would not take it: {why ?? "no reason given"}"
        );
    }
}

/// <summary>
/// Offers a caster whose book is short of a spell the scroll at the nearest open library of its guild or an ally that holds it (07.10.2026,
/// life18b): before a far walk to a counter, or while a want for it stands unanswered. Every gate counted.
/// </summary>
public sealed class BotReader : IBotProposer
{
    public string Name => "Reader";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>What the visit is reckoned at a minute: a little over an errand for a scroll (<c>BotAcquire.Prior</c>, 12), the scroll being in stock.</summary>
    public static double Prior { get; set; } = 16.0;

    /// <summary>What a want already standing for the scroll adds: the caster is waiting on it.</summary>
    public static double WantWorth { get; set; } = 8.0;

    /// <summary>How long one caster waits between visits. Five minutes.</summary>
    public static int EveryMs { get; set; } = 300000;

    /// <summary>How long the walk may take. Four minutes.</summary>
    public static int WalkMs { get; set; } = 240000;

    public static long Asked { get; private set; }

    public static long Engaged { get; private set; }

    public static long TooSoon { get; private set; }

    public static long None { get; private set; }

    public static long Lacks { get; private set; }

    public static long Far { get; private set; }

    public static long Poor { get; private set; }

    public static long Offered { get; private set; }

    public static long ForWant { get; private set; }

    public static long Walked { get; private set; }

    public static long Shut { get; private set; }

    public static long NeverGot { get; private set; }

    public static long Unserved { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    internal static void Went(Mobile who)
    {
        if (who != null)
        {
            _last[who.Serial] = Core.TickCount;
            Walked++;
        }
    }

    internal static void NoteShut() => Shut++;

    internal static void NoteNeverGot() => NeverGot++;

    internal static void NoteUnserved() => Unserved++;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotLibraries.On || BotLibraries.Open.Count == 0)
        {
            return null;
        }

        if (bot?.Self is not BotMobile who || !who.Alive || who.Map == null || who.Map == Map.Internal || who.Guild == null)
        {
            return null;
        }

        if (who.Resolve?.Deed is BotReadAt or BotKeepLibrary || !BotLibraries.Reading(who, out var kind))
        {
            return null;
        }

        Asked++;

        if (who.Squad != null || who.Combatant != null || BotDelveParty.Delving(who))
        {
            Engaged++;

            return null;
        }

        var now = Core.TickCount;

        if (_last.TryGetValue(who.Serial, out var when) && now - when < EveryMs)
        {
            TooSoon++;

            return null;
        }

        var library = BotLibraries.For(who, kind, out var refusal);

        if (library == null)
        {
            switch (refusal)
            {
                case "lacks":
                    {
                        Lacks++;

                        break;
                    }
                case "far":
                    {
                        Far++;

                        break;
                    }
                default:
                    {
                        None++;

                        break;
                    }
            }

            return null;
        }

        var price = BotLibraries.Price(kind);
        var want = BotMarket.Wanted(bot, kind);

        // The money a standing want holds for this very scroll pays for it at the library (BotHandover.Exchange reads it).
        if (BotYield.Spendable(who) + (want != null ? BotMarket.ReservedFor(who, kind) : 0) < price)
        {
            Poor++;

            return null;
        }

        var claim = Prior;

        if (want != null)
        {
            claim += WantWorth;
            ForWant++;
        }

        Offered++;

        return new BotReadAt(library, kind, BotGrimoire.SpellOf(kind), price, claim);
    }

    public static string Describe() =>
        Asked == 0
            ? "no caster short of a scroll looked at for a library"
            : $"{Asked} casters short of a scroll looked at for a library: {Offered} sent ({ForWant} with a want standing for it), {Walked} went; {Engaged} were in a fight, a company or a delve, "
              + $"{TooSoon} within {EveryMs / 60000} minutes of their last, {None} had no library of theirs or an ally's open, {Lacks} found none holding the scroll, {Far} found it over {BotLibraries.ReaderReach} tiles off, {Poor} could not pay; "
              + $"{Shut} found it shut, {NeverGot} could not get there, {Unserved} were not served";

    public static void Forget()
    {
        _last.Clear();
        Asked = 0;
        Engaged = 0;
        TooSoon = 0;
        None = 0;
        Lacks = 0;
        Far = 0;
        Poor = 0;
        Offered = 0;
        ForWant = 0;
        Walked = 0;
        Shut = 0;
        NeverGot = 0;
        Unserved = 0;
    }
}
