using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// The bots' market with the auction taken out of it: Britain's square by the bank, the wants the bots say aloud, the prices
/// the island has learned from goods that really changed hands, and the one correct way to take a bot's money.
///
/// <para>
/// <b>Patrick's order of 30.09.2026, night: "The auction is removed completely; all trade and all the workshops live in
/// Britain… Trade through talk and the global chat. Every order and every purchase must now be hand to hand."</b> The auction held
/// goods out of the world and money in escrow, and every trade on it happened between two bots that never met: 563 stall sales
/// and 421 fills for 24,482gp in the hour of 16:42–17:42 on 30.09.2026, 806 things crossed from a stall to a want by the
/// market's own beat, 171 deliveries fetched off the board by a reflex, against 20 sales for 526gp at the bots' shops by the
/// banks where both sides stood. None of that placeless half is left. What is here is what trade between two bots needs when
/// they must meet: a place (<see cref="Square"/>), a way to say what one wants (<see cref="Ask"/>, spoken in the world's
/// chat), a price that means something (<see cref="Worth"/>), and money that moves only with the goods
/// (<see cref="BotHandover"/>).
/// </para>
///
/// <para>
/// <b>Where the goods go now.</b> A seller keeps its goods in its own pack and bank box, where the engine and the save keep
/// them. It sells them standing by Britain's bank (<see cref="BotShopkeep"/>, a guild's in its own town), carries them to a
/// bot that has said it wants them (<see cref="BotCourier"/>, <see cref="BotDeliver"/>), or takes them to a shopkeeper
/// (<see cref="BotPeddler"/>). A buyer walks to a shop by the bank (<see cref="BotPatron"/>, <see cref="BotShopper"/>) or
/// says what it wants and waits for a carrier. Every one of those ends with the two standing within
/// <see cref="BotHandover.MeetTiles"/> of each other, goods pack to pack and coin pack to pack.
/// </para>
/// </summary>
public static class BotMarket
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMarket));

    // ---- The dials a price moves by: the auction's own numbers, kept because a shop's price and a want's offer still move ----

    /// <summary>Whether bots say wants aloud at all. Off, nothing is asked for and every want stands down on the next beat.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How much a price goes up when the same goods sell again soon, and how much an unanswered want's offer rises.</summary>
    public static double RaiseStep { get; set; } = 0.15;

    /// <summary>How much a price comes down when nothing sold, and how much a want answered again soon asks for less.</summary>
    public static double CutStep { get; set; } = 0.10;

    /// <summary>How soon after a sale another sale counts as brisk: "again, soon", not "a lot". Ten minutes.</summary>
    public static int BriskMs { get; set; } = 600000;

    /// <summary>How long a want stands unanswered before its offer goes up; the give-up clock at the ceiling too. Ten minutes.</summary>
    public static int StaleMs { get; set; } = 600000;

    /// <summary>How long a want may go unanswered before its offer goes up: <see cref="StaleMs"/> unless dialled apart.</summary>
    public static int RaiseMs
    {
        get => _raiseMs ?? StaleMs;
        set => _raiseMs = value;
    }

    private static int? _raiseMs;

    /// <summary>The most, and the least, a price may become as a multiple of what it opened at.</summary>
    public static double MostMultiple { get; set; } = 4.0;

    public static double LeastMultiple { get; set; } = 0.25;

    /// <summary>
    /// The least a thing is worth offering or showing at. Two: below it a seller is not trading, it is storing — and a shop's
    /// pitch held by bones is a pitch another seller's iron wanted.
    /// </summary>
    public static int Floor { get; set; } = 2;

    /// <summary>
    /// How many wants may stand at once. A backstop against a leak, never a shelf to run out of — and at 512 it became a
    /// shelf on the night of 12.10.2026, when 450 bots held 468 wants and 7 227 asks an hour were turned away (ribs,
    /// leather, feathers, logs), each refusal an ERR line. 1200 since, about two and a half a bot; the refusals are
    /// counted in <see cref="BoardFull"/> and said once a minute.
    /// </summary>
    public static int MaxWants { get; set; } = 1200;

    /// <summary>Asks turned away because the board stood at <see cref="MaxWants"/>. Printed on the Lines: line.</summary>
    public static int BoardFull { get; private set; }

    // Seeded a minute in the past from a real tick, so the first refusal is said at once and no zero stands in for "never".
    private static long _boardFullSaidTick = Core.TickCount - 60000;

    /// <summary>How often the market looks at its wants: offers raised, wants given up, reservations swept.</summary>
    public static int BeatMs { get; set; } = 30000;

    /// <summary>How often a buyer says an open want of its own aloud again in the trade chat. Two minutes.</summary>
    public static int SayWantMs { get; set; } = 120000;

    /// <summary>
    /// How long a buyer back on its feet after a death is given before its wants are asked whether it can still pay: the walk
    /// back to its corpse, where its pocket lies (<see cref="BotReclaim"/>). Five minutes. While it lies dead nothing is asked
    /// at all (02.10.2026, build 391): 69 of 95 stand-downs on build 389 were read off a ghost's empty pack.
    /// </summary>
    public static int RisenGraceMs { get; set; } = 300000;

    /// <summary>
    /// How long a want whose buyer cannot reserve a louder price may stand at the price it has. A hundred and ten minutes: about
    /// what ten raises of 15 % take to reach ×4, the ceiling a want that can pay gives up at. See <see cref="Held"/>.
    /// </summary>
    public static int MostAgeMs { get; set; } = 6600000;

    /// <summary>
    /// How near a banker a bot must stand to reach into its own bank box. Twelve tiles: inside the bank, not on the far side of
    /// the square. A box is reached only where a player could open it.
    /// </summary>
    public static int BankReach { get; set; } = 12;

    // ---- Where the market is ------------------------------------------------------------------------------------------------

    private static readonly Dictionary<Map, (Point3D Bank, long Tick)> _squares = [];

    /// <summary>
    /// Britain's bank: the square every bot trades at unless its guild trades at home. The capital's town (<see cref="BotCapital.Name"/>)
    /// read through its hearth to the banker nearest it, and kept for <see cref="BotShopkeep.BankMemoryMs"/>. <c>Point3D.Zero</c>
    /// before the towns are read or on a map with no Britain.
    /// </summary>
    public static Point3D Square(Map map)
    {
        if (map == null || map == Map.Internal)
        {
            return Point3D.Zero;
        }

        var now = Core.TickCount;

        if (_squares.TryGetValue(map, out var known) && known.Bank != Point3D.Zero && now - known.Tick < BotShopkeep.BankMemoryMs)
        {
            return known.Bank;
        }

        var town = BotTowns.Find(BotCapital.Name);
        var hearth = town == null ? Point3D.Zero : BotSettle.Of(town)?.Hearth ?? town.Square;
        var bank = hearth == Point3D.Zero ? Point3D.Zero : BotShopkeep.BankNear(map, hearth);

        _squares[map] = (bank, now);

        return bank;
    }

    /// <summary>Whether this place stands in Britain, the market's town: its workshops are the island's workshops.</summary>
    public static bool InBritain(Point3D where)
    {
        var town = BotTowns.Of(where);

        return town != null && string.Equals(town.Name, BotCapital.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether a bot stands close enough to a banker to reach into its own bank box. The one honest way a bot takes its own goods
    /// out of the box: standing in the bank, as a player must.
    /// </summary>
    public static bool AtBank(Mobile body)
    {
        var map = body?.Map;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        // Asked of every bot on every review while any want stands (the carrier's look at its box): the answer is kept while the
        // bot has not moved, for AtBankMs, so a bot standing still costs a dictionary lookup and not a spatial query.
        var now = Core.TickCount;

        if (_atBank.TryGetValue(body.Serial, out var known) && known.Where == body.Location && known.Map == map
            && now - known.Tick < AtBankMs)
        {
            return known.At;
        }

        var at = false;

        foreach (var banker in map.GetMobilesInRange<Banker>(body.Location, BankReach))
        {
            if (banker is { Deleted: false })
            {
                at = true;

                break;
            }
        }

        if (_atBank.Count > 2048)
        {
            _atBank.Clear();
        }

        _atBank[body.Serial] = (map, body.Location, at, now);

        return at;
    }

    /// <summary>How long a bot's being at a bank, or not, is kept while it has not moved.</summary>
    public static int AtBankMs { get; set; } = 10000;

    private static readonly Dictionary<Serial, (Map Map, Point3D Where, bool At, long Tick)> _atBank = [];

    /// <summary>Things taken out of bank boxes by their owners standing at a bank. See <see cref="FromBank"/>.</summary>
    public static long BoxTaken { get; private set; }

    /// <summary>
    /// Up to <paramref name="units"/> of a kind out of the bot's own bank box into its pack, when it stands at a bank. The honest
    /// replacement for taking one's own goods back off a stall from anywhere: the box is reached only in a bank. Returns how many
    /// moved.
    /// </summary>
    public static int FromBank(IBotWilful bot, Type kind, int units)
    {
        var body = bot?.Self;
        var pack = body?.Backpack;
        var box = body?.FindBankNoCreate();

        if (pack == null || box == null || kind == null || units <= 0 || box.Items.Count == 0 || !AtBank(body))
        {
            return 0;
        }

        var moved = 0;

        for (var i = box.Items.Count - 1; i >= 0 && moved < units; i--)
        {
            var item = box.Items[i];

            if (item?.GetType() != kind || !BotShopkeep.Merchandise(item, bot.Bond))
            {
                continue;
            }

            var part = Math.Min(units - moved, Math.Max(1, item.Amount));

            if (part < Math.Max(1, item.Amount) && (!item.Stackable || Mobile.LiftItemDupe(item, part) == null))
            {
                continue;
            }

            var got = Math.Max(1, item.Amount);

            if (!pack.TryDropItem(body, item, false))
            {
                break;
            }

            moved += got;
        }

        BoxTaken += moved;

        return moved;
    }

    /// <summary>
    /// The island's stock of a kind as a maker choosing what to make should read it, since the stalls went (01.10.2026): what the
    /// shops by the bank show (<see cref="BotShopkeep.Showing"/>), and what this maker already holds in its own pack and bank box —
    /// its unsold work, waiting for a shift or a carrier's walk. Under the auction a maker read the stalls, its own included.
    /// </summary>
    public static int Stock(Mobile maker, Type kind)
    {
        if (kind == null)
        {
            return 0;
        }

        var box = maker?.FindBankNoCreate();

        return BotShopkeep.Showing(kind) + (maker?.Backpack?.GetAmount(kind) ?? 0) + (box?.GetAmount(kind) ?? 0);
    }

    // ---- What a thing is worth ----------------------------------------------------------------------------------------------

    private static readonly HashSet<Type> _worthless = [];

    private static readonly HashSet<Type> _staples = [];

    /// <summary>What goods that really changed hands between bots came to, by kind: the island's own price.</summary>
    private static readonly Dictionary<Type, (long Units, long Gold)> _dealt = [];

    /// <summary>Things refused as worth less than the floor, and of those the ones nothing could price at all.</summary>
    public static long Cheap { get; private set; }

    public static long Unpriced { get; private set; }

    /// <summary>Whether this kind has been measured as worth less than <see cref="Floor"/>. Cleared the moment anybody wants one.</summary>
    public static bool Worthless(Type kind) => kind != null && _worthless.Contains(kind);

    /// <summary>
    /// A kind measured under the floor: remembered for the whole island, so the fifteenth bot carrying one does not walk to find
    /// out. Only a <paramref name="measured"/> price may condemn a kind; a guess is counted and teaches nothing.
    /// </summary>
    public static void Condemn(Type kind, bool measured)
    {
        if (kind == null)
        {
            return;
        }

        Cheap++;

        if (!measured)
        {
            Unpriced++;

            return;
        }

        if (!_staples.Contains(kind))
        {
            _worthless.Add(kind);
        }
    }

    /// <summary>A kind a trade eats as its input may go at a gold: feathers are a gold at every counter and fifty are worth a walk.</summary>
    public static void Staple(Type kind)
    {
        if (kind != null)
        {
            _staples.Add(kind);
            _worthless.Remove(kind);
        }
    }

    /// <summary>The least a thing of this kind may be shown or offered at: a gold for a staple, <see cref="Floor"/> for the rest.</summary>
    public static int FloorFor(Type kind) => kind != null && _staples.Contains(kind) ? 1 : Floor;

    /// <summary>The condemned kinds, names only, for the summary.</summary>
    public static string Condemned(int most)
    {
        if (_worthless.Count == 0)
        {
            return "none";
        }

        List<string> named = [];

        foreach (var kind in _worthless)
        {
            if (named.Count >= most)
            {
                named.Add("and more");

                break;
            }

            named.Add(kind.Name);
        }

        return string.Join(", ", named);
    }

    /// <summary>Goods that changed hands between bots, booked by kind: a shop's till, a delivery, the city at a shop.</summary>
    public static void Traded(Type kind, int units, int gold)
    {
        if (kind == null || units <= 0 || gold <= 0)
        {
            return;
        }

        var (u, g) = _dealt.GetValueOrDefault(kind);

        // Halved past a thousand units, so a price learned in the first hour does not outweigh the fifth hour for ever.
        if (u + units > 1000)
        {
            u /= 2;
            g /= 2;
        }

        _dealt[kind] = (u + units, g + gold);

        // The balance reads what sold between bots against what was made (01.10.2026, Patrick's point 8). See BotGlut.
        BotGlut.Sold(kind, units);
    }

    /// <summary>Kinds the island has a traded price for.</summary>
    public static int Priced => _dealt.Count;

    /// <summary>
    /// What the shard reckons one of these is worth, and the number a producer counts its output at and a buyer opens its want
    /// at: what one has really changed hands for between two bots, else the caller's own estimate — a shopkeeper's price, what
    /// the thing costs to make, or a stand-in.
    ///
    /// <para>
    /// The auction's reckoning with its middle term changed: a stall's sales are gone, and what a bot actually paid another bot
    /// standing beside it is the better fact anyway. A shortage still raises the price — a want nobody answers raises its own
    /// offer, and what it is finally met at is learned here — the price still raises the measure of the work, and the ledger
    /// still moves labour by it.
    /// </para>
    ///
    /// <para>
    /// <b>Never another buyer's standing offer (03.10.2026, build next; Patrick: "a new want starts from the ceiling of the last
    /// and climbs ×4 again. Fix?" — "We fix it.").</b> This read the dearest offer anybody stood at (<see cref="Best"/>) first, up to
    /// ×4 of the caller's estimate — and that offer was usually a want that had climbed ×4 unanswered. So a want that gave up at
    /// its ceiling was said again at that ceiling and climbed ×4 again: Baldwin's chain legs, 216 → gave up at 864 → said again at
    /// 864 → 2,636gp; on build 396 (04:19–12:47) every one of 26 chain-legs wants opened at 864 and climbed to 3,456, 14 of them a
    /// re-ask of a want just given up; leather chests opened at 384 (×4 of their 96), harm scrolls at 88 (×4 of the mage's 22).
    /// The same bids set the materials' ceilings ("Leather 130gp (LeatherBustierArms at 388)", "IronIngot 84gp (PlateGorget at
    /// 416)"). An unmet offer is a sentence, not a sale; what it is met at, if it is met, is learned in <see cref="Traded"/>.
    /// </para>
    /// </summary>
    public static int Worth(Type kind, int fallback)
    {
        // Unbounded as it always was when no offer stood: the loot, peddle and quest reckonings ask with a stand-in of one and
        // mean "what it sold for". What a sale can come to is bounded where it is made — a want's climb (BotWant.Highest) and a
        // trader's carriage (BotTrading.Quote).
        if (kind != null && _dealt.TryGetValue(kind, out var dealt) && dealt.Units > 0)
        {
            return (int)Math.Max(1, dealt.Gold / dealt.Units);
        }

        return fallback;
    }

    // ---- What a material may cost: twice what it becomes (02.10.2026, build next) ----------------------------------------------

    /// <summary>
    /// <b>Patrick, 02.10.2026: "a raw material is never worth more than twice what it becomes."</b> Whether wants for a material
    /// and traders' prices for it stop at <see cref="Ceiling(Type)"/>. Off, no material has a ceiling and wants climb to
    /// <see cref="MostMultiple"/> as before.
    /// </summary>
    public static bool Ceilings { get; set; } = true;

    /// <summary>How many times the worth of what it becomes a material may cost, per unit of it. Two, by Patrick's word.</summary>
    public static double MaterialShare { get; set; } = 2.0;

    /// <summary>How long a material's ceiling is kept before the recipes and the prices of their products are read again.</summary>
    public static int CeilingMs { get; set; } = 300000;

    /// <summary>
    /// The craft systems a bot works in, whose recipes say what a material becomes. The same list the dashboard's chain page reads
    /// (<c>BotWebCraft</c>), so the ceiling is read off the chains the bots can actually walk.
    /// </summary>
    private static readonly Func<Server.Engines.Craft.CraftSystem>[] _systems =
    [
        () => Server.Engines.Craft.DefBlacksmithy.CraftSystem,
        () => Server.Engines.Craft.DefTailoring.CraftSystem,
        () => Server.Engines.Craft.DefAlchemy.CraftSystem,
        () => Server.Engines.Craft.DefBowFletching.CraftSystem,
        () => Server.Engines.Craft.DefCooking.CraftSystem,
        () => Server.Engines.Craft.DefInscription.CraftSystem,
        () => Server.Engines.Craft.DefTinkering.CraftSystem,
        () => Server.Engines.Craft.DefCarpentry.CraftSystem
    ];

    private static readonly Dictionary<Type, (int Ceiling, Type Product, int Worth, long Tick)> _ceilings = [];

    /// <summary>Wants opened at their material's ceiling instead of above it, and the gold over the ceiling they would have asked.</summary>
    public static long CappedAsks { get; private set; }

    public static long CappedGold { get; private set; }

    /// <summary>Raises a want at its material's ceiling did not take; it waited at its price instead (to <see cref="MostAgeMs"/>).</summary>
    public static long CeilingHeld { get; private set; }

    /// <summary>Wants not said because nothing sells or makes the material at or under its ceiling. See <see cref="Sourced"/>.</summary>
    public static long Unsourced { get; private set; }

    /// <summary>
    /// The most one of this kind is worth as a material: <see cref="MaterialShare"/> times the best worth, per unit of it, of anything
    /// a recipe makes out of it — the market's price for the product (<see cref="Worth"/>, which reads the wants and the deals),
    /// else the cheapest counter that sells the product. Nought when it is no material, when its products are priced by nothing,
    /// or for what a bot spends rather than works — bandages, potions, reagents, arrows and bolts (Patrick: wants for what is
    /// actually needed are kept). Kept <see cref="CeilingMs"/>.
    ///
    /// <para>
    /// <b>Measured before it (build 392, 15:26–16:12):</b> raw ribs bought by traders from the butcher at 16gp were 51 % of every
    /// gold coin the bots spent at a counter (10,512 of 20,525gp), resold out of their books at 17.2gp a rib to cooks whose meal
    /// is worth 6gp; ribs handed bot to bot went at 4.5gp. <see cref="MostMultiple"/> let a 5gp want climb to 20, and the trader's
    /// carriage (<see cref="BotTrading.Quote"/>) priced the rest against the butcher.
    /// </para>
    /// </summary>
    public static int Ceiling(Type kind) => Ceiling(kind, out _, out _);

    /// <summary>The same, with the product that set it and that product's worth, for the summary.</summary>
    public static int Ceiling(Type kind, out Type product, out int worth)
    {
        product = null;
        worth = 0;

        if (!Ceilings || kind == null || BotProvision.Consumable(kind))
        {
            return 0;
        }

        var now = Core.TickCount;

        // A material found unpriced is read again sooner: before the counters are surveyed after a start, nothing prices anything.
        if (_ceilings.TryGetValue(kind, out var known) && now - known.Tick < (known.Ceiling > 0 ? CeilingMs : CeilingMs / 10))
        {
            product = known.Product;
            worth = known.Worth;

            return known.Ceiling;
        }

        var best = 0.0;

        for (var s = 0; s < _systems.Length; s++)
        {
            Server.Engines.Craft.CraftSystem system;

            try
            {
                system = _systems[s]();
            }
            catch
            {
                continue;
            }

            var recipes = system?.CraftItems;

            if (recipes == null)
            {
                continue;
            }

            for (var i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                var made = recipe?.ItemType;
                var needs = recipe?.Resources;

                if (made == null || needs == null || made == kind)
                {
                    continue;
                }

                for (var r = 0; r < needs.Count; r++)
                {
                    var res = needs[r];

                    if (res?.ItemType == null || res.Amount <= 0 || res.ItemType != kind && !res.ItemType.IsAssignableFrom(kind))
                    {
                        continue;
                    }

                    var value = Worth(made, BotShops.Lowest(made));

                    if (value <= 0)
                    {
                        break;
                    }

                    // The whole of the product is put down to this one material, so a ceiling is never lower than it should be
                    // for a recipe of two materials: a feather is reckoned at the whole arrow.
                    var per = (double)value / res.Amount;

                    if (per > best)
                    {
                        best = per;
                        product = made;
                        worth = value;
                    }

                    break;
                }
            }
        }

        var ceiling = best > 0.0 ? Math.Max(Floor, (int)Math.Ceiling(best * Math.Max(1.0, MaterialShare))) : 0;

        if (_ceilings.Count > 512)
        {
            _ceilings.Clear();
        }

        _ceilings[kind] = (ceiling, product, worth, now);

        return ceiling;
    }

    /// <summary>
    /// Whether a material can be had at or under its ceiling somewhere: a counter sells it no dearer, bots have made, sold or shown
    /// some this half hour (<see cref="BotGlut.Seen"/>), or it is what a hunter carves off a carcass — meat, hides, leather,
    /// feathers, wool — which the island's hunters bring in every hour. A want for a material with none of these is a want nothing
    /// can meet at a sane price: it is not said, rather than said and failed (Patrick, 02.10.2026). Nought, no ceiling: true.
    /// </summary>
    public static bool Sourced(Type kind, int ceiling)
    {
        if (ceiling <= 0 || kind == null)
        {
            return true;
        }

        var counter = BotShops.Lowest(kind);

        if (counter > 0 && counter <= ceiling)
        {
            return true;
        }

        if (BotGlut.Seen(kind) > 0)
        {
            return true;
        }

        for (var i = 0; i < BotOven.Raw.Count; i++)
        {
            if (BotOven.Raw[i] == kind)
            {
                return true;
            }
        }

        return kind == typeof(Feather) || kind == typeof(Wool) || typeof(BaseHides).IsAssignableFrom(kind)
               || typeof(BaseLeather).IsAssignableFrom(kind);
    }

    // ---- The ceiling of a want read back from the save (03.10.2026, build next) ------------------------------------------------

    /// <summary>A material's ceiling and, for raw meat, the meal it makes, whichever is lower; nought when neither is known.</summary>
    public static int MaterialCap(Type kind)
    {
        var cap = Ceiling(kind);

        if (BotOven.MealWorth(kind) is > 0 and var meal && (cap <= 0 || meal < cap))
        {
            cap = meal;
        }

        return cap;
    }

    /// <summary>How long after the market starts a want read back with no ceiling is still asked for one. Ten minutes.</summary>
    public static int RecapMs { get; set; } = 600000;

    /// <summary>Wants read back from the save given their material's ceiling on the beat; of them, offers brought down to it, and the gold a unit came down by, summed over the units wanted.</summary>
    public static long Recapped { get; private set; }

    public static long BroughtDown { get; private set; }

    public static long BroughtDownGold { get; private set; }

    private static long _startedTick;

    /// <summary>
    /// A want read back from the save is given its material's ceiling, and an offer above it comes down to it.
    ///
    /// <para>
    /// <b>Measured (live 395, 03.10.2026, 03:08–03:25):</b> the board was read back with 113 wants at 03:08:59, before the cooking
    /// system existed and before the counters were surveyed (03:09:17), so <see cref="Materialize"/> read every material's ceiling
    /// as nothing — and a want with no ceiling is a want a trader may buy for at the butcher and bring at the buyer's walk to it.
    /// 52 trader orders for raw ribs were written in those 20 minutes, every one for a want read back: 50 priced over the offer,
    /// at 17–25gp a rib against 6, 3,657gp of carriage, for a meal worth 7. Not one was written for a want said after the start,
    /// whose ceiling was read when it was said (the butcher's 16 is over it, <see cref="BotTrading.OverItsCeiling"/>). Katra's
    /// ribs, said at 12 on build 394 and raised to 13, came back at 13 against a ceiling of 7.
    /// </para>
    /// </summary>
    internal static void Recap(BotWant want)
    {
        if (want?.Kind == null)
        {
            return;
        }

        var cap = MaterialCap(want.Kind);

        if (cap <= 0)
        {
            cap = want.Ceiling;
        }

        if (cap <= 0)
        {
            // Nothing prices it yet. Asked again on the next beat, for as long as a start leaves the island unpriced; after that it is
            // a kind with no ceiling, as a want said now would be.
            if (Core.TickCount - _startedTick >= RecapMs)
            {
                want.CeilingPending = false;
            }

            return;
        }

        cap = Math.Max(FloorFor(want.Kind), cap);
        want.Ceiling = cap;
        want.CeilingPending = false;
        Recapped++;

        var down = want.Bring(cap);

        if (down <= 0)
        {
            return;
        }

        BroughtDown++;
        BroughtDownGold += (long)down * want.Amount;

        logger.Information(
            "{Name}'s want for {Item}, read back from the save at {Was}gp, came down to its material's ceiling of {Offer}gp (want #{Id})",
            want.Buyer?.Self?.Name,
            want.Label,
            want.Offer + down,
            want.Offer,
            want.Id
        );
    }

    /// <summary>The ceilings in force, named, for the market line.</summary>
    private static string DescribeCeilings()
    {
        if (!Ceilings)
        {
            return "off";
        }

        if (_ceilings.Count == 0)
        {
            return "none read yet";
        }

        List<string> named = [];

        foreach (var (kind, known) in _ceilings)
        {
            if (known.Ceiling <= 0 || named.Count >= 8)
            {
                continue;
            }

            named.Add($"{kind.Name} {known.Ceiling}gp ({known.Product?.Name} at {known.Worth})");
        }

        return named.Count == 0 ? "no material read has a priced product" : string.Join(", ", named);
    }

    // ---- What a want opens at, and how far it climbs (03.10.2026, build next) ------------------------------------------------

    /// <summary>
    /// How long a buyer's want that gave up is remembered, so the same buyer's next want for the same kind opens no higher than
    /// it did. Two hours: a re-ask comes minutes after a give-up (Vaeril's chain legs: 05:53:55 given up, 06:03:31 said again).
    /// </summary>
    public static int ReaskMs { get; set; } = 7200000;

    /// <summary>A give-up remembered: what the want had opened at, and when it gave up. By buyer and kind.</summary>
    private static readonly Dictionary<(Serial Buyer, Type Kind), (int Opened, long Tick)> _gaveUp = [];

    /// <summary>New wants opened at what the thing is worth while a dearer offer for the kind stood (the old opening), and the gold that offer was over the opening, a unit, summed over the units asked.</summary>
    public static long UnderDearer { get; private set; }

    public static long UnderDearerGold { get; private set; }

    /// <summary>New wants whose opening was held to <see cref="MostMultiple"/> times the cheapest shopkeeper's price, and the gold over it, summed over units.</summary>
    public static long ShelfCapped { get; private set; }

    public static long ShelfCappedGold { get; private set; }

    /// <summary>New wants anchored where a shopkeeper's price was known, so their climb stops at four times it.</summary>
    public static long ShelfKnown { get; private set; }

    /// <summary>Wants said again by a buyer whose want for the kind gave up within <see cref="ReaskMs"/>; of them, held to what the given-up want opened at, and the gold, summed over units.</summary>
    public static long Reasked { get; private set; }

    public static long ReaskCapped { get; private set; }

    public static long ReaskCappedGold { get; private set; }

    /// <summary>Wants given their shopkeeper's price on the beat (read back from the save, or said before the counters were surveyed); of them, brought down to four times it, and the gold, summed over units.</summary>
    public static long Shelved { get; private set; }

    public static long ShelvedDown { get; private set; }

    public static long ShelvedDownGold { get; private set; }

    /// <summary>Raises a want at four times its shopkeeper's price did not take; it waited at its price instead (to <see cref="MostAgeMs"/>).</summary>
    public static long ShelfHeld { get; private set; }

    /// <summary>What <see cref="Opening"/> did to one opening, counted only when the want is really said.</summary>
    private struct OpeningNote
    {
        public int Shelf;
        public int Dearer;
        public int ShelfCut;
        public bool Reask;
        public int ReaskCut;
    }

    /// <summary>
    /// What a new want opens at: the caller's offer — what the thing is worth (<see cref="Worth"/>: what it sold for between bots,
    /// else the buyer's own estimate: a shopkeeper's price, what it costs to make, a stand-in) — no higher than
    /// <see cref="MostMultiple"/> times what the cheapest shopkeeper on the island asks for one, and, said again by a buyer whose
    /// want for the kind gave up lately, no higher than that want opened at.
    ///
    /// <para>
    /// <b>Measured (build 396, 04:19–12:47, and live 400):</b> the highest raised offers were chain chests at 3,840gp and chain legs
    /// at 3,456 (the smith's counter: 143 and 149), plate gorgets at 1,273 (104), leather chests at 902 (101). Every chain-legs want
    /// opened at 864 — ×4 of the armourer's own 216, read off another buyer's climb — and 14 of the 26 were the same buyer saying
    /// again a want that had just given up at 3,456. Traders handed chain chests at 1,683gp on average, bought for 143. A want's
    /// climb now stops at ×4 of its own honest opening and ×4 of the counter's price, whichever is lower (<see cref="BotWant.Highest"/>),
    /// and a re-ask starts where the first one started.
    /// </para>
    /// </summary>
    private static int Opening(Mobile buyer, Type kind, int offer, out OpeningNote note)
    {
        note = default;

        // The dearest offer standing for the kind: what the old reckoning opened at, kept only to count what this one did not.
        note.Dearer = Best(kind);

        var shelf = BotShops.Lowest(kind);

        if (shelf > 0)
        {
            note.Shelf = shelf;

            var most = (int)Math.Min(int.MaxValue, shelf * (long)Math.Max(1.0, MostMultiple));

            if (offer > most)
            {
                note.ShelfCut = offer - most;
                offer = most;
            }
        }

        if (buyer != null && _gaveUp.TryGetValue((buyer.Serial, kind), out var gave))
        {
            if (Core.TickCount - gave.Tick >= ReaskMs)
            {
                _gaveUp.Remove((buyer.Serial, kind));
            }
            else
            {
                note.Reask = true;

                if (gave.Opened > 0 && offer > gave.Opened)
                {
                    note.ReaskCut = offer - gave.Opened;
                    offer = gave.Opened;
                }
            }
        }

        return offer;
    }

    /// <summary>The counts for a want the market took. See <see cref="Opening"/>.</summary>
    private static void Opened(in OpeningNote note, int offer, int units)
    {
        if (note.Dearer > offer)
        {
            UnderDearer++;
            UnderDearerGold += (long)(note.Dearer - offer) * units;
        }

        if (note.Shelf > 0)
        {
            ShelfKnown++;
        }

        if (note.ShelfCut > 0)
        {
            ShelfCapped++;
            ShelfCappedGold += (long)note.ShelfCut * units;
        }

        if (note.Reask)
        {
            Reasked++;

            if (note.ReaskCut > 0)
            {
                ReaskCapped++;
                ReaskCappedGold += (long)note.ReaskCut * units;
            }
        }
    }

    /// <summary>A want gave up (at its ceiling, or unable to reserve a louder price): what it opened at is remembered for its buyer.</summary>
    private static void GaveUp(BotWant want)
    {
        if (want?.Buyer?.Self is not { } buyer || want.Kind == null)
        {
            return;
        }

        if (_gaveUp.Count > 4096)
        {
            _gaveUp.Clear();
        }

        _gaveUp[(buyer.Serial, want.Kind)] = (want.Opened, Core.TickCount);
    }

    /// <summary>
    /// A want read back from the save, or said before the counters were surveyed, is given its shopkeeper's price on the beat, and
    /// an offer above four times it comes down to it (<see cref="BotWant.Bring"/>). Asked until <see cref="RecapMs"/> after the
    /// start; a kind no shopkeeper sells then has none, as a want said now would.
    ///
    /// <para>
    /// <b>The board read back at 12:47 on 03.10.2026 held chain legs at 993gp anchored at 864, leather chests anchored at 384,
    /// harm scrolls at 88:</b> anchors that were themselves another buyer's climb. A read-back want keeps its anchor (the save
    /// holds no other), so it is the counter's ×4 that brings it back to a price.
    /// </para>
    /// </summary>
    internal static void Shelve(BotWant want)
    {
        if (want?.Kind == null)
        {
            return;
        }

        var shelf = BotShops.Lowest(want.Kind);

        if (shelf <= 0)
        {
            if (Core.TickCount - _startedTick >= RecapMs)
            {
                want.ShelfPending = false;
            }

            return;
        }

        want.Shelf = shelf;
        want.ShelfPending = false;
        Shelved++;

        var down = want.Bring((int)Math.Min(int.MaxValue, shelf * (long)Math.Max(1.0, MostMultiple)));

        if (down <= 0)
        {
            return;
        }

        ShelvedDown++;
        ShelvedDownGold += (long)down * want.Amount;

        logger.Information(
            "{Name}'s want for {Item} came down from {Was}gp to {Offer}gp, four times the {Shelf}gp a shopkeeper asks for one (want #{Id})",
            want.Buyer?.Self?.Name,
            want.Label,
            want.Offer + down,
            want.Offer,
            shelf,
            want.Id
        );
    }

    /// <summary>
    /// The dearest standing offer for each kind against what that want opened at and what a shopkeeper asks, the steepest climbs
    /// first: the number that read "ChainLegs 3456 (anchor 864)" before this build.
    /// </summary>
    private static string DescribeDearest(int most)
    {
        if (_wants.Count == 0)
        {
            return "none standing";
        }

        Dictionary<Type, BotWant> dearest = [];

        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];

            if (want?.Kind == null || want.Amount <= 0)
            {
                continue;
            }

            if (!dearest.TryGetValue(want.Kind, out var known) || want.Offer > known.Offer)
            {
                dearest[want.Kind] = want;
            }
        }

        List<BotWant> ordered = [..dearest.Values];

        ordered.Sort(static (a, b) => ((double)b.Offer / Math.Max(1, b.Anchor)).CompareTo((double)a.Offer / Math.Max(1, a.Anchor)));

        List<string> named = [];

        for (var i = 0; i < ordered.Count && i < most; i++)
        {
            var want = ordered[i];
            var shelf = want.Shelf > 0 ? $", a shopkeeper's {want.Shelf}" : "";

            named.Add($"{want.Kind.Name} {want.Offer}gp against its {want.Anchor} (×{(double)want.Offer / Math.Max(1, want.Anchor):F1}{shelf})");
        }

        return string.Join(", ", named);
    }

    /// <summary>The clause of <c>The market:</c> about what wants open at and how far they climb.</summary>
    private static string DescribeOpenings() =>
        $"what a want opens at (what the thing sold for between bots, else the buyer's own estimate; never another buyer's standing offer): {UnderDearer} of {Posted} said while a dearer offer for the kind stood ({UnderDearerGold}gp a unit under it, summed over units — the old reckoning opened at that offer, to ×{MostMultiple:F0} of the buyer's estimate), "
        + $"{ShelfKnown} anchored where a shopkeeper sells the kind and so climbing no higher than ×{MostMultiple:F0} its price, {ShelfCapped} held to that at the opening ({ShelfCappedGold}gp less asked), {ShelfHeld} raises not taken there (the want waited at its price), "
        + $"{Reasked} said again within {ReaskMs / 60000} min of the same buyer's want for the kind giving up and {ReaskCapped} of them held to what that one opened at ({ReaskCappedGold}gp less asked); "
        + $"read back or said before the counters were read: {Shelved} given their shopkeeper's price on the beat, {ShelvedDown} brought down to ×{MostMultiple:F0} it ({ShelvedDownGold}gp less held); "
        + $"dearest standing offers against what they opened at: {DescribeDearest(6)}";

    /// <summary>An item's name for a person to read: its own, or the type's spaced out.</summary>
    public static string Label(Item item)
    {
        var name = item?.Name;

        return !string.IsNullOrWhiteSpace(name) ? name : Spaced(item?.GetType().Name ?? "goods");
    }

    /// <summary>"IronIngot" as "Iron Ingot".</summary>
    public static string Spaced(string raw)
    {
        raw ??= "goods";

        using var spaced = ValueStringBuilder.Create(raw.Length + 8);

        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];

            if (i > 0 && char.IsUpper(c))
            {
                spaced.Append(' ');
            }

            spaced.Append(c);
        }

        return spaced.ToString();
    }

    // ---- The spoken wants ---------------------------------------------------------------------------------------------------

    private static readonly List<BotWant> _wants = [];

    private static int _nextWantId;

    public static IReadOnlyList<BotWant> Wants => _wants;

    /// <summary>Wants standing.</summary>
    public static int Asks => _wants.Count;

    /// <summary>
    /// Whether the board has no room. Public because the check belongs in whoever is choosing: passing a candidate over is free,
    /// failing an errand is a loop.
    /// </summary>
    public static bool Full => _wants.Count >= MaxWants;

    /// <summary>Wants said for the first time, asked again (topped up), met to the last unit, given up at their ceiling.</summary>
    public static long Posted { get; private set; }

    public static long Topped { get; private set; }

    public static long Met { get; private set; }

    public static long Abandoned { get; private set; }

    /// <summary>Wants refused at the asking because the buyer could not have paid for them.</summary>
    public static long Unfunded { get; private set; }

    /// <summary>Wants dropped on the beat because the buyer could no longer pay for one, or was gone.</summary>
    public static long Unpaid { get; private set; }

    public static long Orphaned { get; private set; }

    /// <summary>Wants taken back by their own buyer: it got the thing some other way.</summary>
    public static long Retracted { get; private set; }

    /// <summary>Offers raised after going unanswered, and cut after being answered again soon.</summary>
    public static long Raises { get; private set; }

    public static long Cuts { get; private set; }

    /// <summary>Units and gold handed over against wants, all routes (a carrier's delivery, a buyer at a shop).</summary>
    public static long Answered { get; private set; }

    public static long AnsweredGold { get; private set; }

    /// <summary>Wants said aloud in the trade chat.</summary>
    public static long Said { get; private set; }

    /// <summary>Reservations of carriers that were gone, swept on the beat.</summary>
    public static long Swept { get; private set; }

    // ---- The reservation: a want's money is promised when the want is said (02.10.2026, build 391) ------------------------

    /// <summary>Wants not said because the buyer's other open wants already held the money (<see cref="Ask"/>), and the gold they asked for.</summary>
    public static long Unreserved { get; private set; }

    public static long UnreservedGold { get; private set; }

    /// <summary>Charges refused for reaching into money the buyer's wants hold (<see cref="Charge(Mobile, int, out int, Type)"/>), and their gold.</summary>
    public static long HeldBack { get; private set; }

    public static long HeldBackGold { get; private set; }

    /// <summary>Wants found on the beat whose buyer's purse no longer covered their whole reservation: money left the buyer that was promised.</summary>
    public static long Uncovered { get; private set; }

    /// <summary>Wants kept through their buyer's death rather than stood down for a ghost's empty pack, and through its grace back on its feet.</summary>
    public static long KeptThroughDeath { get; private set; }

    public static long KeptRising { get; private set; }

    /// <summary>Wants whose buyer left the world to rest, kept standing and not asked about its purse while it was away.</summary>
    public static long KeptAway { get; private set; }

    /// <summary>Of <see cref="Unpaid"/>, stand-downs of a buyer that had died while the want stood.</summary>
    public static long UnpaidAfterDeath { get; private set; }

    /// <summary>Raises a buyer could not reserve, so the want waited at its price; and wants given up at <see cref="MostAgeMs"/> for it.</summary>
    public static long Held { get; private set; }

    public static long AgedOut { get; private set; }

    /// <summary>Of <see cref="Retracted"/>, by why the buyer took the want back. See <see cref="BotWithdraw"/>.</summary>
    private static readonly long[] _retractedFor = new long[5];

    private static readonly Dictionary<Serial, long> _reserved = [];

    private static bool _reservedStale = true;

    /// <summary>Something about a want's units or price changed: the reservations are read again on the next ask.</summary>
    internal static void Touched() => _reservedStale = true;

    /// <summary>
    /// The gold this bot's open wants hold: units still wanted (those on their way included, because they are paid at the
    /// meeting) times the offer, summed. Read off a table rebuilt only after a want changed, so asking it costs a lookup.
    ///
    /// <para>
    /// <b>Patrick, 02.10.2026: "Money for the purchase must be reserved when the want is made."</b> Nothing is taken out of the
    /// purse — hand to hand still pays at the meeting — but what is promised is no longer free: <see cref="BotYield.Spendable"/>
    /// is wealth less this, and every decision to spend money elsewhere reads that.
    /// </para>
    /// </summary>
    public static int Reserved(Mobile body)
    {
        if (body == null || _wants.Count == 0)
        {
            return 0;
        }

        Refresh();

        return (int)Math.Min(int.MaxValue, _reserved.GetValueOrDefault(body.Serial));
    }

    private static void Refresh()
    {
        if (!_reservedStale)
        {
            return;
        }

        _reserved.Clear();

        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];

            if (want.Buyer?.Self is { } buyer)
            {
                // And the carriage over the offer it agreed to with a trader for the units on their way (build next). See BotWant.Premium.
                _reserved[buyer.Serial] = _reserved.GetValueOrDefault(buyer.Serial) + Holds(want);
            }
        }

        _reservedStale = false;
    }

    /// <summary>
    /// The gold one want holds: units still wanted at the offer, and the carriage agreed over it, less what the want has already
    /// advanced a brewer for its glass and herbs (03.10.2026, econ13): that money is out of the purse and comes off the bill at the
    /// meeting. See <see cref="Reserved"/> and <see cref="BotAdvance"/>.
    /// </summary>
    private static long Holds(BotWant want) => Math.Max(0L, (long)want.Amount * want.Offer + want.Premium - BotAdvance.On(want));

    /// <summary>
    /// How many units of this want its buyer may pay for at <paramref name="price"/> apiece, without reaching into what its other
    /// units, its other wants or another carrier's agreed carriage hold. The units' own reservation pays the offer; what a price
    /// asks over the offer comes out of the buyer's free money, and out of the carriage held for <paramref name="carrier"/> when
    /// that carrier agreed it (<see cref="BotWant.PremiumOf"/>). Never more than the purse holds, nor more than are wanted.
    ///
    /// <para>
    /// <b>Measured (live 395, 03.10.2026, 03:08–03:25):</b> every one of the seven raw-rib lines failed "buyer could not pay" or
    /// "… after a death" was a trader's hand-over priced over the offer that drew on the reservation of the units still wanted.
    /// Katra, 13 ribs at 13: Oriel handed 11 at 19 (209gp) and left 9gp against two ribs at 13; Wilma, 10 at 6: Baldwin handed 6
    /// at 20 and left 11gp against four at 6; Cirdan, Gerdrun, Rafe, Amberle and Aldric the same. <see cref="Backing"/> lent the
    /// whole want's reservation to part of it: a price over the offer for some units was paid out of the money held for the rest.
    /// </para>
    /// </summary>
    public static int Affords(BotWant want, int price, IBotWilful carrier = null)
    {
        if (want?.Buyer?.Self is not { } buyer || price <= 0 || want.Amount <= 0)
        {
            return 0;
        }

        var wealth = (long)BotYield.Wealth(buyer);
        var free = Math.Max(0L, wealth - Reserved(buyer));
        var over = price - want.Offer;
        long units = want.Amount;

        if (over > 0)
        {
            units = Math.Min(units, (free + (carrier == null ? 0 : want.PremiumOf(carrier))) / over);
        }

        // What this carrier already holds of the buyer's money as a brewer's advance comes off the bill (econ13). See BotAdvance.
        units = Math.Min(units, (wealth + (carrier == null ? 0 : BotAdvance.Credit(carrier, want))) / price);

        return (int)Math.Max(0L, units);
    }

    /// <summary>This bot's want for this kind, found by its body: the meeting knows the buyer's body and the kind, not the want.</summary>
    public static BotWant Of(Mobile body, Type kind)
    {
        if (body == null || kind == null)
        {
            return null;
        }

        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];

            if (want.Kind == kind && ReferenceEquals(want.Buyer?.Self, body))
            {
                return want;
            }
        }

        return null;
    }

    // ---- The reserve kept in the account (02.10.2026, build next; Patrick's option 3, and the coordinator's reading of 392) ------

    /// <summary>
    /// Whether the gold a bot's wants hold is kept in its account rather than its pocket: what the account lacks of the reservation
    /// is put in from the pocket when a want is said and on the market's beat (<see cref="Secure"/>).
    /// </summary>
    public static bool SecuresReserve { get; set; } = true;

    /// <summary>Deposits that put a buyer's reserve in its account, and their gold; wants still covered by the account when their buyer fell, its reserve having been put there.</summary>
    public static long Secured { get; private set; }

    public static long SecuredGold { get; private set; }

    public static long SavedByBank { get; private set; }

    private static readonly HashSet<Serial> _securedBuyers = [];

    private static readonly HashSet<Serial> _securedThisBeat = [];

    /// <summary>
    /// Puts into a bot's account as much of its pocket as the account lacks to cover what its wants hold. Returns what went in.
    ///
    /// <para>
    /// <b>Measured (live 392 at 16:44, the board's own counters, 80 minutes after the boot):</b> the Needs share stood at 71.6 %;
    /// of 44 lines failed, 21 were "buyer could not pay after a death". The reservation of build 391 promised money that rode in
    /// the pocket, and a death put it on the corpse; 97 % of corpses are never gone back for. Every payment against a want draws
    /// on the account when the pack is short (<see cref="Charge(Mobile, int, out int, Type)"/>: the pack first, then
    /// <c>Banker.Withdraw</c>), so promised money in the account still pays the carrier, the trader and the shop, and a death no
    /// longer takes it. Moved the way a trader's capital is (<see cref="BotTrading.PutAway"/>, the engine's deposit reaches the box
    /// from where the bot stands): the bot's own coin into its own account, nothing made and nothing given. Not for a bot on a
    /// stipend, whose steward fills its pocket, nor a fence, which pays out of it. The coin leaves the pack before the account is
    /// credited.
    /// </para>
    /// </summary>
    public static int Secure(Mobile buyer)
    {
        if (!SecuresReserve || buyer is not BotMobile { Deleted: false, Alive: true } body || body.Class is { Stipend: > 0 } || BotFence.Is(body)
            || body.Backpack is not { } pack)
        {
            return 0;
        }

        var reserved = Reserved(body);

        if (reserved <= 0)
        {
            return 0;
        }

        var pocket = pack.GetAmount(typeof(Gold));

        if (pocket <= 0)
        {
            return 0;
        }

        var lacking = reserved - Banker.GetBalance(body);

        if (lacking <= 0)
        {
            return 0;
        }

        var put = Math.Min(pocket, lacking);

        // A box it has never opened is no box to the engine's deposit; opened here, once.
        if (body.FindBankNoCreate() == null)
        {
            _ = body.BankBox;
        }

        if (!pack.ConsumeTotal(typeof(Gold), put))
        {
            return 0;
        }

        if (!Banker.Deposit(body, put))
        {
            pack.DropItem(new Gold(put));

            return 0;
        }

        Secured++;
        SecuredGold += put;

        if (_securedBuyers.Count > 4096)
        {
            _securedBuyers.Clear();
        }

        _securedBuyers.Add(body.Serial);

        return put;
    }

    /// <summary>The gold this bot's own want for this kind holds, or nought: what a purchase of that very kind may draw on.</summary>
    public static int ReservedFor(Mobile body, Type kind)
    {
        if (body == null || kind == null)
        {
            return 0;
        }

        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];

            if (want.Kind == kind && ReferenceEquals(want.Buyer?.Self, body))
            {
                return (int)Math.Min(int.MaxValue, Holds(want));
            }
        }

        return 0;
    }

    /// <summary>
    /// The gold behind one want: its own reservation and whatever its buyer has free. Enough to ask whether the buyer can pay for
    /// one at the offer; for a number of units at a price, ask <see cref="Affords"/>, which does not lend the reservation of the
    /// units still wanted to a price over the offer for some of them (build next).
    /// </summary>
    public static int Backing(BotWant want)
    {
        if (want?.Buyer?.Self is not { } buyer)
        {
            return 0;
        }

        // Never more than the purse holds (build 391 review): a buyer whose wants hold more than it has — risen with its pack
        // still on its corpse, robbed, fined — pays what it has. Counted on the rest, a carrier walked for a hand-over that
        // BotMarket.Charge then refused whole, and the meeting's "could not pay" never stood the want down.
        var wealth = BotYield.Wealth(buyer);
        var free = Math.Max(0L, (long)wealth - Reserved(buyer));

        return (int)Math.Min(wealth, free + (long)want.Amount * want.Offer);
    }

    /// <summary>
    /// Says a want, or asks for more on the same one. Returns the want, or null.
    ///
    /// <para>
    /// <b>One want per buyer per kind, and nothing is taken.</b> The buyer must be able to pay for what it asks at the offer it
    /// names, now — pack and account together (<see cref="BotYield.Wealth"/>) — or the want is not said at all: a want nobody can
    /// honour is a carrier's walk for nothing. Asking again tops the same want up; the offer on it is not overwritten, because
    /// that offer is what this bot has learned.
    /// </para>
    ///
    /// <para>
    /// <b>And the money is promised once (02.10.2026, build 391).</b> The purse was asked for each want alone, so one purse paid
    /// for every promise at once: Delphine said 130gp of wants in two seconds as a newborn, and 175 of the 320 stand-downs of the
    /// 388 night followed other payments by the same buyer. Now what its other open wants hold (<see cref="Reserved"/>) is taken
    /// off first; a want that cannot be reserved is not said (<see cref="Unreserved"/>), and the bot, short of money, is the bot
    /// the auction sends to work that pays (<see cref="BotYield.Spendable"/>, read by the urges and the poor hunter's rules).
    /// </para>
    ///
    /// <para>
    /// <b>And no dearer than the material is worth (02.10.2026, build next).</b> A new want opens no higher than its material's
    /// ceiling (<see cref="Ceiling(Type)"/>), raw meat no higher than the meal it makes (<see cref="BotOven.MealWorth"/>), and is not
    /// said at all when nothing can supply it under that (<see cref="Sourced"/>).
    /// </para>
    /// </summary>
    public static BotWant Ask(IBotWilful buyer, Type kind, int units, int offer)
    {
        var body = buyer?.Self;

        if (!Running || body == null || kind == null || units <= 0 || offer <= 0)
        {
            return null;
        }

        // <b>Not what the pack cannot take (06.10.2026, work17).</b> On build 410 11 of the 18 Crafters stood at 125 of 125 things and
        // went on saying wants for leather, feathers, logs and ingots: every carrier and trader that answered was refused at the
        // hand-over (deliver 26, haul 31, meet 26 on b410). A want is said only when the pack would take the thing, a free place or
        // a stack it joins; the bot is marked so its peddler and porter make room first. Counted. See BotRoom.Receives.
        if (!BotRoom.Receives(body, kind))
        {
            BotRoom.Note(body);
            BotRoom.NotAsked();

            return null;
        }

        var want = Wanted(buyer, kind);

        // <b>A material's ceiling (Patrick, 02.10.2026: "a raw material is never worth more than twice what it becomes").</b> Read
        // for a new want only: a standing want carries the ceiling it was said with.
        var cap = 0;
        var scribeless = false;

        if (want == null)
        {
            cap = Ceiling(kind);

            // Raw meat is wanted only to be cooked: never dearer than the meal it makes (BotOven.MealWorth), whoever asks.
            if (BotOven.MealWorth(kind) is > 0 and var meal && (cap <= 0 || meal < cap))
            {
                cap = meal;
            }

            if (cap > 0)
            {
                cap = Math.Max(FloorFor(kind), cap);

                if (!Sourced(kind, cap))
                {
                    Unsourced++;

                    return null;
                }

                if (offer > cap)
                {
                    CappedAsks++;
                    CappedGold += (long)(offer - cap) * units;
                    offer = cap;
                }
            }
            else if (BotScribes.Ceiling(kind, offer, out scribeless) is > 0 and var writ)
            {
                // <b>A scroll no shelf sells is asked for at no more than writing it costs here (03.10.2026).</b> Not asked whether it is
                // sourced: the island's scribes are its source, and the line is said either way. See BotScribes.Ceiling.
                if (offer > writ)
                {
                    BotScribes.Capped(units, offer - writ);
                    offer = writ;
                }

                cap = writ;
            }
        }

        // <b>What it opens at is what the thing is worth, never another buyer's climb (03.10.2026, build next).</b> See Opening.
        var opening = default(OpeningNote);

        if (want == null)
        {
            offer = Opening(body, kind, offer, out opening);
        }

        // Somebody will pay for one, so whatever was decided about this kind when a rusty one was last weighed is out of date.
        _worthless.Remove(kind);

        var price = want?.Offer ?? offer;
        var owed = (want?.Amount ?? 0) + units;
        var wealth = BotYield.Wealth(body);
        var bill = (long)owed * price;

        if (wealth < bill)
        {
            Unfunded++;

            return null;
        }

        // What its other wants already hold: this want's own reservation is the part being asked again.
        var others = Reserved(body) - (want == null ? 0L : (long)want.Amount * want.Offer);

        if (wealth - others < bill)
        {
            Unreserved++;
            UnreservedGold += bill;

            return null;
        }

        if (want != null)
        {
            want.Top(units);
            Topped++;
            BotLines.Topped(want, units);

            // The larger promise is kept in the account, out of a death's reach (build next). See Secure.
            Secure(body);

            return want;
        }

        if (_wants.Count >= MaxWants)
        {
            BoardFull++;

            // Once a minute, not once an ask: the same bots ask for the same ribs every look while the board is full.
            if (Core.TickCount - _boardFullSaidTick >= 60000)
            {
                _boardFullSaidTick = Core.TickCount;

                logger.Warning(
                    "The market already holds {Count} wants (MaxWants {Max}), so {Name} could not ask for {Item}; {Refused} asks turned away so far",
                    _wants.Count,
                    MaxWants,
                    body.Name,
                    kind.Name,
                    BoardFull
                );
            }

            return null;
        }

        want = new BotWant(++_nextWantId, buyer, kind, units, offer) { Ceiling = cap, Shelf = opening.Shelf };

        // Said before the counters were surveyed after a start: its shopkeeper's price is read on the beat. See Shelve.
        want.ShelfPending = opening.Shelf <= 0 && Core.TickCount - _startedTick < RecapMs;

        _wants.Add(want);
        Touched();
        Posted++;

        // Counted for a want the market really took, not for one the purse then refused (03.10.2026, build next). See Opening.
        Opened(opening, offer, units);

        var line = BotLines.Open(want, units);

        logger.Information(
            "{Name} wants {Units} {Item} at {Offer}gp each and has said so to the island (line #{Line}; {Held}gp of its {Wealth}gp now held for its wants)",
            body.Name,
            units,
            kind.Name,
            offer,
            line.Name,
            others + bill,
            wealth
        );

        Say(want, "market:want", true);

        // A scroll nobody on the island can write: said, held at this price, counted, and the circle named once. See BotScribes.
        if (scribeless)
        {
            BotScribes.SaidBeyond(kind, offer);
        }

        // The promise is kept in the account, out of a death's reach (build next). See Secure.
        Secure(body);

        return want;
    }

    /// <summary>This bot's want for this kind, or null.</summary>
    public static BotWant Wanted(IBotWilful buyer, Type kind)
    {
        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];

            if (ReferenceEquals(want.Buyer, buyer) && want.Kind == kind)
            {
                return want;
            }
        }

        return null;
    }

    /// <summary>Whether this want is still standing.</summary>
    public static bool Stands(BotWant want) => want != null && _wants.Contains(want);

    /// <summary>
    /// The best open want for this kind that this supplier may answer, or null: never its own, never one for a kind it wants
    /// itself (the carousel the auction's sign rule closed), a partner guild's under standing orders first, then the dearest.
    /// </summary>
    public static BotWant Demand(IBotWilful supplier, Type kind)
    {
        if (kind == null || Wanted(supplier, kind) != null)
        {
            return null;
        }

        BotWant best = null;
        var bestServed = false;

        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];

            if (want.Kind != kind || !want.IsOpen || ReferenceEquals(want.Buyer, supplier))
            {
                continue;
            }

            if (want.Buyer?.Self is not { Deleted: false } buyer)
            {
                continue;
            }

            // Only a want a carrier from here could answer (01.10.2026, build 384 review): the bench's "for an order" count, the
            // cook's reason to cook and the maker's last look all read this, and a buyer on the far side of the island is not
            // an order for a bench whose pieces reach it only by a 400-tile walk. See BotCourier.Answerable.
            if (!BotCourier.Answerable(supplier, want))
            {
                continue;
            }

            var served = BotPact.Serves(supplier?.Self, buyer);

            if (best == null || served && !bestServed || served == bestServed && want.Offer > best.Offer)
            {
                best = want;
                bestServed = served;
            }
        }

        return best;
    }

    /// <summary>The best price anybody is saying it will pay for one of this kind now, or nought.</summary>
    public static int Best(Type kind)
    {
        var best = 0;

        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];

            if (want.Kind == kind && want.IsOpen && want.Offer > best)
            {
                best = want.Offer;
            }
        }

        return best;
    }

    /// <summary>
    /// Of <paramref name="units"/> of a kind a bot has just made or gathered, how many a want within a carrier's reach is waiting
    /// for (<see cref="BotCourier.Deliverable"/>): the count a trade's ending reports as "for an order", where it used to fill the
    /// want from where it stood. Nothing moves; the goods stay in the pack for the carrier's walk.
    /// </summary>
    public static int Answering(IBotWilful bot, Type kind, int units)
    {
        if (units <= 0 || !BotCourier.Deliverable(bot, kind))
        {
            return 0;
        }

        return Math.Min(units, Asked(kind));
    }

    /// <summary>
    /// Of <paramref name="units"/> of a kind in a bot's pack, how many to keep there for a carrier's walk: no more than every want
    /// within reach still asks for (<see cref="Asked"/>), less what this same pass has kept already (<paramref name="held"/>, added
    /// to here). 01.10.2026: the porter kept a whole surplus for one small want before this.
    /// </summary>
    public static int Holding(IBotWilful bot, Type kind, int units, Dictionary<Type, int> held)
    {
        if (units <= 0 || kind == null || !BotCourier.Deliverable(bot, kind))
        {
            return 0;
        }

        held.TryGetValue(kind, out var already);
        var hold = Math.Min(units, Math.Max(0, Asked(kind) - already));

        if (hold > 0)
        {
            held[kind] = already + hold;
        }

        return hold;
    }

    /// <summary>How many units of this kind are asked for and still open to a carrier, across every want.</summary>
    public static int Asked(Type kind)
    {
        var units = 0;

        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];

            if (want.Kind == kind)
            {
                units += want.Payable;
            }
        }

        return units;
    }

    private static readonly HashSet<Type> _bespoke = [];

    private static long _bespokeTick;

    private static bool _bespokeRead;

    /// <summary>How long the set of kinds somebody wants is kept before it is read again. It is asked of every bot on every beat.</summary>
    public static int BespokeMs { get; set; } = 2000;

    /// <summary>Whether anybody has an open want for this kind. Read off a set rebuilt every <see cref="BespokeMs"/>.</summary>
    public static bool Bespoke(Type kind)
    {
        if (kind == null)
        {
            return false;
        }

        var now = Core.TickCount;

        if (!_bespokeRead || now - _bespokeTick >= BespokeMs)
        {
            _bespokeRead = true;
            _bespokeTick = now;
            _bespoke.Clear();

            for (var i = 0; i < _wants.Count; i++)
            {
                if (_wants[i].IsOpen && _wants[i].Kind != null)
                {
                    _bespoke.Add(_wants[i].Kind);
                }
            }
        }

        return _bespoke.Contains(kind);
    }

    /// <summary>Whether anybody at all has an open want. The cheap first question for every reader of the board.</summary>
    public static bool AnyBespoke => _wants.Count > 0;

    /// <summary>
    /// Drops this bot's want for a kind: it has the thing now, or wants it no longer. Returns the units that were still wanted.
    /// Nothing to refund — nothing was taken.
    /// </summary>
    public static int Withdrawn(IBotWilful buyer, Type kind, BotWithdraw why)
    {
        var want = Wanted(buyer, kind);

        if (want == null)
        {
            return 0;
        }

        _wants.Remove(want);
        Touched();
        Retracted++;
        _retractedFor[Math.Clamp((int)why, 0, _retractedFor.Length - 1)]++;

        // A buyer that could not pay at the meeting is a buyer that could not pay, wherever it was found out (build 391).
        if (why == BotWithdraw.Unpaid)
        {
            BotLines.Ended(want, BotLineEnd.Unpaid, want.DiedWhileStanding ? "buyer could not pay after a death" : "buyer could not pay");
        }
        else
        {
            BotLines.Ended(want, BotLineEnd.Withdrawn, Named(why));
        }

        return want.Amount;
    }

    private static string Named(BotWithdraw why) =>
        why switch
        {
            BotWithdraw.Have => "had it another way",
            BotWithdraw.Walked => "nobody brought it, walked to a counter",
            BotWithdraw.Unpaid => "could not pay at the meeting",
            BotWithdraw.Closed => "a trader's gathering closed",
            _ => "other"
        };

    /// <summary>
    /// Goods handed to the want's buyer against it, by whichever route: the want's units come down, and a want answered again
    /// soon asks for less. The want leaves the board when nothing is left of it.
    ///
    /// <para>
    /// <b>The one choke point every fill passes, so the lines are filled here</b> (02.10.2026, build 391): who handed the units
    /// over (<paramref name="seller"/>, for "by one bot or jointly") and by which <paramref name="route"/>. See <see cref="BotLines.Filled"/>.
    /// </para>
    /// </summary>
    public static void Received(BotWant want, int units, int gold, Mobile seller, BotRoute route)
    {
        if (want == null || units <= 0)
        {
            return;
        }

        Answered += units;
        AnsweredGold += Math.Max(0, gold);
        Supplied(want.Kind, units);

        var brisk = want.Took(units, gold, BriskMs);

        BotLines.Filled(want, units, seller, route);
        BotScribes.Handover(want, units, seller);

        if (want.Amount <= 0)
        {
            _wants.Remove(want);
            Touched();
            Met++;
            BotLines.Ended(want, BotLineEnd.Met, null);

            return;
        }

        if (brisk && want.Cut(CutStep, LeastMultiple))
        {
            Cuts++;

            logger.Information(
                "{Name} dropped its offer for {Item} to {Offer}gp after being answered again soon",
                want.Buyer?.Self?.Name,
                want.Label,
                want.Offer
            );
        }
    }

    /// <summary>The same, for a buyer that bought the thing with no want object in hand: whatever want it had for the kind.</summary>
    public static void Received(IBotWilful buyer, Type kind, int units, int gold, Mobile seller, BotRoute route) =>
        Received(Wanted(buyer, kind), units, gold, seller, route);

    // ---- What the island brought against wants of a kind this past hour (03.10.2026, build next) ---------------------------------

    /// <summary>How long one of the twelve buckets of <see cref="Brought"/> is. Five minutes: twelve make the hour a line has.</summary>
    public static int SupplyBucketMs { get; set; } = 300000;

    private const int SupplyBuckets = 12;

    private static readonly Dictionary<Type, (long[] Epoch, int[] Units)> _brought = [];

    /// <summary>Units handed over against wants of a kind, by every route, into the bucket of the present five minutes.</summary>
    private static void Supplied(Type kind, int units)
    {
        if (kind == null || units <= 0)
        {
            return;
        }

        if (!_brought.TryGetValue(kind, out var ring))
        {
            ring = (new long[SupplyBuckets], new int[SupplyBuckets]);
            _brought[kind] = ring;
        }

        var epoch = Core.TickCount / Math.Max(1000, SupplyBucketMs);
        var slot = (int)(epoch % SupplyBuckets);

        if (ring.Epoch[slot] != epoch)
        {
            ring.Epoch[slot] = epoch;
            ring.Units[slot] = 0;
        }

        ring.Units[slot] += units;

        // And into the decaying pace (06.10.2026, ribs17). See Pace.
        _pace[kind] = (Decayed(kind, Core.TickCount) + units, Core.TickCount);
    }

    // ---- The pace of the last quarter of an hour (06.10.2026, ribs17) -------------------------------------------------------

    /// <summary>
    /// The half-life of <see cref="Pace"/>: what was handed fifteen minutes ago weighs half of what was handed now.
    ///
    /// <para>
    /// <b>The hour's sum followed a fall an hour late, and the rib lines said in that hour waited past theirs.</b> Builds 410–412
    /// (06.10.2026): ribs handed against wants ran at 1,100–1,300 an hour for the first 60–80 minutes of a session (carriers'
    /// packs and bank boxes drawn down by a board the gate let grow with them), then fell to 200–300 an hour within ten minutes
    /// (412: 258 handed 15:40–15:50, 25 at 16:30–16:40) while the hunts still carved 500–1,000 an hour. <see cref="Brought"/>
    /// still read 1,146 at 16:21 and 847 at 16:36, so the room stayed 570 and 420 units when a half-hour of what was coming
    /// was 150: the 550 units on the board then stood two hours, and the lines said from 15:26 failed at 60 minutes and were met
    /// at 70–90 (raw meat 98.9 % at 16:06, 80.4 % at 17:16). A decaying sum follows a fall within a half-life.
    /// </para>
    /// </summary>
    public static int SupplyHalfLifeMs { get; set; } = 900000;

    private static readonly Dictionary<Type, (double Units, long Tick)> _pace = [];

    /// <summary>The decaying sum of units handed of a kind, decayed to <paramref name="now"/>.</summary>
    private static double Decayed(Type kind, long now)
    {
        if (!_pace.TryGetValue(kind, out var p))
        {
            return 0.0;
        }

        var half = Math.Max(60000, SupplyHalfLifeMs);

        return p.Units * Math.Pow(0.5, Math.Max(0L, now - p.Tick) / (double)half);
    }

    /// <summary>
    /// Units of a kind handed over against wants an hour, at the pace of the last quarter of an hour: an exponentially decaying
    /// sum of every hand-over (carriers, traders, shops: the same choke point as <see cref="Brought"/>) with a half-life of
    /// <see cref="SupplyHalfLifeMs"/>, read as an hourly rate (a steady flow of r an hour holds the sum at r × half-life ÷ ln 2).
    /// Forgotten at a restart, like <see cref="Brought"/>; it starts at nought and rises with the first hand-overs.
    /// </summary>
    public static int Pace(Type kind)
    {
        if (kind == null)
        {
            return 0;
        }

        var half = Math.Max(60000, SupplyHalfLifeMs);

        return (int)(Decayed(kind, Core.TickCount) * Math.Log(2.0) * 3600000.0 / half);
    }

    /// <summary>
    /// What the island brings of a kind an hour, for sizing the asks for it: the lesser of the last hour's sum and the last
    /// quarter-hour's pace. A rise is believed an hour at a time (the hour's sum climbs no faster than the hand-overs), a fall
    /// within a quarter of an hour (the pace drops with them), so the board is never sized to a rate that has stopped.
    /// </summary>
    public static int Supply(Type kind) => Math.Min(Brought(kind), Pace(kind));

    /// <summary>
    /// Units of a kind handed over against wants in the last hour, by carriers, traders and shops together: what the island
    /// brings the board of that kind in the time a line has to be met. Forgotten at a restart, like every count here.
    /// </summary>
    public static int Brought(Type kind)
    {
        if (kind == null || !_brought.TryGetValue(kind, out var ring))
        {
            return 0;
        }

        var epoch = Core.TickCount / Math.Max(1000, SupplyBucketMs);
        var units = 0;

        for (var i = 0; i < SupplyBuckets; i++)
        {
            if (epoch - ring.Epoch[i] < SupplyBuckets)
            {
                units += ring.Units[i];
            }
        }

        return units;
    }

    /// <summary>
    /// Units of a kind still wanted across the board by buyers in the world, those on their way included. A buyer resting out of
    /// the world (<c>BotRest</c>) keeps its want standing for hours, unserved and unasked: counted, its units would fill the board's
    /// room for the kind once prime time ends and nobody else could ask (review of build next, F1).
    /// </summary>
    public static int Wanting(Type kind)
    {
        var units = 0;

        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];

            if (want.Kind == kind && !want.Away && want.Buyer?.Self is { Map: { } map } && map != Map.Internal)
            {
                units += want.Amount;
            }
        }

        return units;
    }

    /// <summary>How many open wants this bot has, and what they would pay a carrier.</summary>
    public static (int Open, int Worth) Asking(IBotWilful buyer)
    {
        var open = 0;
        var worth = 0;

        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];

            if (!ReferenceEquals(want.Buyer, buyer) || !want.IsOpen)
            {
                continue;
            }

            open++;
            worth += want.Worth;
        }

        return (open, worth);
    }

    /// <summary>Everything asked for, in units still wanted and in what those would pay.</summary>
    public static (int Units, int Worth) Sought()
    {
        var units = 0;
        var worth = 0;

        for (var i = 0; i < _wants.Count; i++)
        {
            units += _wants[i].Amount;
            worth += _wants[i].Amount * _wants[i].Offer;
        }

        return (units, worth);
    }

    /// <summary>Units carriers have taken the delivery of and are bringing, across every want.</summary>
    public static int Coming()
    {
        var units = 0;

        for (var i = 0; i < _wants.Count; i++)
        {
            units += _wants[i].Coming;
        }

        return units;
    }

    /// <summary>
    /// Says a want aloud in the trade chat: what, how many, at what, and where to find the buyer. Rationed by
    /// <see cref="SayWantMs"/> per want, by the voice per bot, and by the voice per item across the market
    /// (<see cref="BotVoice.TradeRepeatMs"/>): the same thing is not called twice in a minute by two buyers.
    /// </summary>
    private static void Say(BotWant want, string occasion, bool force)
    {
        var body = want?.Buyer?.Self;

        if (body == null || !BotVoice.Enabled)
        {
            return;
        }

        var now = Core.TickCount;

        if (!force && want.Said && now - want.SaidTick < SayWantMs)
        {
            return;
        }

        var town = BotTowns.Of(body.Location)?.Name;

        var fill = new Dictionary<string, string>
        {
            ["amount"] = want.Amount.ToString(),
            ["item"] = BotShopkeep.Words(want.Kind),
            ["price"] = want.Offer.ToString(),
            ["town"] = town ?? "the road"
        };

        var line = BotVoice.Phrase(occasion, "market:want", body, fill)
                   ?? $"Buying {fill["amount"]} {fill["item"]} at {fill["price"]} each. Bring them to me.";

        if (BotVoice.Say(body, "trade", line, topic: want.Kind?.Name))
        {
            want.Spoken(now);
            Said++;
        }
    }

    // ---- Money: the one correct way to take a bot's money, and to give it ---------------------------------------------------

    private static long _unfundedSaidTick;

    /// <summary>Whether a refused charge has been said at all: the tick above means nothing until it has (no zero sentinel on a tick).</summary>
    private static bool _unfundedSaid;

    /// <summary>How often a refused charge is said aloud with its amounts, in milliseconds.</summary>
    public static int UnfundedSayMs { get; set; } = 60000;

    /// <summary>
    /// Takes <paramref name="bill"/> from a bot: coin out of the pack first (<c>ConsumeTotal</c>, which really destroys it), the
    /// rest from the account; all or nothing. The auction's order and the reason nothing on this shard mints gold by accident.
    /// </summary>
    public static bool Charge(Mobile buyer, int bill) => Charge(buyer, bill, out _);

    /// <summary>The same, saying how much of the bill came out of the pack as coin; the rest was drawn on the account.</summary>
    public static bool Charge(Mobile buyer, int bill, out int fromPack) => Charge(buyer, bill, out fromPack, null);

    /// <summary>When a refusal to spend reserved money was last said aloud. See <see cref="UnfundedSayMs"/>.</summary>
    private static long _heldBackSaidTick;

    private static bool _heldBackSaid;

    /// <summary>
    /// The same, and the bill may draw on the money the buyer's own want for <paramref name="against"/> holds: the purchase of the
    /// very thing the reservation was made for (a carrier's meeting, a trader's book, the buyer at a shop against its want).
    ///
    /// <para>
    /// <b>The reservation is kept here as well as where the decisions are made (02.10.2026, build 391).</b> Everything that
    /// decides to spend reads <see cref="BotYield.Spendable"/>; this is the backstop for whatever does not, so money promised to a
    /// want is never spent elsewhere through the market's own charge. Refused and counted (<see cref="HeldBack"/>) like a purse
    /// that is short, which it is: the free part of it is.
    /// </para>
    /// </summary>
    public static bool Charge(Mobile buyer, int bill, out int fromPack, Type against)
    {
        fromPack = 0;

        if (bill <= 0)
        {
            return true;
        }

        if (buyer == null)
        {
            return false;
        }

        if (_wants.Count > 0 && Reserved(buyer) > 0)
        {
            var free = (long)BotYield.Spendable(buyer) + (against == null ? 0 : ReservedFor(buyer, against));

            if (free < bill)
            {
                HeldBack++;
                HeldBackGold += bill;

                var tick = Core.TickCount;

                if (!_heldBackSaid || tick - _heldBackSaidTick >= UnfundedSayMs)
                {
                    _heldBackSaid = true;
                    _heldBackSaidTick = tick;

                    logger.Information(
                        "{Name} did not pay {Bill}gp: {Held}gp of its {Wealth}gp is held for its wants on the board, and {Free}gp may be spent on this",
                        buyer.Name,
                        bill,
                        Reserved(buyer),
                        BotYield.Wealth(buyer),
                        free
                    );
                }

                return false;
            }
        }

        var pack = buyer.Backpack;
        var purse = pack?.GetAmount(typeof(Gold)) ?? 0;
        var taken = 0;

        if (purse > 0)
        {
            taken = Math.Min(purse, bill);

            if (!pack.ConsumeTotal(typeof(Gold), taken))
            {
                taken = 0;
            }
        }

        var rest = bill - taken;

        if (rest <= 0 || Banker.Withdraw(buyer, rest))
        {
            fromPack = taken;

            return true;
        }

        if (taken > 0)
        {
            pack.DropItem(new Gold(taken));
        }

        var now = Core.TickCount;

        if (!_unfundedSaid || now - _unfundedSaidTick >= UnfundedSayMs)
        {
            _unfundedSaid = true;
            _unfundedSaidTick = now;

            logger.Warning(
                "{Name} could not pay {Bill}gp: the pack held {Purse} of which {Taken} were consumed, the bank held {Balance} and refused {Rest}",
                buyer.Name,
                bill,
                purse,
                taken,
                Banker.GetBalance(buyer),
                rest
            );
        }

        return false;
    }

    /// <summary>Coin back to a bot for goods that did not move: the account, and the pack when the account will not take it.</summary>
    public static void Refund(Mobile who, int amount)
    {
        if (amount <= 0 || who == null || Banker.Deposit(who, amount))
        {
            return;
        }

        Force(who, amount);
    }

    /// <summary>
    /// Coin to a seller for goods that moved: into the pack as gold — it was handed over — the account when the pack will not take
    /// it, and the pack regardless when neither will, because coin nobody holds is coin destroyed.
    /// </summary>
    public static void Pay(Mobile seller, int amount)
    {
        if (amount <= 0 || seller == null)
        {
            return;
        }

        var pack = seller.Backpack;

        if (pack != null && amount <= 60000)
        {
            var coin = new Gold(amount);

            if (pack.TryDropItem(seller, coin, false))
            {
                return;
            }

            coin.Delete();
        }

        if (Banker.Deposit(seller, amount))
        {
            return;
        }

        Force(seller, amount);
    }

    private static void Force(Mobile who, int amount)
    {
        var pack = who?.Backpack;

        if (pack == null)
        {
            logger.Error("{Amount}gp owed to {Name} had nowhere to go and was lost", amount, who?.Name ?? "nobody");

            return;
        }

        while (amount > 0)
        {
            var part = Math.Min(amount, 60000);

            pack.DropItem(new Gold(part));
            amount -= part;
        }
    }

    // ---- The beat -----------------------------------------------------------------------------------------------------------

    private static MarketTimer _timer;

    public static bool Started => _timer != null;

    public static void Start()
    {
        if (_timer != null)
        {
            return;
        }

        _startedTick = Core.TickCount;

        // The board the save held, made again now that the world is in memory. See Load.
        Materialize();

        var interval = TimeSpan.FromMilliseconds(Math.Max(1000, BeatMs));

        _timer = new MarketTimer(interval);
        _timer.Start();
    }

    public static void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    /// <summary>Each buyer's purse as the beat walks it down its wants, oldest first. Cleared every beat.</summary>
    private static readonly Dictionary<Serial, long> _purses = [];

    /// <summary>
    /// The wants' own turn: a buyer gone takes its want with it; a want whose buyer can no longer pay for one stands down; a
    /// want nobody has answered for <see cref="RaiseMs"/> — and that no carrier is bringing — asks louder, if the buyer can reserve
    /// the louder price; one at its ceiling gives up. Reservations of carriers that are gone are swept, and lines at their hour
    /// are settled (<see cref="BotLines.Aged"/>).
    ///
    /// <para>
    /// <b>A raise must be one the buyer could pay, and there is no escrow to top up any more.</b> The auction charged the
    /// difference out of the purse before lifting the offer; here the purse is asked instead — since build 391 for the louder
    /// price on every unit still wanted, out of what the buyer has free. A bot with nothing to outbid with cannot claim to have
    /// outbid.
    /// </para>
    ///
    /// <para>
    /// <b>A dead buyer keeps its want (02.10.2026, build 391).</b> <see cref="BotYield.Wealth"/> is pack and bank, and a ghost's
    /// pack is on its corpse: 69 of the 95 stand-downs on build 389 were read off a ghost, a median twelve seconds after it fell,
    /// two thirds of them at exactly nought. While the buyer lies dead its purse is not asked, and back on its feet it has
    /// <see cref="RisenGraceMs"/> to walk to its corpse before it is asked again. What it could not get back is then a real
    /// stand-down, and named as one after a death.
    /// </para>
    ///
    /// <para>
    /// <b>One purse, its older promises first.</b> Each buyer's purse is read once and walked down its wants in the order they
    /// were said; a want the rest of the purse cannot pay one of stands down. A want whose whole reservation is not covered is
    /// counted (<see cref="Uncovered"/>): money left the buyer that its wants held — a death, a fine, a robbery, or a spend that
    /// did not ask <see cref="BotYield.Spendable"/>.
    /// </para>
    /// </summary>
    private static void Beat()
    {
        var now = Core.TickCount;

        // Brewers' advances whose want is gone, shrunk or uncarried are called and paid back (03.10.2026, econ13): first, so the
        // purses below read what the loans still hold. See BotAdvance.Sweep.
        BotAdvance.Sweep(now);

        // Lines taken back inside their first ten minutes, followed for twenty: did the buyer get the kind? (06.10.2026, needs15). See BotLines.Watched.
        BotLines.Watched(now);

        // The purses first, oldest want first: the list is in the order the wants were said.
        _purses.Clear();
        _securedThisBeat.Clear();

        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];

            if (want.Buyer?.Self is not { Deleted: false } buyer)
            {
                continue;
            }

            // A want read back before its material could be priced has its ceiling read again now (build next). See Recap.
            if (want.CeilingPending)
            {
                Recap(want);
            }

            // And its shopkeeper's price, the most a climb may reach four times of (03.10.2026, build next). See Shelve.
            if (want.ShelfPending)
            {
                Shelve(want);
            }

            // What its wants hold goes into the account, once a beat for each living buyer (build next): a purchase drew coin into
            // the pocket, or a split was paid there since. Wealth does not move, so the walk below is unchanged. See Secure.
            if (buyer.Alive && _securedThisBeat.Add(buyer.Serial))
            {
                Secure(buyer);
            }

            if (!_purses.TryGetValue(buyer.Serial, out var left))
            {
                left = BotYield.Wealth(buyer);
            }

            var need = Holds(want);

            // A brewer holding the want's advance has been paid that much of it already (econ13). See BotAdvance.
            want.Short = left + BotAdvance.On(want) < want.Offer;

            var uncovered = left < need;

            if (uncovered && !want.Uncovered && buyer.Alive)
            {
                Uncovered++;
            }

            want.Uncovered = uncovered;
            _purses[buyer.Serial] = Math.Max(0L, left - need);
        }

        for (var i = _wants.Count - 1; i >= 0; i--)
        {
            var want = _wants[i];
            var buyer = want.Buyer?.Self;

            if (!Running || buyer == null || buyer.Deleted)
            {
                _wants.RemoveAt(i);
                Touched();
                Orphaned++;
                BotLines.Ended(want, Running ? BotLineEnd.Orphaned : BotLineEnd.Stopped, null);

                continue;
            }

            Swept += want.Sweep();

            if (want.Amount <= 0)
            {
                _wants.RemoveAt(i);
                Touched();
                Met++;
                BotLines.Ended(want, BotLineEnd.Met, null);

                continue;
            }

            // Out of the world, resting (BotRest parks a bot on the internal map): nothing is asked of its purse, nothing raised,
            // and a line at its hour while it is away is kept out. Asked before the hour, so the line knows. See BotLines.AtHour.
            if (buyer.Map == null || buyer.Map == Map.Internal)
            {
                if (want.Left(now))
                {
                    KeptAway++;
                }

                BotLines.Aged(want, now);

                continue;
            }

            want.Returned(now);

            // A line at its hour has failed it, whatever happens to the want next.
            BotLines.Aged(want, now);

            if (!buyer.Alive)
            {
                if (want.Fell(now))
                {
                    KeptThroughDeath++;

                    // Read off a ghost, whose pocket lies on its corpse: still covered, by the account its reserve was put in.
                    if (!want.Uncovered && _securedBuyers.Contains(buyer.Serial))
                    {
                        SavedByBank++;
                    }
                }

                continue;
            }

            if (want.Rose(now))
            {
                KeptRising++;
            }

            if (want.Rising && now - want.RoseTick < RisenGraceMs)
            {
                continue;
            }

            want.Rising = false;

            if (want.Short)
            {
                var wealth = BotYield.Wealth(buyer);

                _wants.RemoveAt(i);
                Touched();
                Unpaid++;

                if (want.DiedWhileStanding)
                {
                    UnpaidAfterDeath++;
                }

                BotLines.Ended(want, BotLineEnd.Unpaid, want.DiedWhileStanding ? "buyer could not pay after a death" : "buyer could not pay");

                logger.Information(
                    "{Name} stood down its want for {Item}: {Wealth}gp will not pay for one at {Offer}gp (want #{Id}{Death})",
                    buyer.Name,
                    want.Label,
                    wealth,
                    want.Offer,
                    want.Id,
                    want.DiedWhileStanding ? ", after a death" : ""
                );

                continue;
            }

            // A carrier is on its way: the want has been answered, the goods are walking.
            if (want.Coming > 0 || now - want.TouchedTick < RaiseMs)
            {
                if (want.IsOpen)
                {
                    Say(want, "market:want", false);
                }

                continue;
            }

            // <b>At its material's ceiling (02.10.2026, build next): it waits at its price.</b> A want for a material that has reached
            // twice what it becomes is not raised and not given up at its first ten minutes; it stands, as a want whose buyer cannot
            // reserve a raise stands, until MostAgeMs. A hunter may still bring it at that price; a trader may not buy it dearer.
            var atMaterial = want.Ceiling > 0 && want.Offer >= want.Ceiling;

            if (atMaterial && now - want.PostedTick < MostAgeMs)
            {
                want.Waited();

                // A scroll's ceiling is the price of writing it, not a material's: counted on the scribes' line. See BotScribes.
                if (BotScribes.Writ(want.Kind))
                {
                    BotScribes.Held();
                }
                else
                {
                    CeilingHeld++;
                }

                if (want.IsOpen)
                {
                    Say(want, "market:want", false);
                }

                continue;
            }

            // <b>At four times what a shopkeeper asks (03.10.2026, build next): it waits at its price, as a material's want does.</b> A
            // want anchored under the counter's ×4 reaches it sooner than its own ×4, or opens there; given up ten minutes later, its
            // line would fail for want of a price nobody should pay. It stands to MostAgeMs, for a carrier or a trader at that price.
            var atShelf = !atMaterial && want.Shelf > 0 && want.Offer >= want.Shelf * (long)Math.Max(1.0, MostMultiple);

            if (atShelf && now - want.PostedTick < MostAgeMs)
            {
                want.Waited();
                ShelfHeld++;

                if (want.IsOpen)
                {
                    Say(want, "market:want", false);
                }

                continue;
            }

            var stepped = want.Stepped(RaiseStep, MostMultiple);

            // The louder price is a larger promise: what the buyer has free must cover the difference on every unit still wanted.
            if (stepped > 0 && BotYield.Spendable(buyer) >= (long)want.Amount * (stepped - want.Offer))
            {
                want.Lift(stepped);
                Raises++;

                logger.Information(
                    "{Name} raised its offer for {Item} to {Offer}gp after {Amount} went unanswered",
                    buyer.Name,
                    want.Label,
                    want.Offer,
                    want.Amount
                );

                Say(want, "market:raise", true);

                continue;
            }

            // <b>Cannot reserve the louder price: it waits at its own (build 391).</b> It used to give up on the spot, at its first
            // ten minutes, for want of one unit's worth more — a poor buyer's want then failed for being poor before anybody had
            // the time to bring it. It stands at the price it has, the buyer goes on earning, and the raise is asked again
            // in RaiseMs; it gives up only at MostAgeMs, about when a want that could pay reaches its ceiling.
            if (stepped > 0 && now - want.PostedTick < MostAgeMs)
            {
                want.Waited();
                Held++;

                if (want.IsOpen)
                {
                    Say(want, "market:want", false);
                }

                continue;
            }

            _wants.RemoveAt(i);
            Touched();
            Abandoned++;

            if (stepped > 0)
            {
                AgedOut++;
            }

            BotLines.Ended(
                want,
                BotLineEnd.Abandoned,
                stepped > 0 ? "could not reserve a louder price, given up"
                : atMaterial ? BotScribes.Writ(want.Kind) ? "given up at the price of writing it" : "given up at its material's ceiling"
                : atShelf ? "given up at four times a shopkeeper's price"
                : "given up at its ceiling"
            );

            // The same buyer's next want for the kind opens no higher than this one did (03.10.2026, build next). See Opening.
            GaveUp(want);

            // Two sentences reach this line: nobody here can make the thing, or this bot cannot ask any louder. The offer against
            // the ceiling tells them apart. The ceiling is the one the raise stopped at (build next): its anchor's ×4, its material's
            // ceiling or its shopkeeper's ×4.
            logger.Information(
                "{Name} gave up wanting {Item} at {Offer}gp of a possible {Ceiling}",
                buyer.Name,
                want.Label,
                want.Offer,
                want.Highest(MostMultiple)
            );
        }
    }

    // ---- What it says -------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The wants, by kind: the most-asked things, and what the shops at the market hold. The two opposite defects — orders
    /// nobody fills, and goods nobody asks for — read side by side.
    /// </summary>
    public static string Board(int most = 6)
    {
        Dictionary<Type, int> wanted = [];
        Dictionary<Type, int> shown = [];

        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];

            if (want?.Kind != null && want.Amount > 0)
            {
                wanted[want.Kind] = wanted.GetValueOrDefault(want.Kind) + want.Amount;
            }
        }

        var open = BotShopkeep.Open;

        for (var i = 0; i < open.Count; i++)
        {
            var front = open[i];

            if (front.Shut)
            {
                continue;
            }

            foreach (var kind in front.Goods)
            {
                shown[kind] = shown.GetValueOrDefault(kind) + front.Held(kind);
            }
        }

        return $"most wanted: {Top(wanted, most)}; on show at the shops: {Top(shown, most)}";
    }

    private static string Top(Dictionary<Type, int> tally, int most)
    {
        if (tally.Count == 0)
        {
            return "nothing";
        }

        List<(Type Kind, int Amount)> ordered = [];

        foreach (var (kind, amount) in tally)
        {
            ordered.Add((kind, amount));
        }

        ordered.Sort(static (a, b) => b.Amount.CompareTo(a.Amount));

        using var say = ValueStringBuilder.Create(160);

        for (var i = 0; i < ordered.Count && i < most; i++)
        {
            if (i > 0)
            {
                say.Append(", ");
            }

            say.Append(ordered[i].Kind.Name);
            say.Append(' ');
            say.Append(ordered[i].Amount);
        }

        return say.ToString();
    }

    /// <summary>
    /// The market line: the wants, what was handed over hand to hand and how, the carriers, and what the island has learned to
    /// price. Printed as <c>The market:</c> every five minutes.
    /// </summary>
    public static string Describe()
    {
        var (units, worth) = Sought();

        return $"{_wants.Count} of {MaxWants} wants for {units} things worth {worth}gp ({Coming()} of them being carried now); "
               + $"{Posted} wants said, {Topped} asked for more, {Said} said aloud in the trade chat, {Met} met to the last unit, {Abandoned} given up at their ceiling, {Unpaid} stood down when the buyer could no longer pay for one, {Unfunded} refused at the asking for want of money, {Retracted} taken back by their buyer, {Orphaned} gone with their buyer; {Raises} offers raised, {Cuts} cut after being answered again soon; "
               + $"{Answered} things handed over against wants for {AnsweredGold}gp; "
               + $"hand to hand: {BotHandover.Describe()}; "
               + $"carriers: {BotCourier.Describe()}; deliveries: {BotDeliver.Describe()}; {BotRoom.Describe()}; {BotWayless.Describe()}; {BotUnreached.Describe()}; {BotRendezvous.Describe()}; "
               + $"{BoxTaken} things taken out of bank boxes by their owners at a bank; {Swept} reservations of carriers swept; "
               + $"materials at most {MaterialShare:F0} times what they become: {CappedAsks} wants opened at their ceiling instead of above it ({CappedGold}gp less asked), {CeilingHeld} raises not taken at the ceiling (the want waited at its price), {Unsourced} wants not said for nothing selling or making the material under it, {BotTrading.OverCeiling} traders' sources passed over for costing more than the want's ceiling; ceilings now: {DescribeCeilings()}; "
               + $"{DescribeOpenings()}; "
               + $"reserve: {DescribeReserve()}; "
               + $"prices learned from goods that changed hands for {_dealt.Count} kinds; {Cheap} things were weighed under the {Floor}gp floor ({Unpriced} of them priced by nothing, which condemns no kind), and the condemned kinds are {Condemned(8)}";
    }

    /// <summary>The gold the wants hold now, and by how many buyers.</summary>
    private static (long Gold, int Buyers) HeldNow()
    {
        if (_wants.Count == 0)
        {
            return (0, 0);
        }

        Refresh();

        long gold = 0;

        foreach (var (_, held) in _reserved)
        {
            gold += held;
        }

        return (gold, _reserved.Count);
    }

    /// <summary>
    /// The reservation and the buyer's death on the board, for the market line (02.10.2026, build 391): what the wants hold, what
    /// was refused for it, what the beat found short, and the wants kept through a death.
    /// </summary>
    private static string DescribeReserve()
    {
        var (gold, buyers) = HeldNow();

        return $"{gold}gp held by {buyers} buyers for their wants; {Secured} deposits put {SecuredGold}gp of it from pockets into accounts{(SecuresReserve ? "" : " (off)")}, {SavedByBank} wants still covered by the account when their buyer fell; {Unreserved} wants not said for the money the buyer's other wants already held ({UnreservedGold}gp asked); "
               + $"{HeldBack} charges refused for reaching into held money ({HeldBackGold}gp); {Uncovered} wants found by the beat no longer covered by their buyer's purse; "
               + $"{KeptThroughDeath} wants kept through their buyer's death and {KeptRising} through its first {RisenGraceMs / 60000} minutes back, {UnpaidAfterDeath} of the stand-downs after a death, {KeptAway} kept while their buyer rested out of the world; "
               + $"{Held} raises the buyer could not reserve, the want waiting at its price, {AgedOut} given up at {MostAgeMs / 60000} minutes for it; "
               + $"taken back: {_retractedFor[(int)BotWithdraw.Have]} had it another way, {_retractedFor[(int)BotWithdraw.Walked]} walked to a counter, {_retractedFor[(int)BotWithdraw.Unpaid]} could not pay at the meeting, {_retractedFor[(int)BotWithdraw.Closed]} gatherings closed, {_retractedFor[(int)BotWithdraw.Other]} other; "
               + $"{BoardFull} asks turned away by the board's ceiling of {MaxWants}; the board kept across restarts: {RestoredWants} wants and {RestoredLines} lines read back, {DroppedWants} dropped (buyer gone or kind unknown), {Recapped} given their material's ceiling on the beat and {BroughtDown} of them brought down to it ({BroughtDownGold}gp less held); "
               + $"carriage over the offer agreed with traders and held now: {Carriage()}gp";
    }

    /// <summary>The carriage over their offers the buyers have agreed with traders and that is held for it now. See <see cref="BotWant.Premium"/>.</summary>
    private static long Carriage()
    {
        long gold = 0;

        for (var i = 0; i < _wants.Count; i++)
        {
            gold += _wants[i].Premium;
        }

        return gold;
    }

    /// <summary>The <c>reserve</c> object inside the <c>needs</c> block of <c>/api/state</c>. See <see cref="BotLines.Web"/>.</summary>
    internal static void WebReserve(System.Text.Json.Utf8JsonWriter w)
    {
        var (gold, buyers) = HeldNow();

        w.WritePropertyName("reserve");
        w.WriteStartObject();
        w.WriteNumber("gold", gold);
        w.WriteNumber("buyers", buyers);
        w.WriteNumber("wants", _wants.Count);
        w.WriteNumber("unreserved", Unreserved);
        w.WriteNumber("unfunded", Unfunded);
        w.WriteNumber("chargesRefused", HeldBack);
        w.WriteNumber("uncovered", Uncovered);
        w.WriteNumber("unpaid", Unpaid);
        w.WriteNumber("unpaidAfterDeath", UnpaidAfterDeath);
        w.WriteNumber("keptThroughDeath", KeptThroughDeath);
        w.WriteNumber("raisesHeld", Held);
        w.WriteNumber("restoredWants", RestoredWants);
        w.WriteNumber("boardFull", BoardFull);
        w.WriteNumber("secured", Secured);
        w.WriteNumber("securedGold", SecuredGold);
        w.WriteNumber("savedByBank", SavedByBank);
        w.WriteNumber("recapped", Recapped);
        w.WriteNumber("broughtDown", BroughtDown);
        w.WriteNumber("carriageHeld", Carriage());
        w.WriteNumber("cutToFree", BotHandover.CutToFree);
        w.WriteEndObject();
    }

    // ---- Kept across restarts (02.10.2026, build 391; Patrick: "the board may be saved across restarts"). See BotMarketStore. --

    /// <summary>Wants and lines read back at the last start, and wants dropped there because their buyer or kind was gone.</summary>
    public static int RestoredWants { get; private set; }

    public static int RestoredLines { get; private set; }

    public static int DroppedWants { get; private set; }

    /// <summary>One want as the save holds it. Made into a <see cref="BotWant"/> at <see cref="Start"/>, once the world is in memory.</summary>
    private sealed class Saved
    {
        public int Id;
        public BotMobile Buyer;
        public Type Kind;
        public int Amount;
        public int Offer;
        public int Anchor;
        public long PostedAge;
        public long TouchedAge;
        public int Filled;
        public int Paid;
        public int Raises;
        public int Cuts;
        public bool Traded;
        public long FilledAge;
        public BotLine.Record[] Lines;
    }

    private static readonly List<Saved> _pending = [];

    /// <summary>
    /// The board into the save: every want with its lines, by ages and never ticks, its buyer by serial and its kind by name. No
    /// carrier's reservation, because no carrier's delivery is saved. Runs on a save worker while the world is frozen: reads only.
    /// </summary>
    internal static void Save(IGenericWriter writer)
    {
        var now = Core.TickCount;
        var count = 0;

        for (var i = 0; i < _wants.Count; i++)
        {
            if (Savable(_wants[i]))
            {
                count++;
            }
        }

        writer.WriteEncodedInt(_nextWantId);
        writer.WriteEncodedInt(count);

        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];

            if (!Savable(want))
            {
                continue;
            }

            writer.WriteEncodedInt(want.Id);
            writer.Write(want.Buyer.Self.Serial);
            writer.Write(want.Kind.FullName ?? want.Kind.Name);
            writer.WriteEncodedInt(want.Amount);
            writer.WriteEncodedInt(want.Offer);
            writer.WriteEncodedInt(want.Anchor);
            writer.Write(Math.Max(0L, now - want.PostedTick));
            writer.Write(Math.Max(0L, now - want.TouchedTick));
            writer.WriteEncodedInt(want.Filled);
            writer.WriteEncodedInt(want.Paid);
            writer.WriteEncodedInt(want.Raises);
            writer.WriteEncodedInt(want.Cuts);
            writer.Write(want.Traded);
            writer.Write(want.Traded ? Math.Max(0L, now - want.FilledTick) : 0L);

            var lines = want.Lines;

            writer.WriteEncodedInt(lines.Count);

            for (var j = 0; j < lines.Count; j++)
            {
                lines[j].Save(writer, now);
            }
        }
    }

    private static bool Savable(BotWant want) => want?.Kind != null && want.Amount > 0 && want.Buyer?.Self is BotMobile { Deleted: false };

    /// <summary>
    /// The board read back: every byte the save wrote is read, whatever is kept, or a headless shard stops on the engine's skip
    /// prompt. A want whose buyer is gone or whose kind this build does not know is dropped and counted; the rest wait for
    /// <see cref="Start"/>, because making a want looks its kind up on a sample item and the world is still being read.
    /// </summary>
    internal static (int Kept, int Dropped) Load(IGenericReader reader)
    {
        _pending.Clear();

        var next = reader.ReadEncodedInt();
        var count = reader.ReadEncodedInt();
        var dropped = 0;

        for (var i = 0; i < count; i++)
        {
            var saved = new Saved
            {
                Id = reader.ReadEncodedInt(),
                Buyer = reader.ReadEntity<BotMobile>()
            };

            var name = reader.ReadString();

            saved.Kind = string.IsNullOrEmpty(name) ? null : AssemblyHandler.FindTypeByName(name, true) ?? AssemblyHandler.FindTypeByName(name);
            saved.Amount = reader.ReadEncodedInt();
            saved.Offer = reader.ReadEncodedInt();
            saved.Anchor = reader.ReadEncodedInt();
            saved.PostedAge = reader.ReadLong();
            saved.TouchedAge = reader.ReadLong();
            saved.Filled = reader.ReadEncodedInt();
            saved.Paid = reader.ReadEncodedInt();
            saved.Raises = reader.ReadEncodedInt();
            saved.Cuts = reader.ReadEncodedInt();
            saved.Traded = reader.ReadBool();
            saved.FilledAge = reader.ReadLong();

            var lines = reader.ReadEncodedInt();

            saved.Lines = new BotLine.Record[Math.Max(0, lines)];

            for (var j = 0; j < saved.Lines.Length; j++)
            {
                saved.Lines[j] = BotLine.Read(reader);
            }

            if (saved.Buyer is not { Deleted: false } || saved.Kind == null || saved.Amount <= 0)
            {
                dropped++;

                continue;
            }

            _pending.Add(saved);
        }

        _nextWantId = Math.Max(_nextWantId, next);
        DroppedWants = dropped;

        return (_pending.Count, dropped);
    }

    /// <summary>The wants read back, made again now that the world is in memory. Their buyers' reservations come with them.</summary>
    private static void Materialize()
    {
        if (_pending.Count == 0)
        {
            return;
        }

        var now = Core.TickCount;
        var wants = 0;
        var lines = 0;

        for (var i = 0; i < _pending.Count; i++)
        {
            var saved = _pending[i];

            if (saved.Buyer is not { Deleted: false } buyer || _wants.Count >= MaxWants || Wanted(buyer, saved.Kind) != null)
            {
                DroppedWants++;

                continue;
            }

            var want = new BotWant(
                saved.Id,
                buyer,
                saved.Kind,
                saved.Amount,
                saved.Offer,
                saved.Anchor,
                now - Math.Max(0L, saved.PostedAge),
                now - Math.Max(0L, saved.TouchedAge)
            );

            // The material's ceiling is not saved; it is read again. A cook's own, the meal's worth, is the lower one. Read here it is
            // usually nothing — the board is made before the craft systems are built and before anything is priced — so a want read
            // back with none is asked again on the beat until it has one (03.10.2026, build next). See Recap.
            want.Ceiling = MaterialCap(saved.Kind);
            want.CeilingPending = true;

            // Its shopkeeper's price too, read on the beat once the counters are surveyed (03.10.2026, build next). See Shelve.
            want.ShelfPending = true;

            want.Restore(saved.Filled, saved.Paid, saved.Raises, saved.Cuts, saved.Traded, now - Math.Max(0L, saved.FilledAge));

            for (var j = 0; j < saved.Lines.Length; j++)
            {
                want.AddLine(BotLine.Restore(want, saved.Lines[j], now));
                lines++;
            }

            // A want saved with no line (none is, but a save is data): its remaining units are one line, opened when it was.
            if (want.Lines.Count == 0)
            {
                var line = want.OpenLine(want.Amount, want.PostedTick, BotTrading.Trader(buyer));

                line.Restored = true;
                lines++;
            }

            _wants.Add(want);
            wants++;
        }

        _pending.Clear();
        Touched();

        var open = 0;

        for (var i = 0; i < _wants.Count; i++)
        {
            var standing = _wants[i].Lines;

            for (var j = 0; j < standing.Count; j++)
            {
                if (standing[j].State == BotLineState.Open)
                {
                    open++;
                }
            }
        }

        RestoredWants = wants;
        RestoredLines = lines;
        BotLines.Carried(open);

        logger.Information(
            "The board was read back from the save: {Wants} wants with {Lines} lines ({Open} still inside their hour), {Dropped} dropped for a buyer or a kind that is gone; their clocks go on from where they stood",
            wants,
            lines,
            open,
            DroppedWants
        );
    }

    /// <summary>
    /// The whole market cleared: every want, every learned price, every count. For a world reload — the debugger's
    /// <c>ResetPopulation</c> — and the module's reset. Nothing is refunded because nothing was held.
    /// </summary>
    public static void Reset()
    {
        Stop();

        _wants.Clear();
        _worthless.Clear();
        _dealt.Clear();
        _bespoke.Clear();
        _bespokeRead = false;
        _squares.Clear();
        _atBank.Clear();
        _nextWantId = 0;
        _pending.Clear();
        _reserved.Clear();
        _reservedStale = true;
        _purses.Clear();
        Array.Clear(_retractedFor);

        Cheap = 0;
        Unpriced = 0;
        Posted = 0;
        Topped = 0;
        Met = 0;
        Abandoned = 0;
        Unfunded = 0;
        Unpaid = 0;
        Orphaned = 0;
        Retracted = 0;
        Raises = 0;
        Cuts = 0;
        Answered = 0;
        AnsweredGold = 0;
        Said = 0;
        Swept = 0;
        BoxTaken = 0;
        Unreserved = 0;
        UnreservedGold = 0;
        HeldBack = 0;
        HeldBackGold = 0;
        Uncovered = 0;
        KeptThroughDeath = 0;
        KeptRising = 0;
        KeptAway = 0;
        UnpaidAfterDeath = 0;
        Held = 0;
        AgedOut = 0;
        RestoredWants = 0;
        RestoredLines = 0;
        DroppedWants = 0;
        _ceilings.Clear();
        _securedBuyers.Clear();
        _securedThisBeat.Clear();
        Secured = 0;
        SecuredGold = 0;
        SavedByBank = 0;
        CappedAsks = 0;
        CappedGold = 0;
        CeilingHeld = 0;
        Unsourced = 0;
        Recapped = 0;
        BroughtDown = 0;
        BroughtDownGold = 0;
        _brought.Clear();
        _pace.Clear();
        _gaveUp.Clear();
        UnderDearer = 0;
        UnderDearerGold = 0;
        ShelfCapped = 0;
        ShelfCappedGold = 0;
        ShelfKnown = 0;
        Reasked = 0;
        ReaskCapped = 0;
        ReaskCappedGold = 0;
        Shelved = 0;
        ShelvedDown = 0;
        ShelvedDownGold = 0;
        ShelfHeld = 0;

        // A wipe writes the board empty at its save: BotMarketStore saves what stands, and nothing does now.
        BotLines.Forget();

        BotHandover.Forget();
        BotCourier.Forget();
        BotDeliver.Forget();
        BotUnreached.Forget();
        BotRoom.Forget();
        BotWayless.Forget();
        BotGlut.Reset();
    }

    private sealed class MarketTimer : Timer
    {
        public MarketTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => Beat();
    }
}
