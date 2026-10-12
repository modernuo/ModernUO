using System;
using System.Collections.Generic;
using Server.Engines.Harvest;
using Server.Items;
using Server.Logging;
using Server.Targeting;

namespace Server.BotAI.V2;

/// <summary>
/// Ore: what is in a hill, whether this bot can get it out, how it is dug, and what it becomes.
///
/// <para>
/// <b>The work is the engine's, not ours.</b> Digging goes through the shard's own
/// <see cref="HarvestSystem"/> — the same call a player's double-click makes — so the swing, the skill
/// check, the yield and the seam running dry are the real ones. A bot mining is a miner, not a bot being
/// credited with ore. That also makes the thing this project measures real: skill gain here is the
/// engine raising a number, not us deciding a number should go up.
/// </para>
/// </summary>
public static class BotOre
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotOre));

    /// <summary>How far a bot will look around itself for something worth swinging at.</summary>
    public static int Reach { get; set; } = 12;

    /// <summary>
    /// How near the worked tile the bot has to stand, <b>asked of the engine rather than assumed</b>.
    ///
    /// It is <c>MaxRange</c> on the ore definition — the same number <c>HarvestSystem.CheckRange</c> compares
    /// against — so this cannot drift out of step with the shard the way a copy would. It is deliberately not
    /// configurable: a settings file able to disagree with the engine about reach is a settings file able to
    /// make every swing fail silently.
    /// </summary>
    public static int SwingReach => Mining.System?.OreAndStone?.MaxRange ?? 2;

    /// <summary>How near a forge the ore has to be to go into it.</summary>
    public static int FireReach { get; set; } = 2;

    /// <summary>
    /// Enough ore to be worth carrying to a fire.
    ///
    /// Smelting needs no tool, no skill and no trade — the fire does the work — so this is not a smith's
    /// number, it is a miner's. Four, as in the first version.
    /// </summary>
    public static int WorthSmelting { get; set; } = 4;

    /// <summary>
    /// What a patch of ground actually holds, or null where a pick would find nothing.
    ///
    /// <para>
    /// <b>The fact the whole trade rests on, and it is the shard's own.</b> Ore is not the same
    /// everywhere: <c>HarvestDefinition.GetVeinAt</c> seeds a stable draw off the bank's coordinates, so
    /// every eight-by-eight block of mountain has a fixed kind for the life of the shard, and iron is only
    /// about half of them. The rest is dull copper, shadow iron, copper, bronze, gold, agapite, verite and
    /// valorite, sitting in hills where nobody has looked.
    /// </para>
    ///
    /// <para>
    /// The first version's miners only ever came home with iron, and this is the reason: they dug whatever
    /// rock was nearest, and rock near a town is as likely as any other to be plain iron. The shard will
    /// say what is in a hill if it is asked properly — <b>bank coordinates, not tile coordinates</b>.
    /// <c>GetVeinAt</c> divides before it asks, so passing the tile puts the question to a bank
    /// sixty-four times too far away, which reads as ore scattered at random instead of ore in seams.
    /// </para>
    /// </summary>
    public static HarvestResource VeinAt(Map map, int x, int y)
    {
        var definition = Mining.System?.OreAndStone;

        if (definition == null || map == null || map == Map.Internal)
        {
            return null;
        }

        return definition
            .GetVeinAt(map, x / definition.BankWidth, y / definition.BankHeight)
            ?.PrimaryResource;
    }

    /// <summary>
    /// How much ore is left in the block this tile belongs to.
    ///
    /// <para>
    /// <b>The engine's own count, and it settles a question that would otherwise be guesswork.</b> A depleted
    /// vein looks exactly like a full one to every test that can be made from outside: what is readable from
    /// the tile is its definition, and depletion lives in the engine's bank of resources. Without this a bot
    /// can only notice an empty rock by swinging at it and getting nothing, six times, and it still cannot
    /// tell a spent seam from a run of failed rolls.
    /// </para>
    ///
    /// <para>
    /// Tile coordinates here, unlike <see cref="VeinAt"/>: <c>GetBank</c> divides them itself. The two take
    /// different coordinates and both are right, which is exactly the sort of thing that produces ore
    /// scattered at random instead of ore in seams.
    /// </para>
    ///
    /// <para>
    /// Asking creates the block's record if it does not exist yet, so it belongs where a bot is actually
    /// working and not in a sweep of a whole region.
    /// </para>
    /// </summary>
    /// <summary>
    /// Rock tiles the last <see cref="Find"/> looked at, and how many of them the engine said were worked out. An instrument
    /// for the one question <c>Find</c>'s null cannot answer: was there no rock, or was every rock empty? See BotDig.
    /// </summary>
    public static int LastRocks { get; private set; }

    /// <summary>Of <see cref="LastRocks"/>, those whose block the engine had emptied.</summary>
    public static int LastEmpty { get; private set; }

    public static int Left(Map map, int x, int y)
    {
        if (map == null || map == Map.Internal)
        {
            return 0;
        }

        return Mining.System?.OreAndStone?.GetBank(map, x, y)?.Current ?? 0;
    }

    /// <summary>
    /// Whether any rock within <see cref="Reach"/> of a seam's middle still holds ore by the engine's count, or there is no
    /// rock there at all — the question <c>BotDig</c> answers on arrival before it rests a seam as worked out, asked from the
    /// same middle, over the same rings and on the same leash, so that the choice refuses nothing the trip would not.
    ///
    /// <para>
    /// No rock at all answers true: that is a barren seam, a different verdict with its own line, and not this question's
    /// to give. Outwards in rings and stopping at the first rock with ore in it, so the common answer costs a handful of
    /// tiles. Asking creates the engine's record for each block it touches (see <see cref="Left"/>), which is why it is
    /// asked of a chosen seam only and never of the whole board.
    /// </para>
    /// </summary>
    /// <summary>The least ore a rock must still hold for its seam to count as stocked. Four: two good swings.</summary>
    public static int StockedLeast { get; set; } = 4;

    public static bool Stocked(Map map, Point3D seam)
    {
        var system = Mining.System;

        if (system == null || map == null || map == Map.Internal)
        {
            return true;
        }

        var rocks = 0;

        for (var radius = 1; radius <= Reach; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                    {
                        continue;
                    }

                    var x = seam.X + dx;
                    var y = seam.Y + dy;

                    if (Examine(map, x, y, system) == null)
                    {
                        continue;
                    }

                    // <b>A rock with a swing or two left in it is not worth the walk.</b> Between 05:11 and 05:39 on
                    // 24.09.2026 seven trips ended "emptied 1 of 1 rocks and found no more" after one or two swings, four
                    // of them with nothing at all: seams of one rock with a bank all but spent, chosen because any ore
                    // at all counted as stocked. See StockedLeast.
                    if (Left(map, x, y) >= StockedLeast)
                    {
                        return true;
                    }

                    rocks++;
                }
            }
        }

        return rocks == 0;
    }

    /// <summary>
    /// The longest the engine may take to put ore back into a bank, in milliseconds.
    ///
    /// <para>
    /// <b>Two numbers on one shelf that had never met.</b> <c>HarvestBank.Consume</c> sets the refill time
    /// the first time a full bank is touched, at <c>MinRespawn + rnd * (MaxRespawn - MinRespawn)</c> — ten to
    /// twenty minutes for ore in this era — and refills the whole bank at once. Our own
    /// <c>BotGround.DrainedMs</c> rested a worked-out seam for ten, a number chosen here for sounding right.
    /// So a seam came back onto the board at the earliest moment the engine could possibly have refilled it
    /// and, most of the time, before it had: the next miner walked out, swung at eight barren rocks and
    /// filed the seam as worked out again. On the morning of 09.09.2026 that was 112 of 122 mining failures
    /// in one hour, and mining finished 7% of what it took on.
    /// </para>
    ///
    /// <para>
    /// Asked of the engine rather than copied from it, for the reason this whole assembly gives everywhere
    /// else: a number written down twice is a number that drifts, and a shard configured for a different era
    /// would move one copy and not the other.
    /// </para>
    /// </summary>
    public static int RespawnMs =>
        (int)(Mining.System?.OreAndStone?.MaxRespawn ?? System.TimeSpan.FromMinutes(20.0)).TotalMilliseconds;

    /// <summary>
    /// What the engine gives this bot for one swing at this tile, between nought and one.
    ///
    /// <para>
    /// <b>The number every backstop in mining was implicitly guessing at.</b> <c>HarvestSystem.Harvest</c>
    /// rolls <c>Mobile.CheckSkill(def.Skill, resource.MinSkill, resource.MaxSkill)</c>, and the engine's own
    /// chance for that is <c>(value - min) / (max - min)</c>. For iron that is nought to a hundred, so a bot
    /// with thirty mining misses seven swings in ten — which is not bad luck, it is the ordinary working life
    /// of a novice miner, and the code that decided a rock was empty after six quiet swings was reading it as
    /// an empty rock.
    /// </para>
    ///
    /// <para>
    /// The resource asked about is the one <c>MutateResource</c> would actually hand over: below the vein's
    /// requirement the engine quietly falls back to iron, so a green miner in a valorite seam is rolling
    /// against iron's numbers and not against valorite's. Getting that wrong would make this pessimistic in
    /// exactly the place it matters most.
    /// </para>
    /// </summary>
    public static double Chance(Mobile bot, Map map, int x, int y)
    {
        var def = Mining.System?.OreAndStone;

        if (bot == null || def == null || map == null)
        {
            return 1.0;
        }

        var vein = VeinAt(map, x, y);

        if (vein == null)
        {
            return 1.0;
        }

        var skill = bot.Skills[def.Skill].Base;

        // The engine's own fallback: below the vein's requirement it hands over iron instead, and the roll
        // is made against iron. See HarvestSystem.MutateResource.
        var resource = skill < vein.ReqSkill || skill < vein.MinSkill
            ? def.Resources is { Length: > 0 } ? def.Resources[0] : vein
            : vein;

        var span = resource.MaxSkill - resource.MinSkill;

        if (span <= 0.0)
        {
            return skill >= resource.MinSkill ? 1.0 : 0.0;
        }

        return System.Math.Clamp((skill - resource.MinSkill) / span, 0.0, 1.0);
    }

    /// <summary>Iron asks nothing of the miner, which is exactly what makes it common.</summary>
    public static bool IsCommon(HarvestResource vein) => vein == null || vein.ReqSkill <= 0.0;

    /// <summary>
    /// Whether this bot could actually get the good ore out of a seam of this difficulty.
    ///
    /// Below the requirement the engine quietly hands back iron instead, so a green miner in a valorite
    /// seam is a green miner digging iron: the walk was wasted and nothing anywhere would have said so.
    /// </summary>
    public static bool CanWork(Mobile bot, double required) =>
        bot != null && bot.Skills[SkillName.Mining].Value >= required;

    /// <summary>
    /// How much better than iron a vein is to this bot, as a share of iron's worth: nought for iron and for a vein past this
    /// bot's skill (the engine hands it iron), above nought for a metal somebody pays more for than iron, and below nought — down
    /// to <see cref="WorstPremium"/> — for a metal nobody buys. See <see cref="Premium(Mobile, HarvestVein, Type)"/>.
    /// </summary>
    public static double Worth(Mobile bot, Map map, int x, int y) => Premium(bot, VeinOf(map, x, y), null);

    // ---- What a metal is worth to whoever digs it (06.10.2026, money15) ------------------------------------------------------

    /// <summary>
    /// The vein a tile belongs to, as the engine draws it: the same bank-coordinate question as <see cref="VeinAt"/>, with the
    /// vein's own chance of handing over its fallback (iron) instead of its metal.
    /// </summary>
    public static HarvestVein VeinOf(Map map, int x, int y)
    {
        var definition = Mining.System?.OreAndStone;

        if (definition == null || map == null || map == Map.Internal)
        {
            return null;
        }

        return definition.GetVeinAt(map, x / definition.BankWidth, y / definition.BankHeight);
    }

    /// <summary>
    /// The bars a valuation is taken over: a trip's worth. What open wants ask for is paid in full, the rest at the surplus price
    /// (<see cref="BotGlut.Yield"/>), so the number of bars decides how far a few wants carry. Twenty.
    /// </summary>
    public static int Batch { get; set; } = 20;

    /// <summary>The least a vein may be worth against iron's, as a share: a metal nobody buys is still the iron it hands over half the time.</summary>
    public static double WorstPremium { get; set; } = -0.9;

    /// <summary>How long what the shopkeepers pay for a bar is remembered before it is asked again. Ten minutes: their lists do not move.</summary>
    public static int CounterMs { get; set; } = 600000;

    private static readonly Dictionary<Type, Type> _ingots = [];

    private static readonly Dictionary<Type, (int Price, long Tick)> _counters = [];

    /// <summary>The bar an ore smelts into, by the engine's own resource table; null for anything that is not a metal ore.</summary>
    public static Type IngotOf(Type ore)
    {
        if (ore == null)
        {
            return null;
        }

        if (_ingots.TryGetValue(ore, out var known))
        {
            return known;
        }

        Type ingot = null;
        var resource = CraftResources.GetFromType(ore);

        if (resource != CraftResource.None && CraftResources.GetInfo(resource)?.ResourceTypes is { Length: > 0 } types
            && typeof(BaseIngot).IsAssignableFrom(types[0]))
        {
            ingot = types[0];
        }

        _ingots[ore] = ingot;

        return ingot;
    }

    /// <summary>
    /// The most any known shopkeeper pays for one bar of this metal, asked of the engine's own buy lists with a bar to show
    /// (<see cref="BotShops.Buys"/>), and nought when none buys it. Iron four (<c>SBBlacksmith</c>, <c>SBSmithTools</c>; a vagabond
    /// three); since 06.10.2026 (ingots16) the same smiths buy coloured bars, three for dull copper to bronze and two for gold to
    /// valorite (<c>ColouredIngots</c>). What the counters' hourly purse lets the bots sell is <see cref="CounterRoom"/>, not this.
    /// Remembered for <see cref="CounterMs"/>.
    /// </summary>
    public static int CounterPays(Type ingot)
    {
        if (ingot == null)
        {
            return 0;
        }

        var now = Core.TickCount;

        if (_counters.TryGetValue(ingot, out var known) && now - known.Tick < CounterMs)
        {
            return known.Price;
        }

        var best = 0;
        var shops = BotShops.Shops;

        // Not remembered before any shopkeeper is known: a young world's nought would stand for ten minutes.
        if (shops.Count == 0)
        {
            return 0;
        }

        var sample = ingot.CreateInstance<Item>();

        if (sample != null)
        {

            for (var i = 0; i < shops.Count; i++)
            {
                if (BotShops.Buys(shops[i], sample, out var price) && price > best)
                {
                    best = price;
                }
            }

            sample.Delete();
        }

        _counters[ingot] = (best, now);

        return best;
    }

    /// <summary>
    /// What one bar of a metal is worth to the bot that digs it, on the evidence: the better of what a shopkeeper pays for it
    /// (<see cref="CounterPays"/>) and what the bots' own demand pays over a trip's <see cref="Batch"/> — the bars open wants ask
    /// for at the island's price, the rest at the surplus price (<see cref="BotGlut.Yield"/>, the same number the miner's takings
    /// are booked at when the fire gives the bars). One at the least.
    /// </summary>
    public static double IngotWorth(Type ingot)
    {
        if (ingot == null)
        {
            return 1.0;
        }

        var batch = Math.Max(1, Batch);
        var guess = ingot == typeof(IronIngot) ? BotDig.GoldPerIngot : Math.Max(1, BotAnvil.Reckon(ingot));
        var bots = BotGlut.Yield(ingot, batch, guess) / (double)batch;

        return Math.Max(1.0, Math.Max(CounterPays(ingot), bots));
    }

    /// <summary>
    /// How much better than iron a vein is to this bot, as a share of what iron's bars are worth: what the vein gives per bar —
    /// its fallback iron for the engine's <c>ChanceToFallback</c> of the swings, its own metal for the rest — over iron's, less one.
    /// Nought for iron and for a vein past this bot's skill (the engine hands it iron). <paramref name="seen"/>, when given, is a
    /// metal the population has lately dug out of that hillside (<c>BotCommons.Dug</c>); the better of the two is taken.
    ///
    /// <para>
    /// <b>The miner dug what nobody buys (06.10.2026, money15; an-money14 §3.4).</b> Until now a seam was worth its Mining floor
    /// over ten — dull copper 6.5, valorite 9.9 — plus the rank of the richest metal dug there, against iron's nought, so a
    /// skilled miner walked seven to ten times as far for coloured ore. No shopkeeper in this era buys a coloured bar. On 04.10
    /// (build 400) 79.5 % of mining takes were coloured veins, 46 % of the 161,536 bars smelted had no buyer, and at 01:26 on
    /// 06.10 the full bank boxes were full of them (Bronze 1,955 places, Copper 1,827, Shadow 1,706, Dull Copper 1,694, Gold
    /// 1,541; iron 989), while the balance read Copper 333 piled against 0 asked. A metal is now worth what it is paid for: a
    /// coloured vein nobody asks for is the iron it hands over half the time and a bar at the surplus price the other half, so
    /// it ranks below iron at the same distance and above iron twice as far; a metal a smith's want or order asks for ranks
    /// above iron by what the want pays. Never a veto: a miner whose iron is spent still digs.
    /// </para>
    /// </summary>
    public static double Premium(Mobile bot, HarvestVein vein, Type seen)
    {
        var metal = vein?.PrimaryResource;

        if (bot == null || IsCommon(metal) || !CanWork(bot, metal.ReqSkill))
        {
            return 0.0;
        }

        var iron = IngotWorth(typeof(IronIngot));
        var fallback = Math.Clamp(vein.ChanceToFallback, 0.0, 1.0);
        var own = IngotWorth(IngotOf(metal.Types is { Length: > 0 } types ? types[0] : null));

        if (seen != null && seen != typeof(IronIngot))
        {
            own = Math.Max(own, IngotWorth(seen));
        }

        var perBar = fallback * iron + (1.0 - fallback) * own;

        return Math.Max(WorstPremium, perBar / iron - 1.0);
    }

    /// <summary>The same, for a vein named by its tile and a metal remembered as an engine resource rank. See <c>BotGround.Choose</c>.</summary>
    public static double Premium(Mobile bot, Map map, Point3D where, int seenRank)
    {
        Type seen = null;

        if (seenRank > (int)CraftResource.Iron && CraftResources.GetInfo((CraftResource)seenRank)?.ResourceTypes is { Length: > 0 } types
            && typeof(BaseIngot).IsAssignableFrom(types[0]))
        {
            seen = types[0];
        }

        return Premium(bot, VeinOf(map, where.X, where.Y), seen);
    }

    /// <summary>
    /// Seam choices where the old reckoning (the floor over ten and the rank dug) would have sent the miner to a coloured vein that
    /// is now worth less than iron, and another seam was chosen: veins passed for a metal nobody buys. Counted once per choice.
    /// </summary>
    public static long Passed { get; private set; }

    /// <summary>Seams chosen, by what the chosen vein gives this bot: iron (or past its skill), a metal worth more than iron, a metal worth less.</summary>
    public static long ChoseIron { get; private set; }

    public static long ChoseWanted { get; private set; }

    public static long ChoseUnwanted { get; private set; }

    /// <summary>One seam choice made, with its premium and whether the old reckoning would have gone to a vein now marked down. See BotGround.Choose.</summary>
    public static void Chose(double premium, bool passed)
    {
        if (premium > 0.0)
        {
            ChoseWanted++;
        }
        else if (premium < 0.0)
        {
            ChoseUnwanted++;
        }
        else
        {
            ChoseIron++;
        }

        if (passed)
        {
            Passed++;
        }
    }

    private static readonly Dictionary<Type, (long Smelted, long Peddled, long Sold, long Paid)> _bars = [];

    /// <summary>Bars of a metal out of a fire, for the line. Booked where the dig books its takings.</summary>
    public static void Smelted(Type ingot, int units) => Book(ingot, units, 0, 0, 0);

    /// <summary>
    /// Bars of a metal sold to a shopkeeper and the gold the shopkeeper paid for them. Booked by <see cref="BotGlut.Peddled"/>,
    /// where every sale over a counter is counted; a coloured metal's gold is taken off the counters' hourly purse here
    /// (<see cref="ColouredPerHour"/>), whatever route sold it.
    /// </summary>
    public static void Peddled(Type ingot, int units, int gold)
    {
        Book(ingot, 0, units, 0, gold);

        if (Coloured(ingot) && gold > 0)
        {
            Purse();
            _purse -= gold;
            ColouredBars += Math.Max(0, units);
            ColouredGold += gold;
        }
    }

    /// <summary>Bars of a metal sold between bots. Booked by <see cref="BotGlut.Sold"/>, where it is counted.</summary>
    public static void Sold(Type ingot, int units) => Book(ingot, 0, 0, units, 0);

    private static void Book(Type kind, int smelted, int peddled, int sold, int paid)
    {
        if (kind == null || !typeof(BaseIngot).IsAssignableFrom(kind))
        {
            return;
        }

        var (s, p, b, g) = _bars.GetValueOrDefault(kind);
        _bars[kind] = (s + Math.Max(0, smelted), p + Math.Max(0, peddled), b + Math.Max(0, sold), g + Math.Max(0, paid));
    }

    // ---- The counters' purse for coloured bars (06.10.2026, ingots16) -------------------------------------------------------

    /// <summary>
    /// The most gold the shopkeepers pay the bots for coloured bars in an hour, all of them together; nought stops the bots
    /// offering a coloured bar to a counter at all. Ten thousand.
    ///
    /// <para>
    /// <b>A new faucet, metered (06.10.2026, ingots16; Patrick: «можно» to the smiths buying coloured bars cheaply).</b> The bank
    /// boxes held 101,197 coloured bars that morning: dull copper, shadow, copper and bronze 74,547 at three, gold, agapite, verite
    /// and valorite 26,650 at two — 276,941gp, against 453–467k gold in every purse and account and a counter income of about
    /// 37k an hour (130,159gp over the counters 06:12–09:42 on build 405, 73,448 of it iron bars). Paid as fast as the bots can
    /// walk it, that is two-thirds of the island's money again inside a few hours. The purse refills evenly through the hour;
    /// the fires add about 2,500 coloured bars an hour (8,677 smelted 06:12–09:42), about 7k gold at the counter, so at ten
    /// thousand the stock drains slowly while the faucet stays a quarter of the counters' income.
    /// </para>
    /// </summary>
    public static int ColouredPerHour { get; set; } = 10000;

    /// <summary>How long a load offered against the purse holds its gold back from other bots' offers before it is let go.</summary>
    public static int PurseHoldMs { get; set; } = 600000;

    private static double _purse;

    private static long _purseTick;

    private static bool _pursePrimed;

    private static readonly Dictionary<Serial, (int Gold, long Tick)> _held = [];

    /// <summary>Coloured bars sold to a shopkeeper since the load, and the gold they brought (every route; of the per-metal list).</summary>
    public static long ColouredBars { get; private set; }

    public static long ColouredGold { get; private set; }

    /// <summary>Coloured loads not offered because the hour's purse was spent, and loads cut to what was left of it.</summary>
    public static long PurseShut { get; private set; }

    public static long PurseCut { get; private set; }

    /// <summary>A bar of any metal but iron: what the counters' purse meters.</summary>
    public static bool Coloured(Type kind) =>
        kind != null && kind != typeof(IronIngot) && typeof(BaseIngot).IsAssignableFrom(kind);

    /// <summary>What is left of the hour's purse now, after an even refill since it was last read; full at the first read.</summary>
    private static double Purse()
    {
        var most = Math.Max(0, ColouredPerHour);
        var now = Core.TickCount;

        if (!_pursePrimed)
        {
            _pursePrimed = true;
            _purse = most;
            _purseTick = now;

            return _purse;
        }

        var elapsed = now - _purseTick;

        if (elapsed > 0)
        {
            _purse = Math.Min(most, _purse + elapsed * (most / 3600000.0));
            _purseTick = now;
        }

        return _purse;
    }

    /// <summary>
    /// How many bars of this kind at this price a load this bot is offered may hold: what is left of the hour's purse, less what
    /// other bots' loads on their way hold of it (<see cref="Hold"/>), over the price. Anything not a coloured bar is not metered.
    /// </summary>
    public static int CounterRoom(Mobile bot, Type kind, int price)
    {
        if (!Coloured(kind))
        {
            return int.MaxValue;
        }

        if (ColouredPerHour <= 0 || price <= 0 || bot == null)
        {
            return 0;
        }

        var free = Purse();
        var now = Core.TickCount;

        foreach (var (who, (gold, tick)) in _held)
        {
            if (who != bot.Serial && now - tick < PurseHoldMs)
            {
                free -= gold;
            }
        }

        return free < price ? 0 : (int)(free / price);
    }

    /// <summary>A coloured load offered to this bot: its gold is held back from other bots' offers until it sells or PurseHoldMs passes.</summary>
    public static void Hold(Mobile bot, Type kind, int gold, bool cut)
    {
        if (bot == null || !Coloured(kind))
        {
            return;
        }

        if (_held.Count > 512)
        {
            _held.Clear();
        }

        _held[bot.Serial] = (Math.Max(0, gold), Core.TickCount);

        if (cut)
        {
            PurseCut++;
        }
    }

    /// <summary>The load this bot held against the purse has sold (its gold is now off the purse itself) or is gone.</summary>
    public static void Release(Mobile bot)
    {
        if (bot != null)
        {
            _held.Remove(bot.Serial);
        }
    }

    /// <summary>A coloured kind passed over by a look because the hour's purse was spent.</summary>
    public static void Shut() => PurseShut++;

    private static string PurseLine()
    {
        if (ColouredPerHour <= 0)
        {
            return $"coloured bars to a shopkeeper: {ColouredBars} for {ColouredGold}gp since the load; not offered (the hourly purse is off)";
        }

        var held = 0;
        var now = Core.TickCount;

        foreach (var (_, (gold, tick)) in _held)
        {
            if (now - tick < PurseHoldMs)
            {
                held += gold;
            }
        }

        return $"coloured bars to a shopkeeper: {ColouredBars} for {ColouredGold}gp since the load, at most {ColouredPerHour}gp an hour ({Math.Max(0, (int)Purse())}gp of it free now, {held}gp held by loads on their way); {PurseShut} coloured kinds not offered for a spent purse, {PurseCut} loads cut to what was left of it";
    }

    /// <summary>
    /// The ore line, read in <c>Gathering:</c>: seams chosen by what they give, veins passed for a metal nobody buys, and bars by
    /// metal since the world loaded — smelted, sold to a shopkeeper, sold to a bot, and the rest (made into a piece by a smith,
    /// put in a box, or held; it can go below nought where bars smelted before the load are sold) — with each metal's worth now.
    /// </summary>
    public static string Describe()
    {
        var say = new System.Text.StringBuilder();

        say.Append(
            $"ore: {ChoseIron + ChoseWanted + ChoseUnwanted} seams chosen, {ChoseIron} iron (or past the miner's skill), {ChoseWanted} a metal worth more than iron, {ChoseUnwanted} a metal worth less; {Passed} coloured veins passed for a metal nobody buys; bars smelted/to a shopkeeper/to a bot/the rest:"
        );

        var any = false;

        foreach (var (kind, (smelted, peddled, sold, paid)) in _bars)
        {
            var name = kind.Name.EndsWith("Ingot", StringComparison.Ordinal) ? kind.Name[..^5] : kind.Name;

            say.Append(
                $" {name} {smelted}/{peddled}/{sold}/{smelted - peddled - sold} (worth {IngotWorth(kind):F1}gp, the counter pays {CounterPays(kind)}gp, {paid}gp paid),"
            );
            any = true;
        }

        if (!any)
        {
            say.Append(" none yet,");
        }

        say.Length--;
        say.Append("; ").Append(PurseLine());

        return say.ToString();
    }

    public static void Forget()
    {
        Passed = 0;
        ChoseIron = 0;
        ChoseWanted = 0;
        ChoseUnwanted = 0;
        _bars.Clear();
        _counters.Clear();
        ColouredBars = 0;
        ColouredGold = 0;
        PurseShut = 0;
        PurseCut = 0;
        _held.Clear();
        _pursePrimed = false;
    }

    /// <summary>What the seam is called. The ore's own type name is the plainest thing there is.</summary>
    public static string NameOf(HarvestResource vein)
    {
        var types = vein?.Types;

        return types is { Length: > 0 } ? types[0].Name : "rock";
    }

    /// <summary>
    /// The digging tool this bot is carrying, or null.
    ///
    /// <b>The tool decides who mines, not the class name.</b> A gatherer is born with a pickaxe and a
    /// hatchet, both bound and weightless; anybody else who buys or loots a pick is a miner for as long as
    /// it holds one. Asking about archetypes instead is how the first version ended up with a list of
    /// which classes were allowed to work, which then had to be edited every time a class was added.
    /// </summary>
    public static Item Tool(Mobile bot)
    {
        var pack = bot?.Backpack;

        if (pack == null)
        {
            return null;
        }

        // The most worn first, so that a spare bought before the last gives out is not carried for ever. See
        // BotOutfit.SpareAt.
        Item tool = BotOutfit.Oldest<Pickaxe>(pack);

        return tool ?? BotOutfit.Oldest<Shovel>(pack);
    }

    /// <summary>The graphic of the smallest pile of ore, which the engine will not smelt below two ore. See Ore.cs.</summary>
    private const int SmallPile = 0x19B7;

    /// <summary>
    /// Whether the engine's smelting would hand this pile back untouched: a small pile of one ore. The engine's rule
    /// (<c>Ore.cs</c>): a pile with the small graphic and fewer than two ore is "not enough metal-bearing ore" and is
    /// returned as it was; and a failed check turns any pile down to one ore into that pile. So one ore, once failed,
    /// can never be smelted, and until 16.09.2026 it counted as ore worth a trip to the fire and went into it on every
    /// visit — "put 2 piles of ore into the fire and got 0 ingots", 143 times in one evening, three of them in the same
    /// second as the deed took the fire, found the ore still there, and took the fire again. It stacks with the next
    /// small pile dug, and is smelted then.
    /// </summary>
    public static bool TooLittle(BaseOre ore) => ore != null && ore.ItemID == SmallPile && ore.Amount < 2;

    /// <summary>Single ore in small piles a fire would not take, left in the pack at the last melt. For the log.</summary>
    public static long Dust { get; private set; }

    /// <summary>How much unsmelted ore the bot is carrying that a fire would take; single small piles are not it.</summary>
    public static int Carried(Mobile bot)
    {
        var pack = bot?.Backpack;

        if (pack == null)
        {
            return 0;
        }

        var carried = 0;
        var items = pack.Items;

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is BaseOre { Deleted: false } ore && !TooLittle(ore))
            {
                carried += ore.Amount;
            }
        }

        return carried;
    }

    /// <summary>How many ingots the bot is carrying.</summary>
    public static int Ingots(Mobile bot) => bot?.Backpack?.GetAmount(typeof(BaseIngot)) ?? 0;

    /// <summary>
    /// Somewhere within reach worth swinging at, or null. Returns the target the harvest system expects,
    /// so the caller can hand it straight over.
    ///
    /// <para>
    /// Outwards in rings, so the nearest workable thing is found first and anything after it is only taken
    /// if it is better ore. Better ore beats nearer ore <b>within a pick's walk only</b> — anything
    /// further is a journey, and journeys are weighed a level up where the road can be priced.
    /// </para>
    /// </summary>
    /// <param name="skip">
    /// Tiles already found to give nothing. <b>Not optional in practice, and the reason is not obvious:</b>
    /// a depleted vein looks exactly like a full one to every test that can be made from outside. Depletion
    /// lives in the engine's own bank of resources, while what is readable here is the tile's definition —
    /// so a bot that has just emptied a rock and looks again is handed the same rock, for ever. Whoever is
    /// digging has to remember what it has already exhausted.
    /// </param>
    /// <param name="anchor">
    /// The seam this search belongs to, when the caller has one. Together with <paramref name="leash"/> it
    /// keeps the answer inside the patch of ground the trip was taken on.
    /// </param>
    /// <param name="leash">
    /// How far from <paramref name="anchor"/> a tile may lie, or zero for no limit.
    ///
    /// <para>
    /// <b>Without it the search and its caller measure from two different points, and a bot walks on the
    /// spot.</b> This looks outwards from the <em>bot</em>, so it will happily hand back a rock twelve tiles
    /// away; whoever is digging then checks whether the bot is still near the <em>seam</em>, and a rock
    /// twelve tiles the far side of the bot is twenty-four from the seam. One step towards the rock puts the
    /// bot out of the seam's range, the next beat sends it back to the seam, arriving puts the rock in front
    /// of it again, and it paces between the same two tiles for as long as the trip lasts — over ground with
    /// no ore on it, because the ore is at neither end of the pacing.
    /// </para>
    /// </param>
    public static IPoint3D Find(
        Mobile bot,
        out HarvestSystem system,
        List<Point3D> skip = null,
        Point3D anchor = default,
        int leash = 0,
        Point3D from = default
    )
    {
        system = Mining.System;

        var map = bot?.Map;

        if (system == null || map == null || map == Map.Internal)
        {
            return null;
        }

        // Outwards from the bot unless told otherwise. See BotDig, which looks again from the middle of a seam before
        // calling it barren.
        var origin = from == Point3D.Zero ? bot.Location : from;

        IPoint3D nearest = null;
        IPoint3D richest = null;

        // Better than iron only: a metal worth less than iron does not pull the pick past the nearest rock (06.10.2026). See Premium.
        var bestWorth = 0.0;

        LastRocks = 0;
        LastEmpty = 0;

        for (var radius = 1; radius <= Reach; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    // The edge of each ring only; the inside was covered by the ring before it.
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                    {
                        continue;
                    }

                    var x = origin.X + dx;
                    var y = origin.Y + dy;

                    // Outside the seam this trip is being paid for. See the leash note above: a rock the
                    // caller will walk away from as soon as it has walked to it is worse than no rock.
                    if (leash > 0 && (Math.Abs(x - anchor.X) > leash || Math.Abs(y - anchor.Y) > leash))
                    {
                        continue;
                    }

                    if (Skipped(skip, x, y) || BotFooting.Footless(map, x, y))
                    {
                        continue;
                    }

                    var found = Examine(map, x, y, system);

                    if (found == null)
                    {
                        continue;
                    }

                    LastRocks++;

                    // Emptied blocks are skipped outright rather than discovered by swinging. This is the one
                    // place the engine can be asked a question no amount of looking at the ground answers.
                    if (Left(map, x, y) <= 0)
                    {
                        LastEmpty++;

                        continue;
                    }

                    nearest ??= found;

                    var worth = Worth(bot, map, found.X, found.Y);

                    if (worth <= bestWorth)
                    {
                        continue;
                    }

                    richest = found;
                    bestWorth = worth;
                }
            }
        }

        return richest ?? nearest;
    }

    private static bool Skipped(List<Point3D> skip, int x, int y)
    {
        if (skip == null)
        {
            return false;
        }

        for (var i = 0; i < skip.Count; i++)
        {
            if (skip[i].X == x && skip[i].Y == y)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether this tile holds anything a pick can work, as the target the engine wants.</summary>
    public static IPoint3D Examine(Map map, int x, int y, HarvestSystem system)
    {
        if (map == null || system == null || x < 0 || y < 0 || x >= map.Width || y >= map.Height)
        {
            return null;
        }

        // Statics first: most stone worth swinging at is a static.
        foreach (var tile in map.Tiles.GetStaticTiles(x, y))
        {
            if (Workable(system, system.GetDefinition(tile.ID & 0x3FFF, false)))
            {
                return new StaticTarget(new Point3D(x, y, tile.Z), tile.ID);
            }
        }

        var land = map.Tiles.GetLandTile(x, y);

        return Workable(system, system.GetDefinition(land.ID & 0x3FFF, true))
            ? new LandTarget(new Point3D(x, y, map.GetAverageZ(x, y)), map)
            : null;
    }

    /// <summary>
    /// Whether a definition is one an ordinary bot can get anything at all out of.
    ///
    /// <b>Mining knows two kinds of ground and only one of them is mining.</b> Sand is worked by masters —
    /// a hundred points of skill and a flag nobody but a trained sand miner has — and a bot swinging at a
    /// beach gets a message it has no client to read and nothing in its pack. Britain is ringed with sand,
    /// so in the first version both gatherers spent an entire night digging it: the ground looked workable
    /// to every test the bot could make and produced not one ingot in eight hours.
    /// </summary>
    private static bool Workable(HarvestSystem system, HarvestDefinition definition) =>
        definition != null && (system is not Mining mining || definition == mining.OreAndStone);

    /// <summary>
    /// One swing. The engine takes it from here: it rolls against the skill and either produces something
    /// or does not, exactly as it would for a player.
    /// </summary>
    public static bool Swing(Mobile bot, Item tool, HarvestSystem system, IPoint3D target)
    {
        if (bot == null || tool == null || system == null || target == null)
        {
            return false;
        }

        bot.Direction = bot.GetDirectionTo(new Point3D(target.X, target.Y, target.Z));

        system.StartHarvesting(bot, tool, target);

        return true;
    }

    /// <summary>
    /// Puts every pile of ore the bot is carrying into a nearby fire, and says how many ingots came out.
    ///
    /// <para>
    /// <b>The forge has to be pointed at, not merely stood near, and this is why nothing was ever
    /// smelted.</b> Ore is smelted by double-clicking it and then targeting a forge — the double-click
    /// only opens a target and waits for a player to answer. A bot has no client, so nothing ever
    /// answered: every pile of ore ever mined in the first version opened a target that hung there until
    /// something replaced it, and not one ingot was produced. The answer is to supply the target
    /// ourselves, which is exactly what a client does.
    /// </para>
    /// </summary>
    public static int Melt(Mobile bot)
    {
        var pack = bot?.Backpack;

        if (pack == null)
        {
            return 0;
        }

        var forge = Fire(bot, FireReach);

        if (forge == null)
        {
            return 0;
        }

        var before = Ingots(bot);

        // A snapshot: smelting replaces the ore, which mutates the list being read.
        List<Item> carried = [.. pack.Items];
        var piles = 0;
        var dust = 0;

        for (var i = 0; i < carried.Count; i++)
        {
            if (carried[i] is not BaseOre ore || ore.Deleted)
            {
                continue;
            }

            // The engine would hand it straight back; see TooLittle.
            if (TooLittle(ore))
            {
                dust++;

                continue;
            }

            ore.OnDoubleClick(bot);

            var target = bot.Target;

            if (target == null)
            {
                continue;
            }

            target.Invoke(bot, forge);
            piles++;
        }

        var made = Ingots(bot) - before;
        Dust += dust;

        if (piles > 0)
        {
            logger.Information(
                "{Name} put {Piles} piles of ore into the fire at {Where} and got {Ingots} ingots, {Dust} single ore left as too little to smelt",
                bot.Name,
                piles,
                bot.Location,
                made,
                dust
            );
        }

        return made > 0 ? made : 0;
    }

    /// <summary>
    /// Something to melt ore in, within range, as a target the ore's own smelting will accept.
    ///
    /// Both kinds are looked for, and that distinction is the whole of why the first version forged
    /// nothing: a forge placed by a player is an item and turns up in a spatial query, while every forge
    /// in every town on this shard is a <b>static tile</b>, part of the map, which no query returns. A
    /// smith standing directly in front of the Britain smithy concluded there was no forge in the world.
    /// </summary>
    public static object Fire(Mobile bot, int range)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        foreach (var item in map.GetItemsInRange(bot.Location, range))
        {
            if (BotGround.IsForgeId(item.ItemID))
            {
                return item;
            }
        }

        var origin = bot.Location;

        for (var dx = -range; dx <= range; dx++)
        {
            for (var dy = -range; dy <= range; dy++)
            {
                var x = origin.X + dx;
                var y = origin.Y + dy;

                if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                {
                    continue;
                }

                foreach (var tile in map.Tiles.GetStaticAndMultiTiles(x, y))
                {
                    if (BotGround.IsForgeId(tile.ID))
                    {
                        return new StaticTarget(new Point3D(x, y, tile.Z), tile.ID);
                    }
                }
            }
        }

        return null;
    }
}
