using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>Where a relic is: nowhere yet, in a lair's chest, in a bot's pack, on a hall's floor, kept by a guild with no hall, or stolen.</summary>
public enum BotRelicPlace : byte
{
    Hidden,
    Lair,
    Carried,
    Hall,
    Kept,
    Stolen
}

/// <summary>One relic in the registry: what it is, the item that is it, and whose it is since when.</summary>
public sealed class BotRelic
{
    /// <summary><c>crown</c>, <c>bell</c>, <c>anchor</c>, <c>lantern</c>, <c>scales</c>, or <c>banner:&lt;guild&gt;</c>.</summary>
    public string Key;

    public string Name;

    /// <summary>The town whose relic this is (prices there are lower for its holder), or null for a banner.</summary>
    public string Town;

    /// <summary>The guild whose banner this is, or null for a town relic.</summary>
    public string Of;

    public int ItemId;

    public int Hue;

    public BotRelicPlace Place;

    /// <summary>The holding guild: in its hall, kept by it, or carried by one of its members. Null when nobody's or a thief's.</summary>
    public string Guild;

    /// <summary>The carrier or the thief, or <see cref="Serial.MinusOne"/>.</summary>
    public Serial Bot = Serial.MinusOne;

    /// <summary>The guild a stolen relic was taken from, or the guild that lost a banner in a war.</summary>
    public string Former;

    /// <summary>When the holder took it, UTC.</summary>
    public DateTime Since = DateTime.UtcNow;

    /// <summary>When its holder last called the Great Fair with it, UTC.</summary>
    public DateTime LastFair = DateTime.MinValue;

    /// <summary>Times it has changed hands.</summary>
    public int Hands;

    public BotRelicItem Item;

    public bool IsBanner => Of != null;

    /// <summary>Whether a guild or a bot holds it openly — the one state its gifts flow from. A thief holds it, but not openly.</summary>
    public bool Held => Place is BotRelicPlace.Carried or BotRelicPlace.Hall or BotRelicPlace.Kept;

    public Mobile Carrier => Bot == Serial.MinusOne ? null : World.FindMobile(Bot);

    /// <summary>Who has it, for a sentence.</summary>
    public string Holder =>
        Place switch
        {
            BotRelicPlace.Hall or BotRelicPlace.Kept => Guild,
            BotRelicPlace.Carried => Guild ?? Carrier?.Name ?? "nobody",
            BotRelicPlace.Stolen => Carrier?.Name ?? "a thief",
            _ => "nobody"
        };

    /// <summary>Where it is, for a sentence.</summary>
    public string Where =>
        Place switch
        {
            BotRelicPlace.Hall => $"in the hall of {Guild}",
            BotRelicPlace.Kept => $"kept by {Guild}, which has no hall to hang it in",
            BotRelicPlace.Carried => $"in {Carrier?.Name ?? "somebody"}'s pack{(Guild != null ? $" for {Guild}" : "")}",
            BotRelicPlace.Stolen => $"in the pack of {Carrier?.Name ?? "a thief"}, stolen from {Former ?? "its holder"}",
            BotRelicPlace.Lair => BotRelicLair.Now is { } lair && ReferenceEquals(lair.Relic, this) ? $"in a lair {lair.Bearing}" : "in a lair",
            _ => "hidden, waiting to be rumoured"
        };
}

/// <summary>
/// Relics and banners (07.10.2026, relics20; Patrick: "4–6 unique items: the Crown of Britannia (a chest behind a lair boss), each
/// guild's banner (hangs in its hall), the Bell of Minoc, the Anchor of Trinsic. The holder gets something tangible and visible: −5 %
/// prices in the relic's town, +appeal, the right to call the Great Fair. A relic can be stolen, taken in war, ransomed, returned.
/// Biographies remember it; the herald tells it; the map pins the relic and its holder").
///
/// <para>
/// <b>The registry.</b> Five town relics — the Crown of Britannia, the Bell of Minoc, the Anchor of Trinsic, the Lantern of Vesper and
/// the Scales of Yew — and one banner per guild. Each is an engine item (<see cref="BotRelicItem"/>) the world save keeps; this class
/// says whose it is, where, and since when, and is itself kept by <see cref="BotRelicStore"/>. A look every <see cref="SyncEveryMs"/>
/// keeps the item where the registry says: a banner on its hall's floor, a relic in its carrier's pack, back in the hall when the
/// hall moved; banners made for new guilds; ransoms asked and paid; the Great Fair called.
/// </para>
///
/// <para>
/// <b>What a relic gives its holder.</b> A town relic held by a guild (in its hall, kept, or carried by a member): its members pay
/// <see cref="Discount"/> at that town's NPC counters (settled by <see cref="BotBurgh"/> like any town rebate — less coin destroyed at
/// a counter, none made), the guild's appeal is <see cref="Appeal"/> higher, and once a day its holder calls the Great Fair in that
/// town (<see cref="BotFair.CallGreat"/>: the fair with twice the rebate). Another guild's banner in the hall is a trophy: the same
/// appeal. A guild's own banner gives nothing but the right to lose it.
/// </para>
///
/// <para>
/// <b>How a relic changes hands.</b> A town relic is put in a lair behind a warden by <see cref="BotRelicLair"/>; the company that
/// clears it takes it, and its carrier walks it home (<see cref="BotRelicHomeward"/>). On the road a thief may take it from the pack
/// (<see cref="BotRelicSnatch"/>); the fence then sells it back to the guild it was taken from for <see cref="RelicRansom"/> out of
/// that guild's chest, or after <see cref="FenceWaitMs"/> to any guild that can pay; the coin goes to the thief. A relic in a hall is
/// taken only by a war: the loser's banner — and with <see cref="WarTakesAll"/> every relic it holds — goes to the winner's hall at
/// the war's end (<see cref="WarEnded"/>). The loser thinks <see cref="Grudge"/> worse of the winner (written in its book of
/// grievances, so its envoy can state it) and may declare a war of revenge on the holder of its banner with no truce and no clock
/// in the way (<see cref="Avenges"/>). It buys its banner back for <see cref="BannerRansom"/>, chest to chest, once
/// <see cref="RansomAfterMs"/> have passed with no war between them; after <see cref="ReturnAfterMs"/> the holder sends it home
/// unasked.
/// </para>
///
/// <para>
/// <b>Honest money.</b> Nothing here makes coin: a ransom and a fence's sale move coin from a chest to a chest or to the thief's
/// account; the discount is a rebate on coin paid at an NPC; the warden's gold is the engine's own loot, a kill. No relic is sold to
/// an NPC. Every move is counted on the <c>Relics:</c> line.
/// </para>
/// </summary>
public static class BotRelics
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRelics));

    // ---- dials ---------------------------------------------------------------------------------------------------------------------

    /// <summary>The switch: off, no relic gives anything, moves or is hunted. The items stay where they are.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>What a member of a town relic's holding guild pays at that town's NPC counters, as a share of the shelf.</summary>
    public static double Discount { get; set; } = 0.95;

    /// <summary>Appeal a guild gains for each relic it holds that is not its own banner. See <c>BotGuilds.Appeal</c>.</summary>
    public static double Appeal { get; set; } = 3.0;

    /// <summary>How often the registry looks at every relic: places, ransoms, returns, banners for new guilds, the Great Fair.</summary>
    public static int SyncEveryMs { get; set; } = 30000;

    /// <summary>What a guild that lost a war thinks the worse of the winner for holding its banner. Written as a grievance.</summary>
    public static double Grudge { get; set; } = -30.0;

    /// <summary>Whether a guild may declare on the holder of its banner with a truce or its own clock standing. See <see cref="Avenges"/>.</summary>
    public static bool Revenge { get; set; } = true;

    /// <summary>
    /// How long after its banner was taken a guild may avenge it past a truce or a clock: six hours. A war of revenge lost leaves the
    /// banner where it was and its clock running, so a guild that keeps losing is not handed one war after another for ever.
    /// </summary>
    public static int RevengeMs { get; set; } = 21600000;

    /// <summary>Whether a war's loser gives up every relic it holds, not only its banner.</summary>
    public static bool WarTakesAll { get; set; } = true;

    /// <summary>What a guild pays the holder of its banner to have it back, chest to chest.</summary>
    public static int BannerRansom { get; set; } = 1500;

    /// <summary>How long after a banner was taken its guild may buy it back (with no war between the two).</summary>
    public static int RansomAfterMs { get; set; } = 3600000;

    /// <summary>What a chest keeps back when it pays a ransom or buys from the fence.</summary>
    public static int ChestKeep { get; set; } = 500;

    /// <summary>How long a captured banner is kept before its holder sends it home unasked.</summary>
    public static int ReturnAfterMs { get; set; } = 86400000;

    /// <summary>What the fence asks for a stolen relic, of the guild it was taken from or of anybody after <see cref="FenceWaitMs"/>.</summary>
    public static int RelicRansom { get; set; } = 1000;

    /// <summary>How long after a theft the fence sends word to the guild that lost the relic.</summary>
    public static int FenceWordMs { get; set; } = 600000;

    /// <summary>How long the fence waits on that guild before it offers the relic to any guild that can pay.</summary>
    public static int FenceWaitMs { get; set; } = 7200000;

    /// <summary>How long a relic may stay in a member's pack before the guild fetches it home itself.</summary>
    public static int CarryMostMs { get; set; } = 3600000;

    /// <summary>How long a holder waits between Great Fairs with one relic; and the least between any two Great Fairs on the island.</summary>
    public static int GreatEveryMs { get; set; } = 86400000;

    public static int GreatGapMs { get; set; } = 21600000;

    /// <summary>How many lines of news are kept for the herald (<see cref="News"/>).</summary>
    public static int NewsKept { get; set; } = 20;

    // ---- counters ------------------------------------------------------------------------------------------------------------------

    public static long BannersMade { get; private set; }

    public static long BannersGone { get; private set; }

    public static long Hung { get; private set; }

    public static long Rehung { get; private set; }

    public static long Fetched { get; private set; }

    public static long Strayed { get; private set; }

    public static long Remade { get; private set; }

    public static long Rehidden { get; private set; }

    public static long BannersTaken { get; private set; }

    public static long RelicsTakenInWar { get; private set; }

    public static long Grudges { get; private set; }

    public static long Waived { get; private set; }

    public static long BannerRansoms { get; private set; }

    public static long BannerRansomGold { get; private set; }

    public static long Returned { get; private set; }

    public static long Ransomed { get; private set; }

    public static long RansomGold { get; private set; }

    public static long Fenced { get; private set; }

    public static long FenceGold { get; private set; }

    public static long Recovered { get; private set; }

    public static long Priced { get; private set; }

    public static long Rebates { get; private set; }

    public static long RebateGold { get; private set; }

    public static long GreatFairs { get; private set; }

    public static long GreatRefused { get; private set; }

    public static long Handovers { get; private set; }

    // ---- the registry ------------------------------------------------------------------------------------------------------------

    private static readonly List<BotRelic> _relics = [];

    private static readonly Dictionary<string, BotRelic> _byKey = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Town relics held openly by somebody: the counter's one comparison when nobody holds any.</summary>
    private static int _townHeld;

    private static readonly List<string> _news = [];

    public static IReadOnlyList<BotRelic> All => _relics;

    /// <summary>
    /// The latest relic news, oldest first, at most <see cref="NewsKept"/> lines: "12:04 The Hammer took the Crown of Britannia
    /// from its lair". For the herald (officers20) and anybody else who tells the island what happened.
    /// </summary>
    public static IReadOnlyList<string> News => _news;

    /// <summary>Lines of news written since the start: a reader keeps the last it saw and reads what is newer.</summary>
    public static long NewsCount { get; private set; }

    /// <summary>The five town relics: key, name, town, graphic, hue.</summary>
    private static readonly (string Key, string Name, string Town, int ItemId, int Hue)[] _towns =
    [
        ("crown", "the Crown of Britannia", "Britain", 0x1412, 0x8A5),
        ("bell", "the Bell of Minoc", "Minoc", 0x1C12, 0x972),
        ("anchor", "the Anchor of Trinsic", "Trinsic", 0x14F7, 0x47E),
        ("lantern", "the Lantern of Vesper", "Vesper", 0xA25, 0x501),
        ("scales", "the Scales of Yew", "Yew", 0x1852, 0x8A5)
    ];

    public static BotRelic Find(string key) => key != null && _byKey.TryGetValue(key, out var r) ? r : null;

    public static string BannerKey(string guild) => $"banner:{guild}";

    public static BotRelic Banner(string guild) => guild == null ? null : Find(BannerKey(guild));

    /// <summary>The five town relics, made at the first start (hidden) and kept by the store afterwards.</summary>
    internal static void Seed()
    {
        for (var i = 0; i < _towns.Length; i++)
        {
            var (key, name, town, itemId, hue) = _towns[i];

            if (Find(key) is { } known)
            {
                known.Name = name;
                known.Town = town;
                known.ItemId = itemId;
                known.Hue = hue;

                continue;
            }

            var r = new BotRelic { Key = key, Name = name, Town = town, ItemId = itemId, Hue = hue, Place = BotRelicPlace.Hidden };

            Add(r);
            Ensure(r);
        }
    }

    internal static void Add(BotRelic relic)
    {
        if (relic?.Key == null || _byKey.ContainsKey(relic.Key))
        {
            return;
        }

        _relics.Add(relic);
        _byKey[relic.Key] = relic;
    }

    private static void Remove(BotRelic relic)
    {
        _relics.Remove(relic);
        _byKey.Remove(relic.Key);
        relic.Item?.Delete();
        relic.Item = null;
    }

    /// <summary>A banner's look: one of the engine's standing banners, chosen by the guild's name so it is the same every start.</summary>
    private static int BannerArt(string guild)
    {
        var sum = 0;

        for (var i = 0; i < guild.Length; i++)
        {
            sum = sum * 31 + guild[i];
        }

        return 0x15AE + 2 * (int)((uint)sum % 35u);
    }

    /// <summary>The item, made when missing (first start, or lost with a deleted carrier). False when it had to be made.</summary>
    private static bool Ensure(BotRelic r)
    {
        if (r.Item is { Deleted: false })
        {
            return true;
        }

        r.Item = new BotRelicItem(r.Key, r.ItemId) { Name = r.Name, Hue = r.Hue };
        r.Item.Internalize();

        return false;
    }

    /// <summary>Links the relics the world save read back to the registry; strays and duplicates are deleted. At the module's start.</summary>
    internal static void Link()
    {
        var loaded = BotRelicItem.Loaded;
        var linked = 0;
        var strays = 0;

        for (var i = 0; i < loaded.Count; i++)
        {
            var item = loaded[i];

            if (item is not { Deleted: false })
            {
                continue;
            }

            var r = Find(item.Key);

            if (r == null || r.Item is { Deleted: false } && !ReferenceEquals(r.Item, item))
            {
                item.Delete();
                strays++;

                continue;
            }

            r.Item = item;
            linked++;
        }

        loaded.Clear();

        if (linked + strays > 0)
        {
            logger.Information("Relics: {Linked} relic items read back from the world and linked, {Strays} strays deleted", linked, strays);
        }
    }

    // ---- the gifts ------------------------------------------------------------------------------------------------------------------

    /// <summary>Whether this bot is of the relic's open holder: a member of its guild, or the guildless carrier itself.</summary>
    private static bool Benefits(BotRelic r, Mobile bot) =>
        r.Held && (r.Guild != null ? bot.Guild is Guild g && g.Name == r.Guild : bot.Serial == r.Bot);

    /// <summary>
    /// What this buyer pays at this NPC's counter for the relics its guild holds, as a share of the shelf: <see cref="Discount"/> in
    /// a town whose relic it holds, one everywhere else. Multiplied into <c>BotBurgh.Factor</c> and settled as its rebate.
    /// </summary>
    public static double Counter(Mobile buyer, BaseVendor vendor)
    {
        if (!Running || _townHeld == 0 || buyer is not BotMobile || vendor == null || BotTowns.Of(vendor.Location) is not { } town)
        {
            return 1.0;
        }

        for (var i = 0; i < _relics.Count; i++)
        {
            var r = _relics[i];

            if (r.Town == town.Name && Benefits(r, buyer))
            {
                Priced++;

                return Math.Clamp(Discount, 0.5, 1.0);
            }
        }

        return 1.0;
    }

    /// <summary>A town's rebate settled at a counter, counted when the buyer's guild holds that town's relic. See <c>BotBurgh.Settle</c>.</summary>
    public static void Rebated(Mobile buyer, int back)
    {
        if (!Running || _townHeld == 0 || back <= 0 || buyer == null || BotTowns.Of(buyer.Location) is not { } town)
        {
            return;
        }

        for (var i = 0; i < _relics.Count; i++)
        {
            if (_relics[i].Town == town.Name && Benefits(_relics[i], buyer))
            {
                Rebates++;
                RebateGold += back;

                return;
            }
        }
    }

    /// <summary>Appeal this guild draws from the relics it holds: <see cref="Appeal"/> each, its own banner left out.</summary>
    public static double AppealOf(Guild guild)
    {
        if (!Running || guild == null || _relics.Count == 0)
        {
            return 0.0;
        }

        var held = 0;

        for (var i = 0; i < _relics.Count; i++)
        {
            var r = _relics[i];

            if (r.Held && r.Guild == guild.Name && r.Of != guild.Name)
            {
                held++;
            }
        }

        return held * Appeal;
    }

    /// <summary>How many relics this guild holds openly, its own banner left out. For the dashboard and the guilds' paragraph.</summary>
    public static int HeldBy(string guild)
    {
        var held = 0;

        for (var i = 0; i < _relics.Count; i++)
        {
            if (_relics[i].Held && _relics[i].Guild == guild && _relics[i].Of != guild)
            {
                held++;
            }
        }

        return held;
    }

    /// <summary>
    /// Whether <paramref name="mine"/> may declare on <paramref name="theirs"/> for revenge: <paramref name="theirs"/> holds
    /// <paramref name="mine"/>'s banner. Asked by <c>BotWar.MayDeclare</c>, which then lets a truce or the declarer's clock stand
    /// aside — never the rules of might, tribute or a war already standing.
    /// </summary>
    public static bool Avenges(string mine, string theirs) =>
        Running && Revenge && mine != null && theirs != null && Banner(mine) is { Held: true } r && r.Guild == theirs
        && (DateTime.UtcNow - r.Since).TotalMilliseconds < RevengeMs;

    private static readonly Dictionary<(string, string), long> _waivedSaid = [];

    /// <summary>The revenge rule let a declaration past a truce or a clock. Said once in ten minutes per pair.</summary>
    public static void Waive(string mine, string theirs, string what)
    {
        Waived++;

        var now = Core.TickCount;

        if (_waivedSaid.TryGetValue((mine, theirs), out var said) && now - said < 600000)
        {
            return;
        }

        _waivedSaid[(mine, theirs)] = now;

        logger.Information("{Mine} may declare on {Theirs} despite {What}: the other holds its banner, and revenge waits for no clock", mine, theirs, what);
    }

    // ---- the war ------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// A war ended with a winner: the loser's banner goes to the winner's hall, and with <see cref="WarTakesAll"/> every relic the
    /// loser holds; the loser's grudge is written. Called from <c>BotWar.End</c> after the opinions were settled.
    /// </summary>
    public static void WarEnded(string winner, string loser)
    {
        if (!Running || winner == null || loser == null || BotGuilds.Find(winner) == null)
        {
            return;
        }

        List<string> taken = null;

        for (var i = 0; i < _relics.Count; i++)
        {
            var r = _relics[i];

            if (!r.Held || r.Guild != loser || r.Of != loser && !WarTakesAll)
            {
                continue;
            }

            Hand(r, winner, loser);

            if (r.Of == loser)
            {
                BannersTaken++;
            }
            else
            {
                RelicsTakenInWar++;
            }

            (taken ??= []).Add(r.Name);
        }

        if (taken == null)
        {
            return;
        }

        var what = string.Join(", ", taken);

        if (Grudge != 0.0)
        {
            BotRegard.Move(loser, winner, Grudge, "its banner taken");
            Grudges++;
        }

        Tell(
            $"{winner} won its war with {loser} and carried home {what}",
            "{Winner} won its war with {Loser} and carries home {What}; the loser thinks {Grudge:F0} the worse of it and may declare a war of revenge with no truce in the way; it may buy its banner back for {Ransom}gp in {Hours:F0} hours",
            winner,
            loser,
            what,
            Grudge,
            BannerRansom,
            RansomAfterMs / 3600000.0
        );

        BotEvents.Post("relic", null, null, winner, 0, 0, $"{winner} took {what} from {loser} in war", "war", null);

        if (BotGuilds.Head(BotGuilds.Find(winner)) is { } head)
        {
            BotVoice.Say(head, "world", $"{loser} is beaten! We carry home {what}.", true);
            Remember(head, "relic-war", loser, $"We beat {loser} in war, and I saw {what} carried into our hall.", 50.0);
        }

        if (BotGuilds.Head(BotGuilds.Find(loser)) is { } theirs)
        {
            BotVoice.Say(theirs, "guild", $"{winner} has our banner. We will have it back, by coin or by blood.", true);
            Remember(theirs, "relic-lost", winner, $"{winner} beat us and took {what}. I will not forget it.", 52.0);
        }
    }

    // ---- moving a relic -------------------------------------------------------------------------------------------------------------

    /// <summary>Hands a relic to a guild: into its hall, or kept by it when it has none. The former holder is remembered.</summary>
    internal static void Hand(BotRelic r, string guild, string former)
    {
        // A carrier setting its own guild's relic up at home is not a change of hands, and its clock keeps running.
        var home = r.Place == BotRelicPlace.Carried && r.Guild == guild;

        if (!home)
        {
            r.Hands++;
            Handovers++;
            r.Since = DateTime.UtcNow;
        }

        r.Guild = guild;
        r.Bot = Serial.MinusOne;
        r.Former = former;
        Hang(r);
        Recount();
    }

    /// <summary>Into a bot's pack: the company that cleared a lair, a carrier. The guild it is carried for is the bot's.</summary>
    internal static bool Carry(BotRelic r, Mobile bot, string how)
    {
        Ensure(r);

        if (bot?.Backpack is not { } pack)
        {
            return false;
        }

        pack.DropItem(r.Item);

        r.Place = BotRelicPlace.Carried;
        r.Guild = bot.Guild is Guild { Disbanded: false } g && g.Name != BotUnderworld.GuildName ? g.Name : null;
        r.Bot = bot.Serial;
        r.Since = DateTime.UtcNow;
        r.Hands++;
        Handovers++;
        Recount();

        logger.Information("{Name} carries {Relic} now ({How}){For}", bot.Name, r.Name, how, r.Guild != null ? $", for {r.Guild}" : "");

        return true;
    }

    /// <summary>Into a thief's pack, taken from the guild that had it.</summary>
    internal static void Steal(BotRelic r, Mobile thief)
    {
        Ensure(r);
        thief.Backpack?.DropItem(r.Item);

        r.Former = r.Guild ?? r.Former;
        r.Guild = null;
        r.Place = BotRelicPlace.Stolen;
        r.Bot = thief.Serial;
        r.Since = DateTime.UtcNow;
        r.Hands++;
        Handovers++;
        Recount();
    }

    /// <summary>Puts the item where the registry says the guild keeps it: on its hall's floor, or out of the world when it has none.</summary>
    private static void Hang(BotRelic r)
    {
        Ensure(r);

        var hall = BotEstate.Hall(BotGuilds.Find(r.Guild));

        if (hall != null)
        {
            var spot = BotFittings.Spot(hall);

            if (spot != Point3D.Zero)
            {
                r.Item.MoveToWorld(spot, hall.Map);
                r.Place = BotRelicPlace.Hall;
                Hung++;

                return;
            }
        }

        r.Item.Internalize();
        r.Place = BotRelicPlace.Kept;
    }

    /// <summary>Back out of the world, to be rumoured again: a town relic whose holder is gone.</summary>
    internal static void Hide(BotRelic r, string why)
    {
        Ensure(r);
        r.Item.Internalize();
        r.Place = BotRelicPlace.Hidden;
        r.Guild = null;
        r.Bot = Serial.MinusOne;
        r.Since = DateTime.UtcNow;
        Rehidden++;
        Recount();

        logger.Information("{Relic} is hidden again ({Why}): it will be rumoured from a lair another day", r.Name, why);
    }

    /// <summary>Into a lair's chest. See <see cref="BotRelicLair"/>.</summary>
    internal static void Lay(BotRelic r, Container chest)
    {
        Ensure(r);
        chest.DropItem(r.Item);
        r.Place = BotRelicPlace.Lair;
        r.Guild = null;
        r.Bot = Serial.MinusOne;
        r.Since = DateTime.UtcNow;
        Recount();
    }

    internal static void Recount()
    {
        _townHeld = 0;
        Carrying = 0;

        for (var i = 0; i < _relics.Count; i++)
        {
            if (_relics[i].Town != null && _relics[i].Held)
            {
                _townHeld++;
            }

            if (_relics[i].Place == BotRelicPlace.Carried)
            {
                Carrying++;
            }
        }
    }

    /// <summary>Relics in a carrier's pack now: the thieves' and the carriers' proposers ask nothing more when it is nought.</summary>
    public static int Carrying { get; private set; }

    /// <summary>The relic this bot carries openly, or null.</summary>
    public static BotRelic CarriedBy(Mobile bot)
    {
        if (Carrying == 0 || bot == null)
        {
            return null;
        }

        for (var i = 0; i < _relics.Count; i++)
        {
            if (_relics[i].Place == BotRelicPlace.Carried && _relics[i].Bot == bot.Serial)
            {
                return _relics[i];
            }
        }

        return null;
    }

    // ---- the look -------------------------------------------------------------------------------------------------------------------

    private static long _syncTick;

    private static bool _synced;

    private static long _greatTick;

    private static bool _greatEver;

    /// <summary>Called on the relics' clock: the registry's look every <see cref="SyncEveryMs"/>.</summary>
    internal static void Beat(long now)
    {
        if (!Running || _synced && now - _syncTick < Math.Max(5000, SyncEveryMs))
        {
            return;
        }

        // Not before the halls are read back: a banner looking for its hall before the estate has adopted it would be put away.
        if (BotEstate.Running && !BotEstate.HallsAdopted)
        {
            return;
        }

        _synced = true;
        _syncTick = now;

        Banners();

        for (var i = _relics.Count - 1; i >= 0; i--)
        {
            Look(_relics[i], now);
        }

        Recount();
    }

    /// <summary>A banner for every standing guild that has none; the banner of a guild gone that nobody else holds is taken down.</summary>
    private static void Banners()
    {
        foreach (var guild in BotGuilds.Standing)
        {
            if (guild is not { Disbanded: false } || guild.Name == BotUnderworld.GuildName || Banner(guild.Name) != null)
            {
                continue;
            }

            var r = new BotRelic
            {
                Key = BannerKey(guild.Name),
                Name = $"the banner of {guild.Name}",
                Of = guild.Name,
                ItemId = BannerArt(guild.Name),
                Hue = 0,
                Guild = guild.Name
            };

            Add(r);
            Hang(r);
            BannersMade++;

            logger.Information("{Guild}'s banner is made and {Where}", guild.Name, r.Where);
        }

        for (var i = _relics.Count - 1; i >= 0; i--)
        {
            var r = _relics[i];

            if (r.IsBanner && BotGuilds.Find(r.Of) == null && (r.Guild == r.Of || r.Guild == null || BotGuilds.Find(r.Guild) == null))
            {
                logger.Information("{Relic} is taken down: {Guild} is no more", r.Name, r.Of);

                Remove(r);
                BannersGone++;
            }
        }
    }

    /// <summary>One relic looked at: the item where the registry says, its holder still there, a ransom asked, a fair called.</summary>
    private static void Look(BotRelic r, long now)
    {
        if (!Ensure(r))
        {
            Remade++;

            // The item was lost (a carrier deleted, the save without it): the relic goes back to whoever openly held it, else hidden.
            if (r.Place is BotRelicPlace.Carried or BotRelicPlace.Stolen or BotRelicPlace.Hall or BotRelicPlace.Kept)
            {
                Restore(r, "its item was lost");
            }
            else if (r.Place == BotRelicPlace.Lair && BotRelicLair.Now?.Relic != r)
            {
                Hide(r, "its lair is gone");
            }

            return;
        }

        switch (r.Place)
        {
            case BotRelicPlace.Hall:
            case BotRelicPlace.Kept:
                {
                    if (BotGuilds.Find(r.Guild) == null)
                    {
                        Orphan(r);

                        return;
                    }

                    var hall = BotEstate.Hall(BotGuilds.Find(r.Guild));

                    if (hall == null && r.Place == BotRelicPlace.Hall
                        || hall != null && (r.Place == BotRelicPlace.Kept || r.Item.Map != hall.Map || !BotFittings.InRoom(hall, r.Item.Location)))
                    {
                        Hang(r);

                        // Counted only when it found a floor: a full hall leaves it kept, and asks again next look.
                        if (r.Place == BotRelicPlace.Hall)
                        {
                            Rehung++;
                        }
                    }

                    Ransom(r);
                    Great(r, now);

                    return;
                }
            case BotRelicPlace.Carried:
                {
                    var carrier = r.Carrier;

                    if (carrier is not { Deleted: false } || r.Item.RootParent != carrier)
                    {
                        Strayed++;
                        Restore(r, carrier == null ? "its carrier is gone" : "it left its carrier's pack");

                        return;
                    }

                    if (r.Guild != null && (carrier.Guild is not Guild g || g.Name != r.Guild))
                    {
                        Restore(r, $"{carrier.Name} left {r.Guild}");

                        return;
                    }

                    // A guildless carrier that has joined a guild since brings the relic to it.
                    if (r.Guild == null && carrier.Guild is Guild { Disbanded: false } joined && joined.Name != BotUnderworld.GuildName)
                    {
                        r.Guild = joined.Name;
                        Recount();
                    }

                    if (r.Guild != null && (DateTime.UtcNow - r.Since).TotalMilliseconds >= CarryMostMs && BotEstate.Hall(BotGuilds.Find(r.Guild)) != null)
                    {
                        Fetched++;
                        Hand(r, r.Guild, r.Former);
                        Tell($"{r.Guild} fetched {r.Name} home from {carrier.Name}'s pack", "{Guild} fetched {Relic} home from {Name}'s pack after {Minutes} minutes on the road", r.Guild, r.Name, carrier.Name, CarryMostMs / 60000);

                        return;
                    }

                    Great(r, now);

                    return;
                }
            case BotRelicPlace.Stolen:
                {
                    Fence(r);

                    return;
                }
        }
    }

    /// <summary>A relic back to whoever openly held it: its guild's hall, the banner's own guild, else hidden.</summary>
    private static void Restore(BotRelic r, string why)
    {
        var to = r.Place == BotRelicPlace.Stolen ? r.Former : r.Guild ?? r.Former;

        if (to == null && r.IsBanner)
        {
            to = r.Of;
        }

        if (to != null && BotGuilds.Find(to) != null)
        {
            Hand(r, to, r.Former);

            logger.Information("{Relic} is back with {Guild} ({Why}): {Where}", r.Name, to, why, r.Where);

            return;
        }

        if (r.IsBanner)
        {
            Remove(r);
            BannersGone++;

            return;
        }

        Hide(r, why);
    }

    /// <summary>The holding guild is no more: a banner goes home or down, a town relic back into hiding.</summary>
    private static void Orphan(BotRelic r)
    {
        if (r.IsBanner && BotGuilds.Find(r.Of) != null)
        {
            Hand(r, r.Of, r.Guild);
            Returned++;

            return;
        }

        if (r.IsBanner)
        {
            Remove(r);
            BannersGone++;

            return;
        }

        Hide(r, $"{r.Guild} is no more");
    }

    // ---- ransom and return ----------------------------------------------------------------------------------------------------------

    /// <summary>A captured banner: bought back by its guild once there is no war between them, or sent home after a day.</summary>
    private static void Ransom(BotRelic r)
    {
        if (!r.IsBanner || r.Guild == r.Of || BotGuilds.Find(r.Of) == null)
        {
            return;
        }

        var held = (DateTime.UtcNow - r.Since).TotalMilliseconds;

        if (held < RansomAfterMs || BotWar.Of(r.Of, r.Guild) != null)
        {
            return;
        }

        var holder = r.Guild;

        if (BannerRansom > 0 && BotChest.Holds(r.Of) >= BannerRansom + Math.Max(0, ChestKeep))
        {
            var paid = BotChest.Pay(r.Of, holder, BannerRansom);

            if (paid > 0)
            {
                BannerRansoms++;
                BannerRansomGold += paid;
                Hand(r, r.Of, holder);

                Tell(
                    $"{r.Of} bought its banner back from {holder} for {paid}gp",
                    "{Guild} bought its banner back from {Holder} for {Paid}gp, chest to chest, after {Hours:F1} hours in its hall",
                    r.Of,
                    holder,
                    paid,
                    held / 3600000.0
                );

                if (BotGuilds.Head(BotGuilds.Find(r.Of)) is { } head)
                {
                    BotVoice.Say(head, "guild", $"Our banner is home. It cost us {paid}gp — remember who made us pay it.", true);
                }

                return;
            }
        }

        if (held >= ReturnAfterMs)
        {
            Returned++;
            Hand(r, r.Of, holder);

            Tell($"{holder} sent {r.Of}'s banner home after a day", "{Holder} sent {Guild}'s banner home unasked after {Hours:F0} hours", holder, r.Of, held / 3600000.0);
        }
    }

    /// <summary>
    /// A stolen relic: given back free when the thief is caught or dead; after <see cref="FenceWordMs"/> sold back to the guild it
    /// was taken from, after <see cref="FenceWaitMs"/> to any guild that can pay. The coin goes from the buyer's chest to the thief.
    /// </summary>
    private static void Fence(BotRelic r)
    {
        var thief = r.Carrier;

        if (thief is not { Deleted: false, Alive: true } || BotOutlaw.Jailed(thief) || r.Item.RootParent != thief)
        {
            Recovered++;

            var why = thief == null || thief.Deleted ? "the thief is gone"
                : !thief.Alive ? $"{thief.Name} the thief fell"
                : BotOutlaw.Jailed(thief) ? $"{thief.Name} the thief was caught"
                : "it left the thief's pack";

            Restore(r, why);
            Tell($"{r.Name} was recovered: {why}", "{Relic} was recovered and goes back to {Guild}: {Why}", r.Name, r.Guild ?? "nobody", why);

            return;
        }

        var held = (DateTime.UtcNow - r.Since).TotalMilliseconds;

        if (held < FenceWordMs || RelicRansom <= 0)
        {
            return;
        }

        string buyer = null;

        if (r.Former != null && BotGuilds.Find(r.Former) != null && BotChest.Holds(r.Former) >= RelicRansom + Math.Max(0, ChestKeep))
        {
            buyer = r.Former;
        }
        else if (held >= FenceWaitMs)
        {
            var most = RelicRansom + Math.Max(0, ChestKeep) - 1;

            foreach (var guild in BotGuilds.Standing)
            {
                if (guild is not { Disbanded: false } || guild.Name == BotUnderworld.GuildName)
                {
                    continue;
                }

                var chest = BotChest.Holds(guild.Name);

                if (chest > most)
                {
                    most = chest;
                    buyer = guild.Name;
                }
            }
        }

        if (buyer == null)
        {
            return;
        }

        var paid = BotChest.Draw(buyer, RelicRansom);

        if (paid <= 0)
        {
            return;
        }

        if (!BotSeasons.Pay(thief, paid))
        {
            BotChest.Put(buyer, paid);

            return;
        }

        // Coin into the thief by the fence's deal, not by the work in hand.
        BotYield.Aside(thief, -paid);

        var back = buyer == r.Former;

        if (back)
        {
            Ransomed++;
            RansomGold += paid;
        }
        else
        {
            Fenced++;
            FenceGold += paid;
        }

        var from = r.Former;

        Hand(r, buyer, from);

        Tell(
            back ? $"{buyer} ransomed {r.Name} from the fence for {paid}gp" : $"the fence sold {r.Name} to {buyer} for {paid}gp",
            "{Buyer} {Did} {Relic} from the fence for {Paid}gp out of its chest, to {Thief} who stole it from {Former} {Minutes} minutes ago; it is {Where}",
            buyer,
            back ? "ransomed" : "bought",
            r.Name,
            paid,
            thief.Name,
            from ?? "nobody",
            (int)(held / 60000),
            r.Where
        );

        BotEvents.Post("relic", thief, back ? $"sold {r.Name} back to {buyer} for {paid}gp" : $"fenced {r.Name} to {buyer} for {paid}gp", "crime");
        BotVoice.Say(thief, "local", $"{paid}gp for a trinket. Pleasure doing business.", true);
    }

    // ---- the Great Fair -------------------------------------------------------------------------------------------------------------

    /// <summary>A town relic's holder calls the Great Fair in its town once a day, when no fair stands and none was great lately.</summary>
    private static void Great(BotRelic r, long now)
    {
        if (r.Town == null || r.Guild == null || !BotSeasons.Running || !BotFair.Running || BotFair.State != BotFair.Phase.None)
        {
            return;
        }

        if ((DateTime.UtcNow - r.LastFair).TotalMilliseconds < GreatEveryMs || _greatEver && now - _greatTick < GreatGapMs)
        {
            return;
        }

        var town = BotTowns.Find(r.Town);

        if (town is not { FromHome: true })
        {
            return;
        }

        if (!BotFair.CallGreat(town, r.Guild, r.Name))
        {
            GreatRefused++;
            r.LastFair = DateTime.UtcNow.AddMilliseconds(-GreatEveryMs / 2.0);

            return;
        }

        r.LastFair = DateTime.UtcNow;
        _greatEver = true;
        _greatTick = now;
        GreatFairs++;

        Tell($"{r.Guild}, holder of {r.Name}, calls the Great Fair in {town.Name}", "{Guild}, holder of {Relic}, calls the Great Fair in {Town}", r.Guild, r.Name, town.Name);

        if (BotGuilds.Head(BotGuilds.Find(r.Guild)) is { } head)
        {
            BotVoice.Say(head, "world", $"By {r.Name}, {r.Guild} calls the Great Fair in {town.Name}! Come one, come all.", true);
        }
    }

    // ---- telling it ----------------------------------------------------------------------------------------------------------------

    /// <summary>A line of news for the herald, and the same in the log in the house style.</summary>
    internal static void Tell(string news, string template, params object[] values)
    {
        logger.Information(template, values);

        _news.Add($"{DateTime.Now:HH:mm} {news}");
        NewsCount++;

        while (_news.Count > Math.Max(1, NewsKept))
        {
            _news.RemoveAt(0);
        }
    }

    /// <summary>A tale for a bot's biography. See <see cref="BotBiography.Keep"/>.</summary>
    internal static void Remember(Mobile bot, string kind, string other, string text, double weight)
    {
        if (BotLives.Of(bot) is { } life)
        {
            BotBiography.Keep(life, kind, other, text, weight, 1);
        }
    }

    // ---- Argus ---------------------------------------------------------------------------------------------------------------------

    /// <summary>Argus's <c>do relics</c>: every relic, its holder, where it is and since when; the lair; the counters.</summary>
    public static string Report()
    {
        var line = new StringBuilder();

        for (var i = 0; i < _relics.Count; i++)
        {
            var r = _relics[i];
            var minutes = (int)(DateTime.UtcNow - r.Since).TotalMinutes;

            line.Append(line.Length > 0 ? "; " : "");
            line.Append($"{r.Name}: {r.Where}, {minutes} minutes, {r.Hands} hands");
        }

        return $"{_relics.Count} relics — {line}. Lair: {BotRelicLair.Describe()}. {Describe()}";
    }

    /// <summary>
    /// Argus's <c>do relic &lt;name&gt; ; &lt;guild&gt;</c>: hands a relic to a guild for testing, into its hall; <c>hide</c> instead of a
    /// guild puts it back into hiding. The name is a key (<c>crown</c>, <c>banner:The Hammer</c>) or a piece of the relic's name.
    /// </summary>
    public static string Give(string tail)
    {
        tail = (tail ?? "").Trim();

        string name;
        string guild;
        var semi = tail.IndexOf(';');

        if (semi >= 0)
        {
            name = tail[..semi].Trim();
            guild = tail[(semi + 1)..].Trim();
        }
        else
        {
            var space = tail.IndexOf(' ');

            name = space < 0 ? tail : tail[..space].Trim();
            guild = space < 0 ? "" : tail[(space + 1)..].Trim();
        }

        var r = Find(name);

        if (r == null)
        {
            for (var i = 0; i < _relics.Count && r == null; i++)
            {
                if (_relics[i].Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    r = _relics[i];
                }
            }
        }

        if (r == null || name.Length == 0)
        {
            return $"no relic called \"{name}\"; I know: {Keys()}";
        }

        if (r.Place == BotRelicPlace.Lair)
        {
            BotRelicLair.Abandon("handed over by hand");
        }

        if (guild.Equals("hide", StringComparison.OrdinalIgnoreCase))
        {
            if (r.IsBanner)
            {
                return "a banner is never hidden; hand it to its own guild instead";
            }

            Hide(r, "hidden by hand");

            return $"{r.Name} is hidden again";
        }

        var g = BotGuilds.Find(guild);

        if (g == null)
        {
            return $"no standing guild called \"{guild}\"";
        }

        var from = r.Holder;

        Hand(r, g.Name, r.Guild);
        Tell($"{r.Name} was handed to {g.Name}", "{Relic} was handed to {Guild} by hand (it was {From}'s): {Where}", r.Name, g.Name, from, r.Where);

        return $"{r.Name} is {r.Where} now (it was {from}'s)";
    }

    private static string Keys()
    {
        var keys = new StringBuilder();

        for (var i = 0; i < _relics.Count; i++)
        {
            keys.Append(i > 0 ? ", " : "").Append(_relics[i].Key);
        }

        return keys.ToString();
    }

    // ---- the page ------------------------------------------------------------------------------------------------------------------

    /// <summary>The <c>relics</c> block of <c>/api/state</c>: every relic with its holder, place and pin, the lair, and the counters.</summary>
    public static void Web(Utf8JsonWriter w)
    {
        w.WritePropertyName("relics");
        w.WriteStartObject();
        w.WriteBoolean("on", Running);
        w.WritePropertyName("list");
        w.WriteStartArray();

        for (var i = 0; i < _relics.Count; i++)
        {
            var r = _relics[i];

            w.WriteStartObject();
            w.WriteString("key", r.Key);
            w.WriteString("name", r.Name);
            w.WriteString("kind", r.IsBanner ? "banner" : "town");
            w.WriteString("town", r.Town);
            w.WriteString("of", r.Of);
            w.WriteString("place", r.Place.ToString());
            w.WriteString("holder", r.Holder);
            w.WriteString("guild", r.Guild);
            w.WriteString("where", r.Where);
            w.WriteNumber("sinceMin", (int)(DateTime.UtcNow - r.Since).TotalMinutes);
            w.WriteNumber("hands", r.Hands);

            if (r.Item is { Deleted: false } item && item.Map != null && item.Map != Map.Internal)
            {
                var at = item.GetWorldLocation();

                w.WriteNumber("x", at.X);
                w.WriteNumber("y", at.Y);
            }
            else
            {
                w.WriteNull("x");
                w.WriteNull("y");
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();

        BotRelicLair.Web(w);

        w.WritePropertyName("counts");
        w.WriteStartObject();
        w.WriteNumber("bannersTaken", BannersTaken);
        w.WriteNumber("relicsTakenInWar", RelicsTakenInWar);
        w.WriteNumber("bannerRansoms", BannerRansoms);
        w.WriteNumber("bannerRansomGold", BannerRansomGold);
        w.WriteNumber("returned", Returned);
        w.WriteNumber("stolen", BotRelicSnatch.Stolen);
        w.WriteNumber("ransomed", Ransomed);
        w.WriteNumber("fenced", Fenced);
        w.WriteNumber("recovered", Recovered);
        w.WriteNumber("rebates", Rebates);
        w.WriteNumber("rebateGold", RebateGold);
        w.WriteNumber("greatFairs", GreatFairs);
        w.WriteNumber("lairsCleared", BotRelicLair.Cleared);
        w.WriteEndObject();

        w.WritePropertyName("news");
        w.WriteStartArray();

        for (var i = 0; i < _news.Count; i++)
        {
            w.WriteStringValue(_news[i]);
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }

    // ---- the summary ---------------------------------------------------------------------------------------------------------------

    public static string Describe()
    {
        if (!Running)
        {
            return "off (BotRelics.Running)";
        }

        var held = new StringBuilder();
        var hidden = 0;
        var banners = 0;
        var trophies = 0;

        for (var i = 0; i < _relics.Count; i++)
        {
            var r = _relics[i];

            if (r.IsBanner)
            {
                banners++;

                if (r.Held && r.Guild != r.Of)
                {
                    trophies++;
                    held.Append(held.Length > 0 ? ", " : "").Append($"{r.Of}'s banner with {r.Guild}");
                }

                continue;
            }

            if (r.Place == BotRelicPlace.Hidden)
            {
                hidden++;

                continue;
            }

            held.Append(held.Length > 0 ? ", " : "").Append($"{r.Name} {r.Where}");
        }

        return $"{_relics.Count - banners} town relics ({hidden} hidden){(held.Length > 0 ? $": {held}" : "")}; {banners} banners ({trophies} in another guild's hand, {BannersMade} made, {BannersGone} taken down); "
               + $"hung {Hung} ({Rehung} re-hung after a hall moved, {Fetched} fetched home from a pack, {Strayed} strayed, {Remade} items remade, {Rehidden} hidden again), {Handovers} handovers; "
               + $"the lair: {BotRelicLair.Describe()}; carried home: {BotRelicHomeward.Describe()}; "
               + $"war: {BannersTaken} banners and {RelicsTakenInWar} relics taken by a winner, {Grudges} grudges written, {Waived} declarations let past a truce or a clock for revenge; "
               + $"banners bought back {BannerRansoms} for {BannerRansomGold}gp, {Returned} sent home; "
               + $"thieves: {BotRelicSnatch.Describe()}; the fence: {Ransomed} ransomed by their guild for {RansomGold}gp, {Fenced} sold to another for {FenceGold}gp, {Recovered} recovered free; "
               + $"gifts: {Priced} counter prices at {Discount:P0}, {Rebates} rebates where a relic applied for {RebateGold}gp, appeal +{Appeal:F0} a relic; {GreatFairs} Great Fairs called, {GreatRefused} refused";
    }

    public static void Forget()
    {
        _synced = false;
        _greatEver = false;
        _waivedSaid.Clear();
        BannersMade = BannersGone = Hung = Rehung = Fetched = Strayed = Remade = Rehidden = 0;
        BannersTaken = RelicsTakenInWar = Grudges = Waived = BannerRansoms = BannerRansomGold = Returned = 0;
        Ransomed = RansomGold = Fenced = FenceGold = Recovered = Priced = Rebates = RebateGold = GreatFairs = GreatRefused = Handovers = 0;
    }

    /// <summary>
    /// <b>Patrick's reset (07.10.2026, reset26): every relic taken out of the world and the registry started again as at a first
    /// start.</b> The reset of 20:56 that night left <c>Saves/BotRelics</c> whole and the relic items where the world save had them:
    /// a town's relic in a guild's name that no longer exists, a banner of an old guild hanging in a hall — or carried by the next
    /// guild founded under that name — and a captured banner still a grudge.
    ///
    /// <para>
    /// <b>What goes.</b> The lair first, by its own ending (<see cref="BotRelicLair.Abandon"/>): its warden and its chest out of the
    /// world. Then every relic's item is deleted, wherever it lies — a hall's floor, a pack, the internal map — and the registry,
    /// exactly what <see cref="BotRelicStore"/> writes, is emptied, with the news and the carriers', thieves' and lair's memory.
    /// An item the registry had lost track of is not hunted for (the repository's rule: no walk over the world's items); the next
    /// boot deletes it as a stray, since <see cref="Link"/> keeps only the item a registered key points at.
    /// </para>
    ///
    /// <para>
    /// <b>What comes back, by the module's own paths.</b> The five town relics are seeded again at once, hidden
    /// (<see cref="Seed"/>, what the store's <c>Apply</c> does at a boot with an empty registry), so the lair has something to
    /// rumour whether or not the shard is restarted; a banner is made by the registry's next look for every guild standing then
    /// (<c>Banners</c>), which after the reset is the guilds the new world founds. No coin is held by a relic: ransoms and fence
    /// prices were paid chest to chest when they happened. Returns the relic items taken out of the world.
    /// </para>
    /// </summary>
    public static int Wipe()
    {
        BotRelicLair.Abandon("the world was wiped");

        var gone = 0;

        for (var i = 0; i < _relics.Count; i++)
        {
            if (_relics[i].Item is { Deleted: false } item)
            {
                item.Delete();
                gone++;
            }

            _relics[i].Item = null;
        }

        _relics.Clear();
        _byKey.Clear();
        _news.Clear();
        NewsCount = 0;
        BotRelicItem.Loaded.Clear();

        Forget();
        BotRelicLair.Forget();
        BotRelicHomeward.Forget();
        BotRelicSnatch.Forget();

        Seed();
        Recount();

        return gone;
    }
}
