using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>One ragman's buying post while it is open: the trader keeping it, the cave mouth it serves, where it stands, and what it bought.</summary>
public sealed class BotRagPost
{
    public BotRagPost(BotMobile keeper, Map map, Point3D stand, Point3D mouth, string dungeon)
    {
        Keeper = keeper;
        Map = map;
        Stand = stand;
        Mouth = mouth;
        Dungeon = dungeon;
        OpenedTick = Core.TickCount;
    }

    public BotMobile Keeper { get; }

    public Map Map { get; }

    public Point3D Stand { get; }

    public Point3D Mouth { get; }

    public string Dungeon { get; }

    public long OpenedTick { get; }

    public bool Shut { get; internal set; }

    public int Units { get; internal set; }

    public int Paid { get; internal set; }

    /// <summary>What the goods bought fetch at the towns' counters: the post's own production (see <see cref="BotKeepRagPost.Made"/>).</summary>
    public int Worth { get; internal set; }

    public int Sellers { get; internal set; }

    public string Name => $"{Keeper?.Name}'s buying post by the mouth of {Dungeon}";
}

/// <summary>
/// The ragmen: a trader keeping a buying post on safe ground near a dungeon's mouth, buying the hunters' loot below what a town's counter
/// pays and carrying it to the counters itself afterwards (07.10.2026, life18b).
///
/// <para>
/// <b>Patrick, 07.10.2026: "a ragman buying loot from hunters at the dungeon mouth"; that morning: "let's try, but if he dies often to the
/// mobs (there are many hostile mobs at a dungeon mouth) we take the role out, or make him stand REALLY safely, though not too far from
/// the entrance".</b> So the post is chosen, once a mouth and remembered for <see cref="PostMemoryMs"/>, on ground <see cref="PostNear"/> to
/// <see cref="PostFar"/> tiles from the mouth where the zones' map reads clear for <see cref="SafeSlack"/> tiles every way (outside every
/// aggressive spawner's patrol and its fringe, <see cref="BotZones.LevelAt(Map, int, int)"/>), no blood stands on the square
/// (<see cref="BotQuad.Muscle(Map, Point3D)"/>), and no creature that attacks on sight is alive within its own perception of it (the reach
/// <c>BotSight</c> prices: <c>RangePerception</c>, capped at <see cref="BotZones.PerceptionMost"/>, and the margin). At the post the ragman
/// leaves at once when its sight holds a danger it cannot beat (<c>BotJourney.Sighted</c>, the reaches <c>BotSight</c> keeps), and the post is
/// rested for <see cref="UnsafeMs"/>; within a creature's reach the sight's own stand-down puts it off its work anyway. Its deaths are
/// counted apart, and <c>BotTrades.Ragmen</c> switches the role off live.
/// </para>
///
/// <para>
/// <b>The money is honest on both sides.</b> The hunter is paid <see cref="Share"/> of what the nearest counter pays for each thing, hand to
/// hand (<see cref="BotHandover.Exchange"/>), and keeps hunting instead of walking a pack across the island; the ragman pays out of its own
/// purse, keeps <see cref="KeepPurse"/>, and sells the goods over a counter later (<see cref="BotRagHauler"/>, the peddle) — the counter is
/// where the coin enters the world, as it always was.
/// </para>
/// </summary>
public static class BotRagmen
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRagmen));

    // ---- Dials ------------------------------------------------------------------------------------------------------------

    public static bool On => BotTrades.RagmenOn;

    /// <summary>The most posts open on the island at once. Three.</summary>
    public static int MostOpen { get; set; } = 3;

    /// <summary>How long a ragman keeps its post, in minutes. Twenty-five.</summary>
    public static double ShiftMinutes { get; set; } = 25.0;

    /// <summary>How long after a shift the same trader may keep another. Thirty minutes.</summary>
    public static int RestMs { get; set; } = 1800000;

    /// <summary>How far a trader may be from a mouth and be offered a post at it. Six hundred tiles.</summary>
    public static int KeeperReach { get; set; } = 600;

    /// <summary>What a trader must have to spend to keep a post. Four hundred.</summary>
    public static int LeastPurse { get; set; } = 400;

    /// <summary>What a ragman keeps whatever it buys. A hundred and fifty.</summary>
    public static int KeepPurse { get; set; } = 150;

    /// <summary>What the ragman pays, of the nearest counter's price. Six tenths.</summary>
    public static double Share { get; set; } = 0.6;

    /// <summary>The nearest a post stands to its mouth, and the furthest. Forty and eighty tiles.</summary>
    public static int PostNear { get; set; } = 40;

    public static int PostFar { get; set; } = 80;

    /// <summary>How far round a post the zones' map must read clear. Sixteen tiles.</summary>
    public static int SafeSlack { get; set; } = 16;

    /// <summary>The margin added to a creature's perception when a post is looked for. Four tiles.</summary>
    public static int CreatureMargin { get; set; } = 4;

    /// <summary>How long a post found is kept for its mouth. Ten minutes.</summary>
    public static int PostMemoryMs { get; set; } = 600000;

    /// <summary>How long a post a ragman left for a danger is not used again. Thirty minutes.</summary>
    public static int UnsafeMs { get; set; } = 1800000;

    /// <summary>How far round a mouth (and inside its dungeon) the hunters are counted. Two hundred tiles.</summary>
    public static int TrafficReach { get; set; } = 200;

    /// <summary>The fewest hunters about a mouth for a post to be worth keeping. Two.</summary>
    public static int LeastTraffic { get; set; } = 2;

    /// <summary>How often the hunters about every mouth are counted. A minute.</summary>
    public static int TrafficMs { get; set; } = 60000;

    /// <summary>The share of its carrying weight at which a ragman stops buying and shuts. Six tenths.</summary>
    public static double Heavy { get; set; } = 0.6;

    /// <summary>What a post's shift is reckoned at a minute before the ledger knows better.</summary>
    public static double Prior { get; set; } = 25.0;

    // ---- Counters ---------------------------------------------------------------------------------------------------------

    public static long Opened { get; private set; }

    public static long Closed { get; private set; }

    public static long ShutTime { get; private set; }

    public static long ShutFull { get; private set; }

    public static long ShutSpent { get; private set; }

    public static long Fled { get; private set; }

    public static long KilledAtPost { get; private set; }

    public static long KilledOnWay { get; private set; }

    public static long Bought { get; private set; }

    public static long PaidGold { get; private set; }

    public static long WorthGold { get; private set; }

    public static long Sales { get; private set; }

    public static long PostsFound { get; private set; }

    public static long NoSafeGround { get; private set; }

    public static long HaulsOffered { get; private set; }

    public static long HaulWorth { get; private set; }

    private static readonly List<BotRagPost> _open = [];

    private static readonly Dictionary<Point3D, (Point3D Post, long Tick)> _posts = [];

    private static readonly Dictionary<Point3D, long> _unsafe = [];

    private static readonly List<(BotGates.Gate Gate, BotDungeon.Deep Deep, int Hunters)> _mouths = [];

    private static long _trafficTick;

    /// <summary>What each ragman bought and has not yet carried to a counter, by kind. Read by <see cref="BotRagHauler"/>.</summary>
    private static readonly Dictionary<Serial, Dictionary<Type, int>> _stock = [];

    public static IReadOnlyList<BotRagPost> Open => _open;

    // ---- The mouths -------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Every mouth on the island side — a gate from the island into the dungeon block — with the hunters counted round it and inside the
    /// dungeon it leads to. One walk of the gates and one of the population list every <see cref="TrafficMs"/>.
    /// </summary>
    private static void Traffic(long now)
    {
        if (now - _trafficTick < TrafficMs && _mouths.Count > 0)
        {
            return;
        }

        _trafficTick = now;
        _mouths.Clear();

        var gates = BotGates.All;

        for (var i = 0; i < gates.Count; i++)
        {
            var gate = gates[i];

            if (gate == null || gate.Moongate || gate.From.X >= BotGates.Edge || gate.To.X < BotGates.Edge)
            {
                continue;
            }

            _mouths.Add((gate, BotDungeon.Holding(gate.To), 0));
        }

        var bots = BotPopulation.Bots;

        for (var b = 0; b < bots.Count; b++)
        {
            var bot = bots[b];

            if (bot is not { Deleted: false, Alive: true } || !BotPeddler.Hunter(bot))
            {
                continue;
            }

            var under = BotDungeon.Under(bot.Location) ? BotDungeon.Holding(bot.Location) : null;

            for (var m = 0; m < _mouths.Count; m++)
            {
                var (gate, deep, hunters) = _mouths[m];

                if (under != null ? ReferenceEquals(under, deep) : Utility.InRange(bot.Location, gate.From, TrafficReach))
                {
                    _mouths[m] = (gate, deep, hunters + 1);
                }
            }
        }
    }

    /// <summary>The busiest mouth within reach of a trader with no post at it, or null.</summary>
    internal static BotGates.Gate Busiest(BotMobile trader, out string dungeon, out int hunters)
    {
        dungeon = null;
        hunters = 0;
        Traffic(Core.TickCount);

        BotGates.Gate best = null;

        for (var m = 0; m < _mouths.Count; m++)
        {
            var (gate, deep, count) = _mouths[m];

            if (count < LeastTraffic || count <= hunters || !Utility.InRange(trader.Location, gate.From, KeeperReach) || At(gate.From) != null
                || !BotGates.Joined(trader.Map, trader.Location, gate.From))
            {
                continue;
            }

            best = gate;
            hunters = count;
            dungeon = deep?.Name ?? gate.Name;
        }

        return best;
    }

    public static BotRagPost At(Point3D mouth)
    {
        for (var i = 0; i < _open.Count; i++)
        {
            if (!_open[i].Shut && _open[i].Mouth == mouth)
            {
                return _open[i];
            }
        }

        return null;
    }

    // ---- The post ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Safe ground for a post at this mouth, or <c>Point3D.Zero</c>. Rings from <see cref="PostNear"/> out to <see cref="PostFar"/> every
    /// eight tiles, sixteen bearings each, the first that passes every test; remembered for <see cref="PostMemoryMs"/>, so the spatial
    /// queries run once a mouth, not once a beat.
    /// </summary>
    internal static Point3D Post(Map map, Point3D mouth)
    {
        var now = Core.TickCount;

        if (_unsafe.TryGetValue(mouth, out var left) && now - left < UnsafeMs)
        {
            return Point3D.Zero;
        }

        if (_posts.TryGetValue(mouth, out var known) && now - known.Tick < PostMemoryMs)
        {
            return known.Post;
        }

        var found = Point3D.Zero;

        for (var r = PostNear; r <= PostFar && found == Point3D.Zero; r += 8)
        {
            for (var k = 0; k < 16 && found == Point3D.Zero; k++)
            {
                var angle = k * Math.PI / 8.0;
                var x = mouth.X + (int)Math.Round(r * Math.Cos(angle));
                var y = mouth.Y + (int)Math.Round(r * Math.Sin(angle));

                if (Safe(map, x, y, mouth, out var at))
                {
                    found = at;
                }
            }
        }

        if (found != Point3D.Zero)
        {
            PostsFound++;
        }
        else
        {
            NoSafeGround++;
        }

        _posts[mouth] = (found, now);

        return found;
    }

    private static bool Safe(Map map, int x, int y, Point3D mouth, out Point3D at)
    {
        at = Point3D.Zero;

        if (x < 0 || y < 0 || x >= BotGates.Edge || !BotStep.Settle(map, x, y, out var z))
        {
            return false;
        }

        var here = new Point3D(x, y, z);

        if (BotDungeon.Under(here) || BotQuad.Muscle(map, here) > 0.0)
        {
            return false;
        }

        // Clear ground all round: no patrol of creatures that attack on sight, and no fringe of one.
        for (var dx = -SafeSlack; dx <= SafeSlack; dx += Math.Max(1, SafeSlack / 2))
        {
            for (var dy = -SafeSlack; dy <= SafeSlack; dy += Math.Max(1, SafeSlack / 2))
            {
                if (BotZones.LevelAt(map, x + dx, y + dy) != BotZoneLevel.Clear)
                {
                    return false;
                }
            }
        }

        if (!BotGates.Joined(map, here, mouth))
        {
            return false;
        }

        // And nothing alive now that would notice a body standing there.
        foreach (var c in map.GetMobilesInRange<BaseCreature>(here, BotZones.PerceptionMost + CreatureMargin))
        {
            if (c is not { Deleted: false, Alive: true } || c.Controlled || c.Summoned || c.Blessed || !BotHaunts.AttacksFirst(c.FightMode))
            {
                continue;
            }

            var reach = Math.Clamp(c.RangePerception, 1, Math.Max(1, BotZones.PerceptionMost)) + CreatureMargin;

            if (Math.Abs(c.X - x) <= reach && Math.Abs(c.Y - y) <= reach)
            {
                return false;
            }
        }

        at = here;

        return true;
    }

    /// <summary>The ragman left this post for a danger: the post is not used again for <see cref="UnsafeMs"/>.</summary>
    internal static void Unsafe(BotRagPost post)
    {
        if (post != null)
        {
            _unsafe[post.Mouth] = Core.TickCount;
            _posts.Remove(post.Mouth);
        }
    }

    internal static BotRagPost OpenAt(BotMobile keeper, Map map, Point3D stand, Point3D mouth, string dungeon)
    {
        if (keeper == null || At(mouth) != null || _open.Count >= MostOpen)
        {
            return null;
        }

        var post = new BotRagPost(keeper, map, stand, mouth, dungeon);

        _open.Add(post);
        Opened++;

        logger.Information(
            "{Name} opened a buying post at ({X}, {Y}), {Away} tiles from the mouth of {Dungeon}: hunters within {Reach} tiles sell their loot there at {Share:P0} of the nearest counter's price, {Purse}gp to spend",
            keeper.Name,
            stand.X,
            stand.Y,
            Math.Max(Math.Abs(stand.X - mouth.X), Math.Abs(stand.Y - mouth.Y)),
            dungeon,
            BotRagSeller.Reach,
            Share,
            Math.Max(0, BotYield.Spendable(keeper) - KeepPurse)
        );

        BotEvents.Post("ragman", keeper, $"opened a buying post by the mouth of {dungeon}");

        var fill = new Dictionary<string, string> { ["dungeon"] = dungeon, ["share"] = $"{Share:P0}" };
        var line = BotVoice.Phrase("ragman:open", null, keeper, fill);

        if (line != null)
        {
            BotVoice.Say(keeper, "trade", line, topic: "ragman");
        }

        return post;
    }

    internal static void Close(BotRagPost post, string why, string cause)
    {
        if (post == null || post.Shut)
        {
            return;
        }

        post.Shut = true;
        _open.Remove(post);
        Closed++;

        switch (cause)
        {
            case "time": ShutTime++; break;
            case "full": ShutFull++; break;
            case "spent": ShutSpent++; break;
            case "fled": Fled++; break;
        }

        logger.Information(
            "{Name} shut {Post} after {Minutes:F1} minutes — {Why}: bought {Units} things from {Sellers} hunters for {Paid}gp, worth {Worth}gp at the counters",
            post.Keeper?.Name,
            post.Name,
            (Core.TickCount - post.OpenedTick) / 60000.0,
            why,
            post.Units,
            post.Sellers,
            post.Paid,
            post.Worth
        );
    }

    internal static void Killed(bool atPost, BotMobile keeper, BotRagPost post)
    {
        if (atPost)
        {
            KilledAtPost++;
        }
        else
        {
            KilledOnWay++;
        }

        logger.Information(
            "{Name} the ragman was killed {Where} — ragmen killed at the post: {AtPost}, on the way: {OnWay}",
            keeper?.Name,
            atPost ? $"at {post?.Name}" : "on the way to its post",
            KilledAtPost,
            KilledOnWay
        );
    }

    // ---- Buying -----------------------------------------------------------------------------------------------------------

    /// <summary>The nearest open post a hunter here may sell at, within <see cref="BotRagSeller.Reach"/> on joined ground; null with how far the nearest stood.</summary>
    public static BotRagPost For(BotMobile hunter, out int away)
    {
        away = int.MaxValue;
        BotRagPost best = null;

        for (var i = 0; i < _open.Count; i++)
        {
            var post = _open[i];

            if (post.Shut || post.Map != hunter.Map || ReferenceEquals(post.Keeper, hunter) || post.Keeper is not { Deleted: false, Alive: true })
            {
                continue;
            }

            var d = Math.Max(Math.Abs(post.Stand.X - hunter.X), Math.Abs(post.Stand.Y - hunter.Y));

            if (d < away && d <= BotRagSeller.Reach && BotGates.Joined(hunter.Map, hunter.Location, post.Stand))
            {
                best = post;
                away = d;
            }
        }

        return best;
    }

    /// <summary>What the ragman of this post may still spend.</summary>
    public static int Purse(BotRagPost post) => post?.Keeper == null ? 0 : Math.Max(0, BotYield.Spendable(post.Keeper) - KeepPurse);

    /// <summary>
    /// One kind of a hunter's loot from its pack to the ragman's, coin the other way, at <paramref name="price"/> apiece and no more than the
    /// ragman may spend. Returns the units bought.
    /// </summary>
    internal static int Buy(BotRagPost post, IBotWilful hunter, Type kind, int units, int price, int counter, out int paid, out string refused)
    {
        paid = 0;
        refused = null;

        if (post?.Keeper is not { Deleted: false, Alive: true } keeper || post.Shut)
        {
            refused = "the post was shut";

            return 0;
        }

        var affords = price <= 0 ? 0 : Purse(post) / price;

        units = Math.Min(units, affords);

        if (units <= 0)
        {
            refused = $"{keeper.Name} had nothing left to spend";

            return 0;
        }

        var given = BotHandover.Exchange(hunter, keeper, kind, units, price, false, $"loot at {post.Name}", out paid, out refused);

        if (given <= 0)
        {
            return 0;
        }

        post.Units += given;
        post.Paid += paid;
        post.Worth += given * counter;
        Bought += given;
        PaidGold += paid;
        WorthGold += given * counter;

        if (!_stock.TryGetValue(keeper.Serial, out var stock))
        {
            stock = [];
            _stock[keeper.Serial] = stock;
        }

        stock[kind] = stock.GetValueOrDefault(kind) + given;

        return given;
    }

    internal static void Sold(BotRagPost post)
    {
        if (post != null)
        {
            post.Sellers++;
        }

        Sales++;
    }

    // ---- The haul ---------------------------------------------------------------------------------------------------------

    internal static Dictionary<Type, int> StockOf(Mobile ragman) =>
        ragman != null && _stock.TryGetValue(ragman.Serial, out var stock) ? stock : null;

    internal static void Cleared(Mobile ragman)
    {
        if (ragman != null)
        {
            _stock.Remove(ragman.Serial);
        }
    }

    internal static void Hauled(int worth)
    {
        HaulsOffered++;
        HaulWorth += Math.Max(0, worth);
    }

    // ---- Words ------------------------------------------------------------------------------------------------------------

    public static Dictionary<string, string[]> Phrases() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["took:ragpost"] = ["Off to buy loot by the caves.", "There's trade at the dungeon mouth."],
            ["finished:ragpost"] = ["That's the post closed. To town with this lot.", "Enough buying for one day."],
            ["took:ragman sale"] = ["The ragman's close. Saves me the walk to town.", "Selling this lot at the post."],
            ["ragman:open"] = ["Buying loot by the mouth of {dungeon}: {share} of the town's price, no walk to town.", "Ragman at the {dungeon} mouth. Bring me your loot."],
            ["ragman:thanks"] = ["{fee} gold and no walk to town. Fair enough.", "Pleasure, {other}.", "Lighter pack, heavier purse."]
        };

    public static string Describe() =>
        $"ragmen {_open.Count} open now: {Opened} opened, {Closed} shut ({ShutTime} at the end of the shift, {ShutFull} with a full pack, {ShutSpent} with the purse spent, {Fled} left the post for a danger in sight); "
        + $"ragmen killed at the post: {KilledAtPost}, on the way to it: {KilledOnWay}; {Sales} sales by hunters, {Bought} things bought for {PaidGold}gp, worth {WorthGold}gp at the counters; "
        + $"{PostsFound} safe posts found, {NoSafeGround} mouths with no safe ground {PostNear}–{PostFar} tiles off; {HaulsOffered} hauls to a counter offered, {HaulWorth}gp at the counters' prices; "
        + $"{BotRagman.Describe()}; {BotRagSeller.Describe()}";

    public static void Forget()
    {
        for (var i = 0; i < _open.Count; i++)
        {
            _open[i].Shut = true;
        }

        _open.Clear();
        _posts.Clear();
        _unsafe.Clear();
        _mouths.Clear();
        _stock.Clear();
        _trafficTick = 0;
        Opened = 0;
        Closed = 0;
        ShutTime = 0;
        ShutFull = 0;
        ShutSpent = 0;
        Fled = 0;
        KilledAtPost = 0;
        KilledOnWay = 0;
        Bought = 0;
        PaidGold = 0;
        WorthGold = 0;
        Sales = 0;
        PostsFound = 0;
        NoSafeGround = 0;
        HaulsOffered = 0;
        HaulWorth = 0;
    }
}

/// <summary>
/// A trader's shift keeping a buying post near a dungeon's mouth: the walk to the safe ground, the post opened, the hunters' loot bought as
/// the auction sends them, and the post shut when the shift, the purse or the pack runs out — or at once when a danger it cannot beat comes
/// into sight (07.10.2026, life18b). Measured as coin, with the goods bought counted at what the counters pay for them.
/// </summary>
public sealed class BotKeepRagPost : BotDeed
{
    public const string Trade = "ragpost";

    private readonly Map _map;

    private readonly Point3D _stand;

    private readonly Point3D _mouth;

    private readonly string _dungeon;

    private readonly double _claim;

    private BotRagPost _post;

    private long _openedTick;

    private int _worth;

    private bool _paused;

    public BotKeepRagPost(Map map, Point3D stand, Point3D mouth, string dungeon, double claim)
    {
        _map = map;
        _stand = stand;
        _mouth = mouth;
        _dungeon = dungeon;
        _claim = Math.Max(0.01, claim);
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _stand;

    public override double Expects => _claim;

    public override double Minutes => BotRagmen.ShiftMinutes;

    public override double Coin => 1.0;

    /// <summary>The goods bought, at what the counters pay for them: the coin paid out is the other half of the post's takings.</summary>
    public override int Made => _worth + (_post?.Worth ?? 0);

    public override bool Still => _post != null;

    public override bool Committed => _post != null;

    public override bool Steadfast => true;

    public override double HoldsFor => _post != null ? Math.Max(1.0, BotRagmen.ShiftMinutes - (Core.TickCount - _openedTick) / 60000.0) + 1.0 : 0.0;

    public override string Stage =>
        _post != null
            ? $"keeping {_post.Name}: bought {_post.Units} things for {_post.Paid}gp"
            : $"going to its buying post by the mouth of {_dungeon}";

    public override void Taken(IBotWilful bot)
    {
        if (bot?.Self is { } body)
        {
            BotRagman.Went(body);
        }
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || !body.Alive || _map == null || body.Map != _map)
        {
            return BotDoing.Failed("no body, or not on the post's map");
        }

        var now = Core.TickCount;

        if (!BotRagmen.On)
        {
            return End(now, "the posts were shut by order", null);
        }

        if (_post is { Shut: true })
        {
            _worth += _post.Worth;
            _post = null;
        }

        // <b>Gone at once when something it cannot beat is in sight (Patrick, 07.10.2026).</b> The sight keeps the reaches of what the
        // bot has seen within its twenty tiles and cannot beat; a ragman does not wait for one to come within its own reach.
        // On the way the sight steers the walk round what it sees (BotSight); only near the post does a danger send it home.
        if (body.Journey?.Sighted is { Count: > 0 } && (_post != null || body.InRange(_stand, 24)))
        {
            var post = _post;

            BotRagmen.Unsafe(post ?? new BotRagPost(body, _map, _stand, _mouth, _dungeon));

            if (post == null)
            {
                return BotDoing.Failed($"a danger it cannot beat is in sight on the way to the mouth of {_dungeon}");
            }

            return End(now, "left the post: a danger it cannot beat came within sight", "fled");
        }

        if (_post == null)
        {
            if (!body.InRange(_stand, 1))
            {
                return BotDoing.Walk(_map, _stand, BotArrival.Within(1), $"to its buying post by the mouth of {_dungeon}");
            }

            _post = BotRagmen.OpenAt(body, _map, body.Location, _mouth, _dungeon);

            if (_post == null)
            {
                return BotDoing.Failed($"no post could open by the mouth of {_dungeon}: another is open there, or {BotRagmen.MostOpen} are open");
            }

            _openedTick = now;

            return BotDoing.Work(Stage);
        }

        if (now - _openedTick >= BotRagmen.ShiftMinutes * 60000.0)
        {
            return End(now, "the shift is over", "time");
        }

        if (BotRagmen.Purse(_post) < 5)
        {
            return End(now, "its purse is spent", "spent");
        }

        if (body.MaxWeight > 0 && body.TotalWeight >= body.MaxWeight * BotRagmen.Heavy)
        {
            return End(now, "its pack is full", "full");
        }

        if (!body.InRange(_post.Stand, 2))
        {
            return BotDoing.Walk(_map, _post.Stand, BotArrival.Within(1), $"back to {_post.Name}");
        }

        return BotDoing.Work(Stage);
    }

    private BotDoing End(long now, string why, string cause)
    {
        var post = _post;

        if (post == null)
        {
            return BotDoing.Done(why);
        }

        _worth += post.Worth;
        _post = null;
        BotRagmen.Close(post, why, cause);

        return BotDoing.Done($"{why}: bought {post.Units} things from {post.Sellers} hunters for {post.Paid}gp, worth {post.Worth}gp at the counters, in {(now - _openedTick) / 60000.0:F1} minutes");
    }

    public override void Resumed(IBotWilful bot) => _paused = false;

    public override void Paused(IBotWilful bot)
    {
        _paused = true;

        if (_post != null)
        {
            _worth += _post.Worth;
            BotRagmen.Close(_post, "the ragman was called away", null);
            _post = null;
        }
    }

    public override void Drop(IBotWilful bot)
    {
        var body = bot?.Self as BotMobile;

        // Killed holding the post, or on the way to it; a death while the post was put down for something else is that work's.
        if (body is { Alive: false } && !_paused)
        {
            BotRagmen.Killed(_post != null, body, _post);
        }

        if (_post != null)
        {
            BotRagmen.Close(_post, body is { Alive: false } ? "the ragman was killed" : "the shift was given up", null);
            _post = null;
        }
    }
}

/// <summary>
/// Offers a trader with coin to spend a buying post by the busiest dungeon mouth within its reach, on safe ground (07.10.2026, life18b).
/// Every gate counted.
/// </summary>
public sealed class BotRagman : IBotProposer
{
    public string Name => "Ragman";

    public BotStanding Rung => BotStanding.Free;

    public static long Looks { get; private set; }

    public static long Rested { get; private set; }

    public static long Full { get; private set; }

    public static long Poor { get; private set; }

    public static long Hauling { get; private set; }

    public static long NoMouth { get; private set; }

    public static long NoGround { get; private set; }

    public static long Offered { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    internal static void Went(Mobile trader)
    {
        if (trader != null)
        {
            _last[trader.Serial] = Core.TickCount;
        }
    }

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotRagmen.On || bot?.Self is not BotMobile body || !body.Alive || body.Tired || body.Squad != null || !BotTrading.Trader(body))
        {
            return null;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal || body.Resolve?.Deed is BotKeepRagPost || body.Kills >= 5 || body.Criminal)
        {
            return null;
        }

        Looks++;

        var now = Core.TickCount;

        if (_last.TryGetValue(body.Serial, out var when) && now - when < BotRagmen.RestMs)
        {
            Rested++;

            return null;
        }

        if (BotRagmen.Open.Count >= BotRagmen.MostOpen)
        {
            Full++;

            return null;
        }

        // What it bought last time goes to a counter first.
        if (BotRagmen.StockOf(body) is { Count: > 0 })
        {
            Hauling++;

            return null;
        }

        if (BotYield.Spendable(body) < BotRagmen.LeastPurse)
        {
            Poor++;

            return null;
        }

        var mouth = BotRagmen.Busiest(body, out var dungeon, out var hunters);

        if (mouth == null)
        {
            NoMouth++;

            return null;
        }

        var stand = BotRagmen.Post(map, mouth.From);

        if (stand == Point3D.Zero || !BotGates.Joined(map, body.Location, stand))
        {
            NoGround++;

            return null;
        }

        Offered++;

        return new BotKeepRagPost(map, stand, mouth.From, dungeon, BotRagmen.Prior * Math.Min(2.0, hunters / 4.0 + 0.5));
    }

    public static string Describe() =>
        Looks == 0
            ? "no trader looked at for a buying post"
            : $"{Looks} traders looked at for a buying post: {Offered} offered, {Rested} were resting from one, {Full} found {BotRagmen.MostOpen} open, {Hauling} still carried the last one's goods, "
              + $"{Poor} had under {BotRagmen.LeastPurse}gp to spend, {NoMouth} found no mouth within {BotRagmen.KeeperReach} tiles with {BotRagmen.LeastTraffic}+ hunters about it and no post, {NoGround} found no safe ground by it";

    public static void Forget()
    {
        _last.Clear();
        Looks = 0;
        Rested = 0;
        Full = 0;
        Poor = 0;
        Hauling = 0;
        NoMouth = 0;
        NoGround = 0;
        Offered = 0;
    }
}

/// <summary>
/// A hunter's walk to a ragman's post with its loot: each kind the ragman can pay for, sold hand to hand at <see cref="BotRagmen.Share"/> of
/// the nearest counter's price (07.10.2026, life18b). Measured as coin.
/// </summary>
public sealed class BotRagSale : BotDeed
{
    public const string Trade = "ragman sale";

    private readonly BotRagPost _post;

    private readonly List<(Type Kind, int Units, int Price, int Counter)> _lots;

    private readonly double _claim;

    private long _began;

    public BotRagSale(BotRagPost post, List<(Type Kind, int Units, int Price, int Counter)> lots, double claim)
    {
        _post = post;
        _lots = lots;
        _claim = claim;
    }

    public override string Kind => Trade;

    public override Map Map => _post?.Map;

    public override Point3D Where => _post?.Stand ?? Point3D.Zero;

    public override double Expects => _claim;

    public override double Minutes => 1.0;

    public override double Coin => 1.0;

    public override bool AtCounter => true;

    public override bool Steadfast => true;

    public override string Stage => $"taking its loot to {_post?.Name}";

    public override bool Repeats(BotDeed other) => other is BotRagSale sale && ReferenceEquals(sale._post, _post);

    public override void Taken(IBotWilful bot)
    {
        _began = Core.TickCount;

        if (bot?.Self is { } body)
        {
            BotRagSeller.Went(body);
        }
    }

    public override void Resumed(IBotWilful bot) => _began = Core.TickCount;

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || !body.Alive || _post == null || body.Map != _post.Map)
        {
            return BotDoing.Failed("no body, or no post on this map");
        }

        var keeper = _post.Keeper;

        if (_post.Shut || keeper is not { Deleted: false, Alive: true })
        {
            BotRagSeller.NoteShut();

            return BotDoing.Failed($"{_post.Name} shut before it got there");
        }

        if (!body.InRange(keeper, BotHandover.MeetTiles))
        {
            if (Core.TickCount - _began >= BotRagSeller.WalkMs)
            {
                BotRagSeller.NoteNeverGot();

                return BotDoing.Failed($"could not get to {_post.Name} in {BotRagSeller.WalkMs / 60000} minutes");
            }

            return BotDoing.Walk(_post.Map, keeper, BotArrival.Within(BotHandover.MeetTiles), $"to {_post.Name} with its loot");
        }

        var units = 0;
        var gold = 0;
        string refused = null;

        for (var i = 0; i < _lots.Count; i++)
        {
            var (kind, count, price, counter) = _lots[i];
            var sold = BotRagmen.Buy(_post, bot, kind, count, price, counter, out var paid, out var why);

            if (sold <= 0)
            {
                refused ??= why;

                continue;
            }

            units += sold;
            gold += paid;
        }

        if (units <= 0)
        {
            BotRagSeller.NoteUnbought();

            return BotDoing.Failed($"sold nothing at {_post.Name}: {refused ?? "no reason given"}");
        }

        BotRagmen.Sold(_post);

        var fill = new Dictionary<string, string> { ["fee"] = gold.ToString() };

        BotCampTalk.Say(body, "ragman:thanks", null, keeper, fill);

        return BotDoing.Done($"sold {units} things of its loot at {_post.Name} for {gold}gp");
    }
}

/// <summary>
/// Offers a hunter with a laden pack outside a town, whose nearest counter for its loot is far, the ragman's post near it (07.10.2026,
/// life18b). The pack is read at most once a minute for one hunter. Every gate counted.
/// </summary>
public sealed class BotRagSeller : IBotProposer
{
    public string Name => "Ragman's customer";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>How far a hunter may be from a post and be sent to it. A hundred and twenty tiles.</summary>
    public static int Reach { get; set; } = 120;

    /// <summary>What a load must fetch at the counters to be carried to a post. Forty.</summary>
    public static int LeastLoad { get; set; } = 40;

    /// <summary>How much nearer than the nearest counter for the load the post must stand. Sixty tiles: the town is far.</summary>
    public static int FarGain { get; set; } = 60;

    public static int LookMs { get; set; } = 60000;

    public static int EveryMs { get; set; } = 600000;

    public static int WalkMs { get; set; } = 240000;

    public static long Asked { get; private set; }

    public static long Engaged { get; private set; }

    public static long TooSoon { get; private set; }

    public static long None { get; private set; }

    public static long Light { get; private set; }

    public static long Near { get; private set; }

    public static long Offered { get; private set; }

    public static long Walked { get; private set; }

    public static long Shut { get; private set; }

    public static long NeverGot { get; private set; }

    public static long Unbought { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    private static readonly Dictionary<Serial, long> _looked = [];

    private static readonly Dictionary<Type, (Item Sample, int Units)> _loot = [];

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

    internal static void NoteUnbought() => Unbought++;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotRagmen.On || BotRagmen.Open.Count == 0)
        {
            return null;
        }

        if (bot?.Self is not BotMobile who || !who.Alive || who.Map == null || who.Map == Map.Internal || !BotPeddler.Hunter(who))
        {
            return null;
        }

        if (who.Resolve?.Deed is BotRagSale or BotKeepRagPost || BotDungeon.Under(who.Location) || BotTowns.Of(who.Location) != null)
        {
            return null;
        }

        var post = BotRagmen.For(who, out var away);

        if (post == null)
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

        if (_looked.TryGetValue(who.Serial, out var at) && now - at < LookMs)
        {
            return null;
        }

        if (_looked.Count > 1024)
        {
            _looked.Clear();
        }

        _looked[who.Serial] = now;

        // The loot: everything movable and unbound off its keep list that no want within a carrier's reach waits for (BotPeddler's reading).
        _loot.Clear();

        var keep = BotUnload.Keeps(bot);
        var items = who.Backpack?.Items;

        if (items == null)
        {
            return null;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item == null || item.Deleted || !item.Movable || item is Gold || !item.IsStandardLoot() || !BotShopkeep.Merchandise(item, bot.Bond))
            {
                continue;
            }

            var kind = item.GetType();

            if (keep.ContainsKey(kind) || BotCourier.Deliverable(bot, kind))
            {
                continue;
            }

            _loot[kind] = _loot.TryGetValue(kind, out var had) ? (had.Sample, had.Units + Math.Max(1, item.Amount)) : (item, Math.Max(1, item.Amount));
        }

        var purse = BotRagmen.Purse(post);
        var nearest = int.MaxValue;
        var worth = 0;
        List<(Type Kind, int Units, int Price, int Counter)> lots = [];

        foreach (var (kind, (sample, units)) in _loot)
        {
            var vendor = BotShops.Buyer((Mobile)who, sample, out var counter);

            if (vendor == null || counter <= 0)
            {
                continue;
            }

            var price = Math.Max(1, (int)Math.Floor(counter * BotRagmen.Share));
            var take = Math.Min(units, purse / price);

            if (take <= 0)
            {
                continue;
            }

            purse -= take * price;
            worth += take * counter;
            lots.Add((kind, take, price, counter));
            nearest = Math.Min(nearest, Math.Max(Math.Abs(vendor.X - who.X), Math.Abs(vendor.Y - who.Y)));
        }

        if (lots.Count == 0 || worth < LeastLoad)
        {
            Light++;

            return null;
        }

        if (nearest - away < FarGain)
        {
            Near++;

            return null;
        }

        var paid = 0;

        for (var i = 0; i < lots.Count; i++)
        {
            paid += lots[i].Units * lots[i].Price;
        }

        Offered++;

        return new BotRagSale(post, lots, paid);
    }

    public static string Describe() =>
        Asked == 0
            ? "no hunter looked at for a ragman"
            : $"{Asked} hunters within {Reach} tiles of a post looked at: {Offered} sent, {Walked} went; {Engaged} were in a fight, a company or a delve, {TooSoon} within {EveryMs / 60000} minutes of their last, "
              + $"{Light} carried under {LeastLoad}gp the post could pay for, {Near} had a counter within {FarGain} tiles further than the post; {Shut} found it shut, {NeverGot} could not get there, {Unbought} sold nothing";

    public static void Forget()
    {
        _last.Clear();
        _looked.Clear();
        Asked = 0;
        Engaged = 0;
        TooSoon = 0;
        None = 0;
        Light = 0;
        Near = 0;
        Offered = 0;
        Walked = 0;
        Shut = 0;
        NeverGot = 0;
        Unbought = 0;
    }
}

/// <summary>
/// Offers a ragman whose post has shut the goods it bought, a kind at a time, to the counter that pays most for them once the road is paid
/// for: the peddle (<see cref="BotPeddle"/>), as a fighter's loot goes (07.10.2026, life18b). Runs with the posts switched off too, so what
/// was bought is never stranded.
/// </summary>
public sealed class BotRagHauler : IBotProposer
{
    public string Name => "Ragman's haul";

    public BotStanding Rung => BotStanding.Free;

    public static int LookMs { get; set; } = 30000;

    private static readonly Dictionary<Serial, long> _looked = [];

    private static readonly List<Type> _kinds = [];

    public BotDeed Propose(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || !body.Alive || body.Map == null || body.Map == Map.Internal || body.Resolve?.Deed is BotKeepRagPost or BotPeddle)
        {
            return null;
        }

        var stock = BotRagmen.StockOf(body);

        if (stock == null || stock.Count == 0)
        {
            return null;
        }

        var now = Core.TickCount;

        if (_looked.TryGetValue(body.Serial, out var at) && now - at < LookMs)
        {
            return null;
        }

        _looked[body.Serial] = now;

        var pack = body.Backpack;

        if (pack == null)
        {
            BotRagmen.Cleared(body);

            return null;
        }

        BotShops.Survey(body.Map, body.Location);

        BotPeddle best = null;
        var bestWorth = 0;

        _kinds.Clear();
        _kinds.AddRange(stock.Keys);

        for (var i = 0; i < _kinds.Count; i++)
        {
            var kind = _kinds[i];
            var held = Math.Min(stock[kind], pack.GetAmount(kind, true));

            if (held <= 0)
            {
                // Sold, banked or lost since: nothing of this kind left to carry.
                stock.Remove(kind);

                continue;
            }

            var sample = pack.FindItemByType(kind);

            if (sample == null)
            {
                stock.Remove(kind);

                continue;
            }

            var shop = BotShops.Buyer(bot, sample, held, false, out var price, out var factor, out _);

            if (shop == null || price <= 0)
            {
                // No counter in reach buys it: left to the porter and the peddler like any other goods in the pack.
                stock.Remove(kind);

                continue;
            }

            var worth = (int)(held * price * factor);

            if (worth > bestWorth)
            {
                bestWorth = worth;
                best = new BotPeddle(shop, kind, BotMarket.Label(sample), held, price, false, factor) { Most = held };
                stock[kind] = held;
            }
        }

        if (stock.Count == 0)
        {
            BotRagmen.Cleared(body);
        }

        if (best != null)
        {
            BotRagmen.Hauled(bestWorth);
        }

        return best;
    }

    public static void Forget() => _looked.Clear();
}
