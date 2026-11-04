using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Engines.Craft;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// Town charters: a monopoly on a craft in a town, sold at a sealed auction every six hours, and the levy everybody else pays
/// its holder on each sale of that craft's goods there.
///
/// <para>
/// <b>Patrick, 07.10.2026 evening, approved:</b> "every N hours a town auctions charters (the smiths' charter of Britain, the
/// tailors' of Minoc, the woodsmen's of Yew). The charter guild sells that kind in the town's shops without a levy; everyone else
/// pays the charter guild a levy on each sale (the guild's money, not the town's); the town's purse gets the auction price.
/// Charters make real specialisation; losing one at the auction is a grievance; a failed town task can cost the charter."
/// </para>
///
/// <para>
/// <b>Six crafts, read off the goods.</b> A kind belongs to a craft when it is that craft's material (ingots; leather and cloth;
/// logs, boards, shafts, arrows and bolts; potions; scrolls; food) or when the engine's own recipe book for the craft makes it
/// (<c>DefBlacksmithy</c>, <c>DefTailoring</c>, <c>DefBowFletching</c> and <c>DefCarpentry</c>, <c>DefAlchemy</c>,
/// <c>DefInscription</c>, <c>DefCooking</c>) — the same books the market's material ceiling reads. One charter per craft per town.
/// </para>
///
/// <para>
/// <b>The craft's sales are its practitioners' sales.</b> A hunter selling a looted kryss or a Flamestrike scroll off a corpse is
/// not practising smithing or scribing, and the poor hunt (Patrick's standing rule): with <see cref="LootFree"/> a seller under
/// <see cref="CraftSkill"/> in the craft's skills is neither levied nor counted in the craft's book. The book of each town's
/// craft — the gold its practitioners took there this period, by guild — is what a guild's bid is reckoned from.
/// </para>
///
/// <para>
/// <b>The auction.</b> Every <see cref="EveryMs"/> each town's crafts that sold anything, or are held, go under the hammer. A
/// guild bids when its practitioners took at least <see cref="LeastShare"/> of the craft's gold there: the charter's worth over a
/// term is the levy on the whole book (<see cref="Levy"/> × the period's gold, scaled to the term), and a guild offers
/// <see cref="Appetite"/> × (½ + ½ × its share) of it, never more than <see cref="SpareShare"/> of what its chest spares
/// (<c>BotDues.Spare</c>, which already keeps back what the members voted to save for, <c>BotPurpose</c>); a chest voted to a war
/// chest bids nothing while it is one. The highest sealed bid wins and its chest pays the town's purse (<c>BotCity.Tax</c>); no bid
/// and the charter rests. The holder that lost it, and every other losing bidder at half the weight, think worse of the winner
/// (<see cref="LostRegard"/>, written into the grievance book as "a charter lost at auction").
/// </para>
///
/// <para>
/// <b>The levy is hand to hand and makes no coin.</b> At a bot's shop (<c>BotShopkeep.Serve</c>, the city's buy) the buyer's coin is
/// split at the till: the levy into the holder's chest, the rest to the keeper. At a shopkeeper's counter (<c>BotShops.Sell</c>) and
/// a tavern's table the seller pays it out of what it was just paid, pack first, then account. A seller whose guild is hostile to
/// the holder (at war, or at the war line of regard) pays the town's purse instead: nothing crosses between enemies. The holder's own sales are free and counted.
/// </para>
///
/// <para>
/// <b>Losing it early.</b> A holder that fails <see cref="FailsToLose"/> of the town's tasks in a row (<c>BotBurgh</c>), or is put out
/// of the town, loses the town's charters at once; they go to an early auction the next minute, with that guild barred from it
/// until the next general auction. A holder guild that no longer exists loses them too.
/// </para>
/// </summary>
public static class BotCharters
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCharters));

    public enum Craft : byte
    {
        Smith,
        Tailor,
        Woodsman,
        Alchemist,
        Scribe,
        Cook
    }

    /// <summary>How many crafts there are.</summary>
    public const int Crafts = 6;

    private static readonly string[] _words = ["smiths'", "tailors'", "woodsmen's", "alchemists'", "scribes'", "cooks'"];

    private static readonly string[] _keys = ["smith", "tailor", "woodsman", "alchemist", "scribe", "cook"];

    /// <summary>"smiths'", as in "the smiths' charter of Britain".</summary>
    public static string Word(Craft craft) => _words[(int)craft];

    /// <summary>"smith": the craft's one-word name, for the page and the door.</summary>
    public static string Key(Craft craft) => _keys[(int)craft];

    // ---- Dials ---------------------------------------------------------------------------------------------------------------

    /// <summary>Whether charters are auctioned and levied at all. Off: no levy is taken and no auction is held; the charters stand.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How often every town auctions its charters: six hours of running time.</summary>
    public static int EveryMs { get; set; } = 21600000;

    /// <summary>The first auction after a boot with nothing saved: an hour, so the books have a period of sales to bid by.</summary>
    public static int FirstMs { get; set; } = 3600000;

    /// <summary>The levy on a sale of a charter's craft by anybody but its holder: a tenth of the sale.</summary>
    public static double Levy { get; set; } = 0.10;

    /// <summary>The share of a charter's worth over a term a guild with all of the craft's sales would bid. See <see cref="Bid"/>.</summary>
    public static double Appetite { get; set; } = 0.8;

    /// <summary>The most of what a chest spares (<c>BotDues.Spare</c>) one bid may take.</summary>
    public static double SpareShare { get; set; } = 0.5;

    /// <summary>The least share of a town's craft sales a guild's practitioners must have taken to bid for its charter.</summary>
    public static double LeastShare { get; set; } = 0.1;

    /// <summary>A bid under this is no bid.</summary>
    public static int LeastBid { get; set; } = 50;

    /// <summary>The shortest period a book is reckoned over, so an hour's sales are not scaled into a fortune: half an hour.</summary>
    public static int LeastPeriodMs { get; set; } = 1800000;

    /// <summary>Town tasks failed in a row that cost a holder its charters in that town.</summary>
    public static int FailsToLose { get; set; } = 2;

    /// <summary>What a holder that lost its charter at auction thinks of the winner, at once; other losing bidders half of it.</summary>
    public static double LostRegard { get; set; } = -8.0;

    /// <summary>The skill in the craft that makes a seller its practitioner. See <see cref="Practises"/>.</summary>
    public static double CraftSkill { get; set; } = 30.0;

    /// <summary>Whether a seller who does not practise the craft — a hunter with its loot — sells free and outside the book.</summary>
    public static bool LootFree { get; set; } = true;

    /// <summary>Whether a seller says aloud what its levy went to (the voice's own clock still holds it).</summary>
    public static bool SayLevy { get; set; } = true;

    // ---- Counters (cumulative since the start, like every summary number) -----------------------------------------------------

    /// <summary>Auctions held (one town's one craft each), charters sold at them, kept by the holder, rested for want of a bid, held early.</summary>
    public static long Auctions { get; private set; }

    public static long Sold { get; private set; }

    public static long Kept { get; private set; }

    public static long Rested { get; private set; }

    public static long Early { get; private set; }

    /// <summary>Sealed bids made and their gold; gold paid into the towns' purse for charters.</summary>
    public static long Bids { get; private set; }

    public static long BidGold { get; private set; }

    public static long PriceGold { get; private set; }

    /// <summary>Guilds with a share that made no bid: the chest spared too little, or it was voted a war chest; bids a chest could not pay.</summary>
    public static long TooPoor { get; private set; }

    public static long ForWar { get; private set; }

    public static long Short { get; private set; }

    /// <summary>Levies paid to holders' chests, and their gold.</summary>
    public static long Levies { get; private set; }

    public static long LevyGold { get; private set; }

    /// <summary>Levies a seller at war with the holder paid into the town's purse instead, and their gold.</summary>
    public static long ToTown { get; private set; }

    public static long ToTownGold { get; private set; }

    /// <summary>Levies owed that the seller's pack and account could not cover, and the gold short.</summary>
    public static long Unpaid { get; private set; }

    public static long UnpaidGold { get; private set; }

    /// <summary>Sales by a charter's own guild, free of the levy, and their gold.</summary>
    public static long Spared { get; private set; }

    public static long SparedGold { get; private set; }

    /// <summary>Sales of a craft's kind by a seller who does not practise it, free and outside the book. See <see cref="LootFree"/>.</summary>
    public static long LootSpared { get; private set; }

    public static long LootSparedGold { get; private set; }

    /// <summary>Practitioners' sales of a craft in a town, and their gold: the books' intake, whether a charter stood or not.</summary>
    public static long Booked { get; private set; }

    public static long BookedGold { get; private set; }

    /// <summary>Charters taken away: tasks failed in a row, the holder put out of the town, the holder gone.</summary>
    public static long LostFails { get; private set; }

    public static long LostExile { get; private set; }

    public static long LostGone { get; private set; }

    /// <summary>Grievances written against a winner by a loser.</summary>
    public static long Grievances { get; private set; }

    // ---- State ----------------------------------------------------------------------------------------------------------------

    /// <summary>One charter: a town's craft, held by a guild, bought for a price.</summary>
    public sealed class Charter
    {
        public string Town;

        public Craft Craft;

        public string Holder;

        public int Price;

        /// <summary>When it was won, on the wall clock (UTC).</summary>
        public DateTime Since;

        public long Levies;

        public long LevyGold;

        public long Spared;

        public long SparedGold;
    }

    private static readonly Dictionary<(string Town, Craft Craft), Charter> _charters = [];

    /// <summary>The period's book: the gold each guild's practitioners took for a craft in a town ("" for the guildless).</summary>
    private static readonly Dictionary<(string Town, Craft Craft), Dictionary<string, long>> _book = [];

    /// <summary>Charters taken away and waiting for the next minute's early auction.</summary>
    private static readonly List<(string Town, Craft Craft)> _early = [];

    /// <summary>The guild a charter was taken from, barred from bidding for it until the next general auction.</summary>
    private static readonly Dictionary<(string Town, Craft Craft), string> _barred = [];

    private static bool _opened;

    private static long _next;

    private static long _periodStart;

    /// <summary>What the store read back, used when the clock first opens: ms to the next auction, ms into the period.</summary>
    private static bool _restored;

    private static long _restoredNext;

    private static long _restoredElapsed;

    /// <summary>The charters standing.</summary>
    public static IEnumerable<Charter> All => _charters.Values;

    public static int Held => _charters.Count;

    // ---- What a kind is ---------------------------------------------------------------------------------------------------------

    private static readonly Dictionary<Type, sbyte> _kinds = [];

    /// <summary>The craft whose charter covers this kind, if any. Cached per type once the craft books are read.</summary>
    public static bool Of(Type kind, out Craft craft)
    {
        craft = Craft.Smith;

        if (kind == null)
        {
            return false;
        }

        if (!_kinds.TryGetValue(kind, out var c))
        {
            c = Classify(kind, out var sure);

            if (sure)
            {
                _kinds[kind] = c;
            }
        }

        if (c < 0)
        {
            return false;
        }

        craft = (Craft)c;

        return true;
    }

    private static sbyte Classify(Type kind, out bool sure)
    {
        sure = true;

        if (typeof(BaseIngot).IsAssignableFrom(kind))
        {
            return (sbyte)Craft.Smith;
        }

        if (typeof(BaseLeather).IsAssignableFrom(kind) || typeof(Cloth).IsAssignableFrom(kind) || typeof(UncutCloth).IsAssignableFrom(kind)
            || typeof(BoltOfCloth).IsAssignableFrom(kind))
        {
            return (sbyte)Craft.Tailor;
        }

        if (typeof(Log).IsAssignableFrom(kind) || typeof(Board).IsAssignableFrom(kind) || typeof(Shaft).IsAssignableFrom(kind)
            || typeof(Arrow).IsAssignableFrom(kind) || typeof(Bolt).IsAssignableFrom(kind))
        {
            return (sbyte)Craft.Woodsman;
        }

        if (typeof(BasePotion).IsAssignableFrom(kind))
        {
            return (sbyte)Craft.Alchemist;
        }

        if (typeof(SpellScroll).IsAssignableFrom(kind))
        {
            return (sbyte)Craft.Scribe;
        }

        if (typeof(Food).IsAssignableFrom(kind))
        {
            return (sbyte)Craft.Cook;
        }

        try
        {
            // Before content initialisation the books are null; nothing is cached until they can answer.
            if (DefBlacksmithy.CraftSystem == null)
            {
                sure = false;

                return -1;
            }

            if (Made(DefBlacksmithy.CraftSystem, kind))
            {
                return (sbyte)Craft.Smith;
            }

            if (Made(DefTailoring.CraftSystem, kind))
            {
                return (sbyte)Craft.Tailor;
            }

            if (Made(DefBowFletching.CraftSystem, kind) || Made(DefCarpentry.CraftSystem, kind))
            {
                return (sbyte)Craft.Woodsman;
            }

            if (Made(DefAlchemy.CraftSystem, kind))
            {
                return (sbyte)Craft.Alchemist;
            }

            if (Made(DefInscription.CraftSystem, kind))
            {
                return (sbyte)Craft.Scribe;
            }

            if (Made(DefCooking.CraftSystem, kind))
            {
                return (sbyte)Craft.Cook;
            }
        }
        catch
        {
            sure = false;
        }

        return -1;
    }

    private static bool Made(CraftSystem system, Type kind) => system?.CraftItems != null && system.CraftItems.SearchFor(kind) != null;

    /// <summary>Whether this seller practises the craft: <see cref="CraftSkill"/> or more in one of its skills.</summary>
    public static bool Practises(Mobile seller, Craft craft)
    {
        var skills = seller?.Skills;

        if (skills == null)
        {
            return false;
        }

        var least = CraftSkill;

        return craft switch
        {
            Craft.Smith     => skills[SkillName.Blacksmith].Base >= least || skills[SkillName.Mining].Base >= least,
            Craft.Tailor    => skills[SkillName.Tailoring].Base >= least,
            Craft.Woodsman  => skills[SkillName.Fletching].Base >= least || skills[SkillName.Carpentry].Base >= least
                               || skills[SkillName.Lumberjacking].Base >= least,
            Craft.Alchemist => skills[SkillName.Alchemy].Base >= least,
            Craft.Scribe    => skills[SkillName.Inscribe].Base >= least,
            _               => skills[SkillName.Cooking].Base >= least
        };
    }

    /// <summary>The craft named by a word at the door: smith(s), tailor(s), woodsman/woodsmen/carpenter/bowyer/fletcher, alchemist(s), scribe(s), cook(s).</summary>
    public static bool Parse(string word, out Craft craft)
    {
        craft = Craft.Smith;

        switch ((word ?? "").Trim().TrimEnd('\'').ToLowerInvariant())
        {
            case "smith":
            case "smiths":
            case "metal":
                {
                    craft = Craft.Smith;

                    return true;
                }
            case "tailor":
            case "tailors":
            case "cloth":
                {
                    craft = Craft.Tailor;

                    return true;
                }
            case "woodsman":
            case "woodsmen":
            case "woodsmens":
            case "wood":
            case "carpenter":
            case "carpenters":
            case "bowyer":
            case "bowyers":
            case "fletcher":
            case "fletchers":
                {
                    craft = Craft.Woodsman;

                    return true;
                }
            case "alchemist":
            case "alchemists":
            case "potions":
                {
                    craft = Craft.Alchemist;

                    return true;
                }
            case "scribe":
            case "scribes":
            case "scrolls":
                {
                    craft = Craft.Scribe;

                    return true;
                }
            case "cook":
            case "cooks":
            case "food":
                {
                    craft = Craft.Cook;

                    return true;
                }
            default:
                {
                    return false;
                }
        }
    }

    /// <summary>The guild holding this town's charter for this craft, or null. For other folders (politics, officers).</summary>
    public static string Holder(string town, Craft craft) =>
        town != null && _charters.TryGetValue((town, craft), out var charter) ? charter.Holder : null;

    /// <summary>How many charters this guild holds.</summary>
    public static int HeldBy(string guild)
    {
        if (guild == null)
        {
            return 0;
        }

        var n = 0;

        foreach (var charter in _charters.Values)
        {
            if (charter.Holder == guild)
            {
                n++;
            }
        }

        return n;
    }

    // ---- The levy ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// A sale at a bot's shop or to the city there, split at the till: returns the levy, already in the holder's chest (or the town's
    /// purse from an enemy), which the caller keeps back from what it pays the keeper. Nought when no charter applies. Called from
    /// <c>BotShopkeep.Serve</c> and <c>BotShopkeep.CityBuy</c> before the keeper is paid.
    /// </summary>
    public static int Split(Mobile seller, string town, Type kind, int units, int gold)
    {
        if (!Running || seller == null || town == null || gold <= 0 || !Of(kind, out var craft))
        {
            return 0;
        }

        var levy = Reckon(seller, town, craft, gold, out var charter, out var enemy);

        if (levy <= 0)
        {
            return 0;
        }

        Deliver(seller, charter, enemy, craft, units, kind, gold, levy, "at the till");

        return levy;
    }

    /// <summary>
    /// A sale over a shopkeeper's counter: each line's share of what the counter measured paid is reckoned by craft, and the levies
    /// taken out of the seller's pack, then account, where the coin just went. Returns what was paid in levies. Called from the end
    /// of <c>BotShops.Sell</c>.
    /// </summary>
    public static int Counter(Mobile seller, Point3D at, List<(Type Kind, int Amount, int Paying)> lines, int earned)
    {
        if (!Running || seller == null || lines == null || lines.Count == 0 || earned <= 0)
        {
            return 0;
        }

        var town = BotTowns.Of(at)?.Name;

        if (town == null)
        {
            return 0;
        }

        Span<long> weight = stackalloc long[Crafts];
        Span<int> units = stackalloc int[Crafts];
        Span<int> kindOf = stackalloc int[Crafts];
        long whole = 0;
        var any = false;

        for (var i = 0; i < Crafts; i++)
        {
            kindOf[i] = -1;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var (kind, amount, paying) = lines[i];
            var line = (long)Math.Max(0, amount) * Math.Max(0, paying);

            whole += line;

            if (Of(kind, out var craft))
            {
                weight[(int)craft] += line;
                units[(int)craft] += amount;

                if (kindOf[(int)craft] < 0)
                {
                    kindOf[(int)craft] = i;
                }

                any = true;
            }
        }

        if (!any || whole <= 0)
        {
            return 0;
        }

        var paid = 0;

        for (var c = 0; c < Crafts; c++)
        {
            if (weight[c] <= 0)
            {
                continue;
            }

            var craft = (Craft)c;
            var gold = (int)(earned * weight[c] / whole);
            var levy = Reckon(seller, town, craft, gold, out var charter, out var enemy);

            if (levy <= 0)
            {
                continue;
            }

            var taken = Take(seller, levy);

            if (taken > 0)
            {
                Deliver(seller, charter, enemy, craft, units[c], lines[kindOf[c]].Kind, gold, taken, "over the counter");
                paid += taken;
            }
        }

        return paid;
    }

    /// <summary>
    /// A sale already paid in full to the seller (a tavern's meal): the levy taken out of its pack, then account. Returns it.
    /// Called from <c>BotTaverns.Serve</c>.
    /// </summary>
    public static int After(Mobile seller, Point3D at, Type kind, int units, int gold)
    {
        if (!Running || seller == null || gold <= 0 || !Of(kind, out var craft))
        {
            return 0;
        }

        var town = BotTowns.Of(at)?.Name;

        if (town == null)
        {
            return 0;
        }

        var levy = Reckon(seller, town, craft, gold, out var charter, out var enemy);

        if (levy <= 0)
        {
            return 0;
        }

        var taken = Take(seller, levy);

        if (taken > 0)
        {
            Deliver(seller, charter, enemy, craft, units, kind, gold, taken, "at the table");
        }

        return taken;
    }

    /// <summary>
    /// Books a practitioner's sale and says what levy it owes: nought for a seller who does not practise the craft (when
    /// <see cref="LootFree"/>), where no charter stands, and for the holder's own sale.
    /// </summary>
    private static int Reckon(Mobile seller, string town, Craft craft, int gold, out Charter charter, out bool enemy)
    {
        charter = null;
        enemy = false;

        if (gold <= 0)
        {
            return 0;
        }

        if (LootFree && !Practises(seller, craft))
        {
            LootSpared++;
            LootSparedGold += gold;

            return 0;
        }

        var mine = seller.Guild as Guild;
        var guild = mine is { Disbanded: false } ? mine.Name : null;

        Book(town, craft, guild ?? "", gold);

        if (!_charters.TryGetValue((town, craft), out var held))
        {
            return 0;
        }

        if (guild != null && guild == held.Holder)
        {
            held.Spared++;
            held.SparedGold += gold;
            Spared++;
            SparedGold += gold;

            return 0;
        }

        var levy = (int)(gold * Math.Clamp(Levy, 0.0, 1.0));

        if (levy <= 0)
        {
            return 0;
        }

        charter = held;
        // Nothing crosses between hostile guilds (Patrick's standing rule): at war, or either at or under the war line of regard.
        enemy = mine != null && BaseGuild.FindByName(held.Holder) is Guild holder
                && (BotRegard.AtWar(mine, holder) || BotRegard.Of(mine, holder) <= BotRegard.Enmity || BotRegard.Of(holder, mine) <= BotRegard.Enmity);

        return levy;
    }

    private static void Book(string town, Craft craft, string guild, int gold)
    {
        var key = (town, craft);

        if (!_book.TryGetValue(key, out var book))
        {
            _book[key] = book = new Dictionary<string, long>(StringComparer.Ordinal);
        }

        book[guild] = book.GetValueOrDefault(guild) + gold;
        Booked++;
        BookedGold += gold;
    }

    /// <summary>Coin out of a seller's pack, then its account: as much of <paramref name="levy"/> as it holds. What fell short is counted.</summary>
    private static int Take(Mobile seller, int levy)
    {
        var pack = seller.Backpack;
        var fromPack = pack == null ? 0 : Math.Min(pack.GetAmount(typeof(Gold)), levy);

        if (fromPack > 0 && !pack.ConsumeTotal(typeof(Gold), fromPack))
        {
            fromPack = 0;
        }

        var fromBank = Math.Min(levy - fromPack, Banker.GetBalance(seller));

        if (fromBank > 0 && !Banker.Withdraw(seller, fromBank))
        {
            fromBank = 0;
        }

        var taken = fromPack + fromBank;

        if (taken < levy)
        {
            Unpaid++;
            UnpaidGold += levy - taken;
        }

        return taken;
    }

    /// <summary>The levy where it belongs: the holder's chest, or the town's purse when the seller's guild is hostile to the holder.</summary>
    private static void Deliver(Mobile seller, Charter charter, bool enemy, Craft craft, int units, Type kind, int gold, int levy, string where)
    {
        if (enemy)
        {
            BotCity.Tax(levy);
            ToTown++;
            ToTownGold += levy;

            logger.Information(
                "{Seller} paid {Town}'s purse a {Levy}gp levy on {Units} {Kind} sold {Where} for {Gold}gp: the {Craft} charter is held by {Holder}, hostile to its guild",
                seller.Name,
                charter.Town,
                levy,
                units,
                kind?.Name,
                where,
                gold,
                Word(craft),
                charter.Holder
            );

            return;
        }

        BotChest.Put(charter.Holder, levy);
        charter.Levies++;
        charter.LevyGold += levy;
        Levies++;
        LevyGold += levy;

        logger.Information(
            "{Seller} paid {Holder} a {Levy}gp levy on {Units} {Kind} sold {Where} in {Town} for {Gold}gp: the {Craft} charter's {Rate:P0}; {Chest}gp in their chest",
            seller.Name,
            charter.Holder,
            levy,
            units,
            kind?.Name,
            where,
            charter.Town,
            gold,
            Word(craft),
            Levy,
            BotChest.Holds(charter.Holder)
        );

        if (SayLevy)
        {
            var line = BotVoice.Phrase(
                "charter:levy",
                null,
                seller,
                new Dictionary<string, string>
                {
                    ["levy"] = levy.ToString(),
                    ["holder"] = charter.Holder,
                    ["craft"] = Word(craft),
                    ["town"] = charter.Town,
                    ["rate"] = $"{Levy:P0}"
                }
            );

            if (line != null)
            {
                BotVoice.Say(seller, "trade", line);
            }
        }
    }

    // ---- The auction ------------------------------------------------------------------------------------------------------------

    /// <summary>One guild's sealed bid.</summary>
    private readonly record struct Offer(string Guild, int Bid, double Share, long Sales, int Spare);

    /// <summary>The clock, once a minute from the module: the first opening, holders gone or put out, early auctions, the general one.</summary>
    public static void Look(long now)
    {
        if (!_opened)
        {
            _opened = true;

            if (_restored)
            {
                _next = now + Math.Max(60000L, _restoredNext);
                _periodStart = now - Math.Max(0L, _restoredElapsed);
            }
            else
            {
                _next = now + Math.Max(60000, FirstMs);
                _periodStart = now;
            }
        }

        // The towns' reach is asked of the gates after the survey; a general auction before it would find no town to hold one in.
        if (!Running || !BotTowns.Ready)
        {
            return;
        }

        Police();

        if (now - _next >= 0)
        {
            General(now, "the town's clock");

            return;
        }

        if (_early.Count == 0)
        {
            return;
        }

        List<(string Town, Craft Craft)> due = [.. _early];

        _early.Clear();

        for (var i = 0; i < due.Count; i++)
        {
            var town = BotTowns.Find(due[i].Town);

            if (town != null && !_charters.ContainsKey(due[i]))
            {
                Early++;
                Auction(town, due[i].Craft, now, false, "early: the charter was taken from its holder");
            }
        }
    }

    /// <summary>Holders that no longer exist, and holders put out of the town, lose their charters there.</summary>
    private static void Police()
    {
        if (_charters.Count == 0)
        {
            return;
        }

        List<((string Town, Craft Craft) Key, string Why, bool Exile)> lost = null;

        foreach (var (key, charter) in _charters)
        {
            if (BaseGuild.FindByName(charter.Holder) is not Guild guild || guild.Disbanded)
            {
                (lost ??= []).Add((key, "the guild is gone", false));

                continue;
            }

            if (BotBurgh.Bars(guild, BotTowns.Find(key.Town)))
            {
                (lost ??= []).Add((key, "the town has put the guild out", true));
            }
        }

        if (lost == null)
        {
            return;
        }

        for (var i = 0; i < lost.Count; i++)
        {
            var (key, why, exile) = lost[i];

            if (exile)
            {
                LostExile++;
            }
            else
            {
                LostGone++;
            }

            Revoke(key, why, exile);
        }
    }

    /// <summary>
    /// A town task failed by a guild: at <see cref="FailsToLose"/> in a row it loses its charters in that town, to an early auction it
    /// may not bid at. Called from <c>BotBurgh.Fail</c> after the count is raised.
    /// </summary>
    public static void Failed(string guild, string town, int inRow)
    {
        if (!Running || guild == null || town == null || FailsToLose <= 0 || inRow < FailsToLose)
        {
            return;
        }

        for (var c = 0; c < Crafts; c++)
        {
            var key = (town, (Craft)c);

            if (_charters.TryGetValue(key, out var charter) && charter.Holder == guild)
            {
                LostFails++;
                Revoke(key, $"it failed {inRow} of the town's tasks in a row", true);
            }
        }
    }

    private static void Revoke((string Town, Craft Craft) key, string why, bool bar)
    {
        if (!_charters.Remove(key, out var charter))
        {
            return;
        }

        if (bar)
        {
            _barred[key] = charter.Holder;
        }

        if (!_early.Contains(key))
        {
            _early.Add(key);
        }

        logger.Warning(
            "{Town} took the {Craft} charter from {Holder}: {Why}; held since {Since:HH:mm} UTC for {Price}gp, {Levies} levies of {Gold}gp taken; it goes to an early auction{Barred}",
            key.Town,
            Word(key.Craft),
            charter.Holder,
            why,
            charter.Since,
            charter.Price,
            charter.Levies,
            charter.LevyGold,
            bar ? $", {charter.Holder} barred from it" : ""
        );

        BotVoice.ToGuild(
            charter.Holder,
            "charter:revoked",
            new Dictionary<string, string> { ["town"] = key.Town, ["craft"] = Word(key.Craft), ["why"] = why }
        );

        BotEvents.Post("charter", null, null, charter.Holder, 0, 0, $"{key.Town} took the {Word(key.Craft)} charter from {charter.Holder}: {why}", null, BotJson.Object(w => w.WriteString("outcome", "revoked")));
    }

    /// <summary>Every town's crafts under the hammer, the books closed and a new period opened.</summary>
    public static int General(long now, string why)
    {
        var held = 0;
        var towns = BotTowns.All;

        for (var t = 0; t < towns.Count; t++)
        {
            var town = towns[t];

            if (!town.FromHome && !town.BySea)
            {
                continue;
            }

            for (var c = 0; c < Crafts; c++)
            {
                var key = (town.Name, (Craft)c);

                if (!_charters.ContainsKey(key) && !_book.ContainsKey(key))
                {
                    continue;
                }

                held++;
                Auction(town, (Craft)c, now, true, why);
            }
        }

        _book.Clear();
        _barred.Clear();
        _early.Clear();
        _periodStart = now;
        _next = now + Math.Max(60000, EveryMs);

        logger.Information(
            "The towns auctioned their charters ({Why}): {Held} crafts under the hammer, {Charters} charters standing; the next auction in {Minutes} minutes",
            why,
            held,
            _charters.Count,
            Math.Max(60000, EveryMs) / 60000
        );

        return held;
    }

    /// <summary>
    /// One town's charter for one craft auctioned now: the bids reckoned from the period's book, the highest paid to the town's purse,
    /// the losers' grievances written. Returns what happened, in one line. A general auction's charter runs a whole term; an early or
    /// hand-called one until the next general auction.
    /// </summary>
    public static string Auction(BotTowns.Town town, Craft craft, long now, bool general, string why)
    {
        var key = (town.Name, craft);

        _charters.TryGetValue(key, out var prev);
        _book.TryGetValue(key, out var book);
        _barred.TryGetValue(key, out var barred);

        Auctions++;

        long total = 0;

        if (book != null)
        {
            foreach (var gold in book.Values)
            {
                total += gold;
            }
        }

        var elapsed = Math.Max(Math.Max(60000L, LeastPeriodMs), now - _periodStart);
        var term = general ? Math.Max(60000L, EveryMs) : Math.Max(60000L, _next - now);
        var worth = total * Math.Clamp(Levy, 0.0, 1.0) * term / elapsed;

        List<Offer> offers = [];

        if (book != null && total > 0)
        {
            foreach (var (name, gold) in book)
            {
                if (name.Length == 0 || name == barred)
                {
                    continue;
                }

                var share = (double)gold / total;

                if (share < LeastShare)
                {
                    continue;
                }

                if (BaseGuild.FindByName(name) is not Guild guild || guild.Disbanded || BotUnderworld.Band(guild) || BotBurgh.Bars(guild, town))
                {
                    continue;
                }

                // The head decides by the chest's purpose: a chest the members voted a war chest bids nothing while it is one.
                if (BotPurpose.Goal(guild, out var what, out var aim) && aim > 0 && what == BotPurpose.Word(BotPurpose.Aim.WarChest))
                {
                    ForWar++;

                    continue;
                }

                var spare = BotDues.Spare(guild);
                var bid = Bid(worth, share, spare);

                if (bid < Math.Max(1, LeastBid))
                {
                    TooPoor++;

                    continue;
                }

                offers.Add(new Offer(name, bid, share, gold, spare));
                Bids++;
                BidGold += bid;
            }
        }

        offers.Sort(static (a, b) => b.Bid != a.Bid ? b.Bid.CompareTo(a.Bid) : b.Share.CompareTo(a.Share));

        Offer won = default;
        var paid = 0;

        for (var i = 0; i < offers.Count; i++)
        {
            var offer = offers[i];

            if (BotChest.Holds(offer.Guild) < offer.Bid)
            {
                Short++;

                continue;
            }

            paid = BotChest.Draw(offer.Guild, offer.Bid);

            if (paid < offer.Bid)
            {
                BotChest.Return(offer.Guild, paid);
                Short++;
                paid = 0;

                continue;
            }

            won = offer;

            break;
        }

        var said = Bidding(offers);

        if (won.Guild == null)
        {
            if (prev != null)
            {
                _charters.Remove(key);
            }

            Rested++;

            logger.Information(
                "{Town}'s {Craft} charter rests ({Why}): {Total}gp of the craft's sales this period, worth {Worth}gp a term at the {Rate:P0} levy; {Bids}{Prev}",
                town.Name,
                Word(craft),
                why,
                total,
                (long)worth,
                Levy,
                offers.Count == 0 ? "no guild bid" : $"no bid could be paid ({said})",
                prev != null ? $"; {prev.Holder} held it and holds it no longer" : ""
            );

            if (prev != null)
            {
                BotVoice.ToGuild(prev.Holder, "charter:rests", new Dictionary<string, string> { ["town"] = town.Name, ["craft"] = Word(craft) });
            }

            return $"{town.Name}'s {Word(craft)} charter rests: {total}gp of sales this period, worth {(long)worth}gp a term; {(offers.Count == 0 ? "no bid" : said)}.";
        }

        BotCity.Tax(paid);
        PriceGold += paid;

        var kept = prev != null && prev.Holder == won.Guild;

        if (kept)
        {
            Kept++;
        }
        else
        {
            Sold++;
        }

        _charters[key] = new Charter { Town = town.Name, Craft = craft, Holder = won.Guild, Price = paid, Since = Core.Now };

        logger.Information(
            "{Guild} won {Town}'s {Craft} charter ({Why}) for {Price}gp into the town's purse: its practitioners took {Share:P0} of the craft's {Total}gp this period, worth {Worth}gp a term; the bids: {Bids}{Prev}",
            won.Guild,
            town.Name,
            Word(craft),
            why,
            paid,
            won.Share,
            total,
            (long)worth,
            said,
            prev == null ? "; nobody held it" : kept ? "; it keeps it" : $"; taken from {prev.Holder}"
        );

        // The losers think worse of the winner: the old holder at the full weight, whether it bid or not, other bidders at half.
        if (prev != null && !kept)
        {
            Grieve(prev.Holder, won.Guild, LostRegard);
        }

        for (var i = 0; i < offers.Count; i++)
        {
            var loser = offers[i].Guild;

            if (loser != won.Guild && (prev == null || loser != prev.Holder))
            {
                Grieve(loser, won.Guild, LostRegard * 0.5);
            }
        }

        var fill = new Dictionary<string, string>
        {
            ["town"] = town.Name,
            ["craft"] = Word(craft),
            ["price"] = paid.ToString(),
            ["winner"] = won.Guild,
            ["rate"] = $"{Levy:P0}"
        };

        Speak(won.Guild, kept ? "charter:kept" : "charter:won", fill);

        if (prev != null && !kept)
        {
            BotVoice.ToGuild(prev.Holder, "charter:lost", new Dictionary<string, string>(fill));
        }

        BotEvents.Post("charter", null, null, won.Guild, town.Square.X, town.Square.Y, $"{won.Guild} holds the {Word(craft)} charter of {town.Name}: {paid}gp to the town", null, BotJson.Object(w => w.WriteString("outcome", kept ? "kept" : "won")));

        return $"{won.Guild} holds {town.Name}'s {Word(craft)} charter for {paid}gp ({won.Share:P0} of {total}gp of sales; bids: {said}).";
    }

    /// <summary>
    /// A guild's sealed bid: <see cref="Appetite"/> × (½ + ½ × share) of the charter's worth over its term, never more than
    /// <see cref="SpareShare"/> of what the chest spares.
    /// </summary>
    public static int Bid(double worth, double share, int spare)
    {
        if (worth <= 0.0 || spare <= 0)
        {
            return 0;
        }

        var want = worth * Math.Clamp(Appetite, 0.0, 10.0) * (0.5 + 0.5 * Math.Clamp(share, 0.0, 1.0));
        var most = spare * Math.Clamp(SpareShare, 0.0, 1.0);

        return (int)Math.Min(want, most);
    }

    private static string Bidding(List<Offer> offers)
    {
        if (offers.Count == 0)
        {
            return "none";
        }

        using var say = ValueStringBuilder.Create(128);

        for (var i = 0; i < offers.Count && i < 6; i++)
        {
            var o = offers[i];

            if (i > 0)
            {
                say.Append(", ");
            }

            say.Append($"{o.Guild} {o.Bid}gp ({o.Share:P0}, spares {o.Spare}gp)");
        }

        if (offers.Count > 6)
        {
            say.Append($" and {offers.Count - 6} more");
        }

        return say.ToString();
    }

    private static void Grieve(string loser, string winner, double by)
    {
        if (loser == null || winner == null || loser == winner || by >= 0.0)
        {
            return;
        }

        BotRegard.Move(loser, winner, by, "a charter lost at auction");
        Grievances++;
    }

    /// <summary>The guild's head (or any living member) says it on the trade channel, for the whole island; the guild's channel when none can.</summary>
    private static void Speak(string guildName, string occasion, Dictionary<string, string> fill)
    {
        if (BaseGuild.FindByName(guildName) is not Guild guild)
        {
            return;
        }

        fill.TryAdd("guild", guild.Name);

        Mobile speaker = null;

        if (guild.Leader is { Deleted: false, Alive: true } leader && leader.Map != null && leader.Map != Map.Internal)
        {
            speaker = leader;
        }
        else
        {
            var members = guild.Members;

            for (var i = 0; i < members.Count; i++)
            {
                if (members[i] is { Deleted: false, Alive: true } m && m.Map != null && m.Map != Map.Internal)
                {
                    speaker = m;

                    break;
                }
            }
        }

        var line = speaker == null ? null : BotVoice.Phrase(occasion, null, speaker, fill);

        if (line == null || !BotVoice.Say(speaker, "trade", line, force: true))
        {
            BotVoice.ToGuild(guild.Name, occasion, fill);
        }
    }

    /// <summary>The door's <c>charter &lt;town&gt; &lt;craft&gt;</c>: that town's charter auctioned now, until the next general auction.</summary>
    public static string ByHand(string tail)
    {
        var words = (tail ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length < 2 || !Parse(words[^1], out var craft))
        {
            return "charter <town> <craft>: the craft is one of smith, tailor, woodsman, alchemist, scribe, cook.";
        }

        var name = string.Join(' ', words, 0, words.Length - 1);
        var town = BotTowns.Find(name);

        if (town == null)
        {
            return $"I know no town called \"{name}\".";
        }

        var now = Core.TickCount;

        if (!_opened)
        {
            Look(now);
        }

        return Auction(town, craft, now, false, "called by hand");
    }

    // ---- Telling it ---------------------------------------------------------------------------------------------------------------

    /// <summary>The door's <c>charters</c>: every charter standing, the next auction, and each town's book this period.</summary>
    public static string Tell()
    {
        using var say = ValueStringBuilder.Create(1024);

        say.Append($"{_charters.Count} charters standing");

        if (_charters.Count > 0)
        {
            say.Append(": ");
            say.Append(Standing(40));
        }

        say.Append($". Next auction in {(_opened ? Math.Max(0, (_next - Core.TickCount) / 60000) : -1)} minutes; ");
        say.Append($"the levy {Levy:P0}, a bid {Appetite:0.##} × (½ + ½ × share) of the worth, at most {SpareShare:P0} of what a chest spares, from {LeastShare:P0} of the sales. ");
        say.Append("This period's books: ");
        say.Append(Books(30));

        say.Append($". {Describe()}");

        return say.ToString();
    }

    private static string Standing(int most)
    {
        using var say = ValueStringBuilder.Create(256);
        var n = 0;

        foreach (var charter in _charters.Values)
        {
            if (n >= most)
            {
                say.Append($" and {_charters.Count - most} more");

                break;
            }

            if (n > 0)
            {
                say.Append("; ");
            }

            say.Append(
                $"{charter.Town} {Key(charter.Craft)}: {charter.Holder} for {charter.Price}gp, {charter.Levies} levies of {charter.LevyGold}gp, own {charter.Spared} sales of {charter.SparedGold}gp"
            );

            n++;
        }

        return say.ToString();
    }

    private static string Books(int most)
    {
        if (_book.Count == 0)
        {
            return "empty";
        }

        using var say = ValueStringBuilder.Create(256);
        var n = 0;

        foreach (var (key, book) in _book)
        {
            if (n >= most)
            {
                say.Append($" and {_book.Count - most} more");

                break;
            }

            long total = 0;
            string top = null;
            long topGold = 0;

            foreach (var (guild, gold) in book)
            {
                total += gold;

                if (guild.Length > 0 && gold > topGold)
                {
                    top = guild;
                    topGold = gold;
                }
            }

            if (n > 0)
            {
                say.Append(", ");
            }

            if (top == null)
            {
                say.Append($"{key.Town} {Key(key.Craft)} {total}gp");
            }
            else
            {
                say.Append($"{key.Town} {Key(key.Craft)} {total}gp ({top} {(double)topGold / Math.Max(1, total):P0})");
            }

            n++;
        }

        return say.ToString();
    }

    /// <summary>The <c>Charters:</c> line, every five minutes.</summary>
    public static string Describe()
    {
        using var say = ValueStringBuilder.Create(512);

        say.Append(Running ? "" : "OFF; ");
        say.Append($"{_charters.Count} held");

        if (_charters.Count > 0)
        {
            say.Append(" (");
            say.Append(Standing(12));
            say.Append(')');
        }

        say.Append(
            $"; auctions {Auctions} ({Sold} sold, {Kept} kept, {Rested} rested, {Early} early), {Bids} bids of {BidGold}gp, {PriceGold}gp into the towns' purse"
            + $" ({TooPoor} chests too poor to bid, {ForWar} kept for a war chest, {Short} bids the chest could not pay)"
            + $"; levies {Levies} of {LevyGold}gp to the holders, {ToTown} of {ToTownGold}gp to a town's purse from the holder's enemies, {Unpaid} short by {UnpaidGold}gp"
            + $"; the holders' own sales {Spared} of {SparedGold}gp free, non-practitioners' {LootSpared} of {LootSparedGold}gp free"
            + $"; booked {Booked} sales of {BookedGold}gp; lost for failed tasks {LostFails}, for exile {LostExile}, the guild gone {LostGone}; grievances {Grievances}"
            + $"; next auction in {(_opened ? Math.Max(0, (_next - Core.TickCount) / 60000) : -1)} min"
        );

        return say.ToString();
    }

    /// <summary>The town's charters inside its object in <c>/api/state</c>'s <c>towns</c>. From <c>BotWebSnapshot.WriteTowns</c>.</summary>
    public static void Write(Utf8JsonWriter w, string town)
    {
        w.WritePropertyName("charters");
        w.WriteStartArray();

        if (town != null)
        {
            for (var c = 0; c < Crafts; c++)
            {
                var key = (town, (Craft)c);
                var held = _charters.TryGetValue(key, out var charter);
                _book.TryGetValue(key, out var book);

                if (!held && book == null)
                {
                    continue;
                }

                long total = 0;

                if (book != null)
                {
                    foreach (var gold in book.Values)
                    {
                        total += gold;
                    }
                }

                w.WriteStartObject();
                w.WriteString("craft", Key((Craft)c));
                w.WriteString("name", $"the {Word((Craft)c)} charter of {town}");
                BotJson.StringOrNull(w, "holder", held ? charter.Holder : null);
                w.WriteNumber("price", held ? charter.Price : 0);
                BotJson.StringOrNull(w, "since", held ? charter.Since.ToString("O") : null);
                w.WriteNumber("levies", held ? charter.Levies : 0);
                w.WriteNumber("levyGold", held ? charter.LevyGold : 0);
                w.WriteNumber("spared", held ? charter.Spared : 0);
                w.WriteNumber("sparedGold", held ? charter.SparedGold : 0);
                w.WriteNumber("periodGold", total);
                w.WriteEndObject();
            }
        }

        w.WriteEndArray();
    }

    // ---- Keeping it ----------------------------------------------------------------------------------------------------------------

    internal static void Save(IGenericWriter writer)
    {
        var now = Core.TickCount;
        var next = _opened ? Math.Max(0L, _next - now) : _restored ? _restoredNext : -1L;
        var elapsed = _opened ? Math.Max(0L, now - _periodStart) : _restored ? _restoredElapsed : 0L;

        writer.Write(next);
        writer.Write(elapsed);

        writer.WriteEncodedInt(_charters.Count);

        foreach (var charter in _charters.Values)
        {
            writer.Write(charter.Town);
            writer.WriteEncodedInt((int)charter.Craft);
            writer.Write(charter.Holder);
            writer.WriteEncodedInt(charter.Price);
            writer.Write(charter.Since);
            writer.Write(charter.Levies);
            writer.Write(charter.LevyGold);
            writer.Write(charter.Spared);
            writer.Write(charter.SparedGold);
        }

        writer.WriteEncodedInt(_book.Count);

        foreach (var (key, book) in _book)
        {
            writer.Write(key.Town);
            writer.WriteEncodedInt((int)key.Craft);
            writer.WriteEncodedInt(book.Count);

            foreach (var (guild, gold) in book)
            {
                writer.Write(guild);
                writer.Write(gold);
            }
        }

        writer.WriteEncodedInt(_barred.Count);

        foreach (var (key, guild) in _barred)
        {
            writer.Write(key.Town);
            writer.WriteEncodedInt((int)key.Craft);
            writer.Write(guild);
        }
    }

    internal static (int Charters, int Books) Load(IGenericReader reader)
    {
        _charters.Clear();
        _book.Clear();
        _barred.Clear();

        var next = reader.ReadLong();
        var elapsed = reader.ReadLong();

        if (next >= 0)
        {
            _restored = true;
            _restoredNext = next;
            _restoredElapsed = elapsed;
        }

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var charter = new Charter
            {
                Town = reader.ReadString(),
                Craft = (Craft)Math.Clamp(reader.ReadEncodedInt(), 0, Crafts - 1),
                Holder = reader.ReadString(),
                Price = reader.ReadEncodedInt(),
                Since = reader.ReadDateTime(),
                Levies = reader.ReadLong(),
                LevyGold = reader.ReadLong(),
                Spared = reader.ReadLong(),
                SparedGold = reader.ReadLong()
            };

            if (!string.IsNullOrEmpty(charter.Town) && !string.IsNullOrEmpty(charter.Holder))
            {
                _charters[(charter.Town, charter.Craft)] = charter;
            }
        }

        var books = reader.ReadEncodedInt();

        for (var i = 0; i < books; i++)
        {
            var town = reader.ReadString();
            var craft = (Craft)Math.Clamp(reader.ReadEncodedInt(), 0, Crafts - 1);
            var entries = reader.ReadEncodedInt();
            var book = new Dictionary<string, long>(StringComparer.Ordinal);

            for (var e = 0; e < entries; e++)
            {
                var guild = reader.ReadString() ?? "";
                var gold = reader.ReadLong();

                if (gold > 0)
                {
                    book[guild] = gold;
                }
            }

            if (!string.IsNullOrEmpty(town) && book.Count > 0)
            {
                _book[(town, craft)] = book;
            }
        }

        var barred = reader.ReadEncodedInt();

        for (var i = 0; i < barred; i++)
        {
            var town = reader.ReadString();
            var craft = (Craft)Math.Clamp(reader.ReadEncodedInt(), 0, Crafts - 1);
            var guild = reader.ReadString();

            if (!string.IsNullOrEmpty(town) && !string.IsNullOrEmpty(guild))
            {
                _barred[(town, craft)] = guild;
            }
        }

        return (_charters.Count, _book.Count);
    }

    /// <summary>The session's counters; the charters, the books and the clock are the store's and are kept.</summary>
    public static void Forget()
    {
        Auctions = 0;
        Sold = 0;
        Kept = 0;
        Rested = 0;
        Early = 0;
        Bids = 0;
        BidGold = 0;
        PriceGold = 0;
        TooPoor = 0;
        ForWar = 0;
        Short = 0;
        Levies = 0;
        LevyGold = 0;
        ToTown = 0;
        ToTownGold = 0;
        Unpaid = 0;
        UnpaidGold = 0;
        Spared = 0;
        SparedGold = 0;
        LootSpared = 0;
        LootSparedGold = 0;
        Booked = 0;
        BookedGold = 0;
        LostFails = 0;
        LostExile = 0;
        LostGone = 0;
        Grievances = 0;
    }

    /// <summary>
    /// <b>Patrick's reset (07.10.2026, reset26): every charter let go, so the save writes the charters empty.</b> <see cref="Forget"/>
    /// keeps the charters, the period's books and the clock on purpose, for a reload; a reset that kept them left the old guilds'
    /// names holding the towns' crafts in <c>Saves/BotCharters</c>, levying a population that never bid for them. This empties
    /// exactly what <see cref="Save"/> writes — the charters, the books, the guilds barred — and the early auctions waiting, and
    /// opens the clock again as at a first start, so the first auction comes <see cref="FirstMs"/> after the next look.
    ///
    /// <para>
    /// <b>No coin is held here.</b> A charter's price went into the city's purse when it was won (<c>BotCity.Tax</c>) and every
    /// levy since went to the holder's chest and the purse at once; the charter is a right, not a purse, so letting it go moves no
    /// gold and leaves nothing counted anywhere. Returns the charters let go.
    /// </para>
    /// </summary>
    public static int Wipe()
    {
        var held = _charters.Count;

        _charters.Clear();
        _book.Clear();
        _early.Clear();
        _barred.Clear();
        _opened = false;
        _restored = false;
        _restoredNext = 0;
        _restoredElapsed = 0;
        Forget();

        return held;
    }

    /// <summary>What the bots say about it, put into the voice's bank wherever the bank has nothing for the occasion.</summary>
    public static Dictionary<string, string[]> Phrases() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["charter:won"] =
            [
                "{guild} holds the {craft} charter of {town}.",
                "The {craft} charter of {town} is ours: {price}gp to the town. Sell there, and {rate} of it comes to us.",
                "{town}'s {craft} charter goes to {guild}. Hear it, every trader."
            ],
            ["charter:kept"] = ["{guild} keeps the {craft} charter of {town}: {price}gp to the town again.", "The {craft} charter of {town} stays with {guild}."],
            ["charter:lost"] = ["{winner} took the {craft} charter of {town} from us. We will remember it.", "{town} sold our {craft} charter to {winner}. That is a grievance."],
            ["charter:revoked"] = ["{town} has taken the {craft} charter from us: {why}.", "We lost the {craft} charter of {town}. {why}."],
            ["charter:rests"] = ["Nobody bid for the {craft} charter of {town}; it rests.", "The {craft} charter of {town} is nobody's now."],
            ["charter:levy"] = ["{levy}gp of that to {holder}: they hold the {craft} charter of {town}.", "{rate} to {holder}. Their charter, their {town}."]
        };
}
