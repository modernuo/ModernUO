using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Each guild's standing in each town: what the town's shops charge it, the task the town has set it, how many it has failed in
/// a row, and whether it has been put out of the town.
///
/// <para>
/// <b>Patrick's order of 29.09.2026, point 3.</b> "Guilds deal with the town: the debugger is summoned, and gives the guild a
/// unique task. Each success lowers the town's prices for that guild by half a percent; each failure raises them by half a
/// percent. Three failed tasks in a row — the guild is put out of the town and may settle there again only after a couple of
/// days." Each guild sends an envoy to every town it lives in — its seat's, and any where <see cref="LeastResidents"/> of its
/// members live — once in <see cref="EveryMs"/> while it has no task there (<see cref="Look"/>); the envoy walks to the town's
/// hall and the town's voice sets the task (<see cref="BotTownTask"/>); the task is checked once a look until it is done or
/// its time is up.
/// </para>
///
/// <para>
/// <b>The price is paid at the counter, and no gold is made by it.</b> Every purchase a bot makes from a shopkeeper goes through
/// <c>BotShops.Buy</c> and the shopkeeper's own <c>OnBuyItems</c>, which takes the engine's price. For a guild in good standing
/// the difference is handed back into the buyer's account out of what it has just paid (<see cref="Settle"/>): a rebate is
/// never more than a share of that one purchase, so every purchase still takes coin out of the world, only less of it. For a
/// guild in poor standing the difference is taken from its pack or account on top, and the affordability of the purchase is
/// judged at the dearer price before anything is bought. The factor lives between <see cref="Least"/> and <see cref="Most"/>.
/// <c>BotShops.Price</c> — what bots expect to pay when they weigh work — is left at the shelf's price: a few percent either
/// way is not worth a second price for one fact.
/// </para>
///
/// <para>
/// <b>Put out of a town</b> means its members may not live there (<c>BotSettle.Choose</c> passes the town over for them,
/// <c>BotRelocate</c> moves those who do, and a guild seated by the town is seated by another — <see cref="Reseat"/>), until
/// <see cref="ExileHours"/> of the wall clock have passed, as the order asked. Its members may still walk through and buy there,
/// at the price its record has earned.
/// </para>
/// </summary>
public static class BotBurgh
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBurgh));

    /// <summary>Whether guilds deal with their towns at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How long after one task ends (or an audience lapses) a guild goes back to the same town for the next. An hour.</summary>
    public static int EveryMs { get; set; } = 3600000;

    /// <summary>How long after a boot the first audiences are spread over, so seven guilds do not all send envoys in the first minute.</summary>
    public static int FirstMs { get; set; } = 1200000;

    /// <summary>How many of a guild's members must live in a town, besides its seat's own, for the guild to deal with it. Three.</summary>
    public static int LeastResidents { get; set; } = 3;

    /// <summary>What one task done or failed moves the guild's prices in the town by. Half a percent, by order.</summary>
    public static double Step { get; set; } = 0.005;

    /// <summary>The cheapest a town may ever be to a guild, and the dearest: a tenth either way, twenty tasks from even.</summary>
    public static double Least { get; set; } = 0.90;

    public static double Most { get; set; } = 1.10;

    /// <summary>Tasks failed in a row that put a guild out of a town. Three, by order.</summary>
    public static int FailsToExile { get; set; } = 3;

    /// <summary>How long a guild put out of a town stays out, in hours of the wall clock. Forty-eight: "a couple of days".</summary>
    public static double ExileHours { get; set; } = 48.0;

    /// <summary>Whether a guild seated by a town that puts it out is seated by another. On: a seat in a town the guild may not live in is a seat nobody can live at.</summary>
    public static bool ExileMovesSeat { get; set; } = true;

    /// <summary>How often a supply task's request is put back on the guild's board while it stands. Twenty minutes, inside the board's thirty.</summary>
    public static int RequestEveryMs { get; set; } = 1200000;

    /// <summary>One guild in one town.</summary>
    public sealed class Standing
    {
        public string Guild;

        public string Town;

        /// <summary>What the town's shops charge this guild, as a multiple of their price.</summary>
        public double Factor = 1.0;

        /// <summary>Tasks failed in a row, and done and failed in all.</summary>
        public int Fails;

        public int Done;

        public int Failed;

        /// <summary>Until when the guild is put out of the town, on the wall clock; null when it is not.</summary>
        public DateTime? Out;

        /// <summary>The task standing, or null.</summary>
        public BotTownTask Task;

        /// <summary>When the next audience may be called. Session only: a boot spreads them afresh (see <see cref="FirstMs"/>).</summary>
        public long Next;

        public long Requested;

        public bool Exiled => Out is { } until && Core.Now < until;
    }

    private static readonly Dictionary<(string Guild, string Town), Standing> _standing = [];

    /// <summary>Whether any guild's prices anywhere differ from the shelf, so the counter can skip the lookup when none does.</summary>
    private static int _priced;

    public static long Given { get; private set; }

    public static long Succeeded { get; private set; }

    public static long FailedAll { get; private set; }

    public static long Exiles { get; private set; }

    public static long Reseated { get; private set; }

    private static readonly long[] _givenBy = new long[3];

    /// <summary>Purchases at a guild's price, and the gold handed back or taken on top by it.</summary>
    public static long Rebates { get; private set; }

    public static long Rebated { get; private set; }

    public static long Surcharges { get; private set; }

    public static long Surcharged { get; private set; }

    public static long Unpaid { get; private set; }

    /// <summary>Choices of a town to live in that passed one over because the bot's guild is put out of it.</summary>
    public static long TurnedAway { get; private set; }

    /// <summary>Goods counted towards a supply task, in units.</summary>
    public static long SuppliedUnits { get; private set; }

    /// <summary>This guild's standing in this town, made even if it was never there. Never null for a name and a town.</summary>
    public static Standing Of(string guild, BotTowns.Town town) => Get(guild ?? "", town?.Name ?? "");

    private static Standing Get(string guild, string town)
    {
        var key = (guild, town);

        if (!_standing.TryGetValue(key, out var st))
        {
            _standing[key] = st = new Standing
            {
                Guild = guild,
                Town = town,
                Next = Core.TickCount + Utility.Random(Math.Max(1, FirstMs))
            };
        }

        return st;
    }

    /// <summary>Every standing, for the store and the page.</summary>
    public static IEnumerable<Standing> All => _standing.Values;

    // ---- the look ------------------------------------------------------------------------------------------------------------

    /// <summary>The towns a guild deals with: its seat's, and every town where <see cref="LeastResidents"/> of its members live.</summary>
    public static List<BotTowns.Town> Towns(Guild guild)
    {
        List<BotTowns.Town> towns = [];

        if (guild?.Members == null)
        {
            return towns;
        }

        if (BotParley.Seat(guild, out var seat, out _) && seat != null)
        {
            towns.Add(seat);
        }

        Dictionary<BotTowns.Town, int> counts = [];

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false } member && BotResidence.Of(member) is { } town)
            {
                counts[town] = counts.GetValueOrDefault(town) + 1;
            }
        }

        foreach (var (town, n) in counts)
        {
            if (n >= LeastResidents && !towns.Contains(town))
            {
                towns.Add(town);
            }
        }

        return towns;
    }

    /// <summary>
    /// Once a look: every task standing is brought up to date and ended if done or out of time; then, if there is room, one guild
    /// sends an envoy to one town it is due to deal with.
    /// </summary>
    public static void Look(long sinceMs)
    {
        if (!Running)
        {
            return;
        }

        var now = Core.TickCount;

        foreach (var st in _standing.Values)
        {
            var task = st.Task;

            // A task read back off the save waits for its town to be read (Resolve) before it is checked or charged.
            if (task?.Town == null)
            {
                continue;
            }

            if (BotGuilds.Named(st.Guild) is not { } guild)
            {
                st.Task = null;

                continue;
            }

            if (task.Check(sinceMs))
            {
                Done(st, guild, now);

                continue;
            }

            if (now - task.Deadline >= 0)
            {
                Fail(st, guild, now);

                continue;
            }

            Charge(st, guild, task, now);
        }

        if (!BotParley.AudienceRoom || !BotTowns.Ready)
        {
            return;
        }

        foreach (var guild in BotGuilds.Standing)
        {
            if (guild == null || guild.Disbanded || guild.Name == BotUnderworld.GuildName || BotParley.Treating(guild.Name))
            {
                continue;
            }

            var towns = Towns(guild);

            for (var i = 0; i < towns.Count; i++)
            {
                var town = towns[i];
                var st = Of(guild.Name, town);

                if (st.Task != null || st.Exiled || now - st.Next < 0)
                {
                    continue;
                }

                // Held back an hour whatever comes of the call, so a town the guild cannot reach is not asked every minute.
                st.Next = now + EveryMs;

                if (BotParley.Audience(guild, town, st.Done + st.Failed == 0 ? "its first dealing with the town" : $"its standing there is ×{st.Factor:F3}") != null)
                {
                    return;
                }
            }
        }
    }

    /// <summary>The town's word: the task it named is set, and the guild is charged with it.</summary>
    public static void Give(BotHearing m)
    {
        var id = m.Verdict?.Outcome;
        BotTownTask task = null;

        for (var i = 0; m.Tasks != null && i < m.Tasks.Count && task == null; i++)
        {
            if (m.Tasks[i].Id == id)
            {
                task = m.Tasks[i];
            }
        }

        var guild = BotGuilds.Named(m.Guest);

        if (task == null || guild == null)
        {
            m.Ending = "no task was set";
            m.Applied = BotDuke.Nothing;

            return;
        }

        var now = Core.TickCount;
        var st = Of(guild.Name, m.Town);

        task.Began = now;
        task.Deadline = now + (long)(BotTownTask.Hours * 3600000);
        task.Charge = m.Verdict.Speech;
        st.Task = task;
        st.Requested = now - RequestEveryMs;
        Given++;
        _givenBy[(int)task.Kind]++;

        m.Ending = $"set: {task.Offer}";
        m.Applied = task.Id;

        Charge(st, guild, task, now);

        BotEvents.Post("parley", m.Envoy, $"{m.Town.Name} set {guild.Name} a task: {task.Offer}", "task");
    }

    /// <summary>
    /// Keeps the guild charged with its task, as a price rather than a command (<c>BotCharter</c>): a muster at the square or the
    /// hall, set again only when the guild's charter has lapsed so a maker's own orders are not overwritten; a request for the
    /// material on the guild's board, put back every <see cref="RequestEveryMs"/>.
    /// </summary>
    private static void Charge(Standing st, Guild guild, BotTownTask task, long now)
    {
        if (task.Kind == BotTownTask.Kinds.Supply)
        {
            if (now - (st.Requested + RequestEveryMs) >= 0)
            {
                st.Requested = now;

                var left = Math.Max(1, (int)Math.Ceiling(task.Need - task.Done));

                BotCharter.Want(guild, task.Material, left, $"the town of {task.Town?.Name}", $"{task.Town?.Name} asked for {task.Need:0} {task.Material} on the market");
            }

            return;
        }

        if (BotCharter.Says(guild) == "nothing")
        {
            BotCharter.Order(guild, null, null, task.Map, task.Target, $"{task.Town?.Name}'s task: {task.Offer}");
        }
    }

    private static void Done(Standing st, Guild guild, long now)
    {
        var task = st.Task;

        st.Task = null;
        st.Fails = 0;
        st.Done++;
        st.Factor = Math.Clamp(st.Factor - Step, Least, Most);
        st.Next = now + EveryMs;
        Succeeded++;
        Recount();

        logger.Warning(
            "{Guild} has done {Town}'s task ({Offer}; {Progress}) in {Minutes} minutes: its prices there are ×{Factor:F3} now",
            guild.Name,
            st.Town,
            task.Offer,
            task.Progress,
            (now - task.Began) / 60000,
            st.Factor
        );

        BotVoice.ToGuild(guild.Name, "parley:town-done", new Dictionary<string, string> { ["town"] = st.Town });
        BotEvents.Post("parley", guild.Name, null, guild.Name, task.Target.X, task.Target.Y, $"{guild.Name} did {st.Town}'s task: prices ×{st.Factor:F3}", "done", null);
    }

    private static void Fail(Standing st, Guild guild, long now)
    {
        var task = st.Task;

        st.Task = null;
        st.Fails++;
        st.Failed++;
        st.Factor = Math.Clamp(st.Factor + Step, Least, Most);
        st.Next = now + EveryMs;
        FailedAll++;
        Recount();

        logger.Warning(
            "{Guild} has failed {Town}'s task ({Offer}; {Progress}): its prices there are ×{Factor:F3} now, {Fails} failed in a row",
            guild.Name,
            st.Town,
            task.Offer,
            task.Progress,
            st.Factor,
            st.Fails
        );

        BotVoice.ToGuild(guild.Name, "parley:town-failed", new Dictionary<string, string> { ["town"] = st.Town });
        BotEvents.Post("parley", guild.Name, null, guild.Name, task.Target.X, task.Target.Y, $"{guild.Name} failed {st.Town}'s task: prices ×{st.Factor:F3}, {st.Fails} in a row", "failed", null);

        // Failed tasks in a row cost a holder the town's charters (07.10.2026, charters20). See BotCharters.Failed.
        BotCharters.Failed(guild.Name, st.Town, st.Fails);

        if (st.Fails >= FailsToExile)
        {
            Exile(st, guild);
        }
    }

    /// <summary>Put out of the town: nobody of the guild may live there until the time is up, and its seat moves if it was there.</summary>
    private static void Exile(Standing st, Guild guild)
    {
        st.Out = Core.Now.AddHours(Math.Max(0.0, ExileHours));
        st.Fails = 0;
        Exiles++;

        logger.Warning(
            "{Town} has put {Guild} out after {Fails} failed tasks in a row: none of its members may live there until {Until:yyyy-MM-dd HH:mm} UTC",
            st.Town,
            guild.Name,
            FailsToExile,
            st.Out
        );

        BotAlarm.Note("exile", $"{st.Town} put {guild.Name} out for {ExileHours:0} hours after {FailsToExile} failed tasks", FailsToExile, FailsToExile, $"{ExileHours:0}h");
        BotVoice.ToGuild(guild.Name, "parley:town-exiled", new Dictionary<string, string> { ["town"] = st.Town });

        Reseat(guild, BotTowns.Find(st.Town));
    }

    /// <summary>
    /// A guild seated by the town that put it out is seated by another: the reachable town fit to live in with the fewest seats
    /// by it, that has not put it out too, near its square and apart from the other seats — the choice <c>BotSeat.Choose</c> makes
    /// for a guild founded abroad. Written into the seats' store like a seat that followed a hall (<c>BotSeat.Set</c>, not by
    /// hand), and the hall, if it has one, is carried there by the estate's own errand.
    /// </summary>
    private static void Reseat(Guild guild, BotTowns.Town from)
    {
        var map = BotPopulation.Home;

        if (!ExileMovesSeat || guild == null || from == null || map == null || map == Map.Internal)
        {
            return;
        }

        var seat = BotSeat.Of(guild.Name);

        if (seat == Point3D.Zero || !ReferenceEquals(BotTowns.Nearest(seat), from))
        {
            return;
        }

        BotTowns.Town best = null;
        var fewest = int.MaxValue;
        var towns = BotTowns.All;

        for (var i = 0; i < towns.Count; i++)
        {
            var town = towns[i];

            if (ReferenceEquals(town, from) || !town.FromHome || BotSettle.Of(town) is not { Fit: true } || Bars(guild, town))
            {
                continue;
            }

            var seated = 0;

            foreach (var other in BotGuilds.Standing)
            {
                var at = BotSeat.Of(other?.Name);

                if (at != Point3D.Zero && Utility.InRange(at, town.Square, BotParley.SeatTownTiles))
                {
                    seated++;
                }
            }

            if (seated < fewest)
            {
                fewest = seated;
                best = town;
            }
        }

        if (best == null)
        {
            return;
        }

        for (var attempt = 0; attempt < 16; attempt++)
        {
            var x = best.Square.X + Utility.RandomMinMax(-BotSeat.TownSpread, BotSeat.TownSpread);
            var y = best.Square.Y + Utility.RandomMinMax(-BotSeat.TownSpread, BotSeat.TownSpread);

            if (!BotStep.Settle(map, x, y, out var z))
            {
                continue;
            }

            var at = new Point3D(x, y, z);

            if (BotSeat.TooNear(guild.Name, at, out _, out _))
            {
                continue;
            }

            // Not on ground another guild holds. See BotBounds.
            if (BotBounds.Foreign(guild.Name, map, at, out _))
            {
                BotBounds.SeatRefused();

                continue;
            }

            BotSeat.Set(guild.Name, at, false);
            Reseated++;

            logger.Warning("{Guild}, put out of {From}, is seated by {Town} at {X},{Y}", guild.Name, from.Name, best.Name, x, y);

            return;
        }
    }

    /// <summary>A meeting with a town ended. One that lapsed is tried again in half an hour rather than the hour.</summary>
    public static void Ended(BotHearing m)
    {
        if (m.Heard || m.Town == null)
        {
            return;
        }

        var st = Of(m.Guest, m.Town);

        st.Next = Core.TickCount + Math.Min(EveryMs, BotParley.RetryMs);
    }

    // ---- the counter ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// What this buyer's guild pays at this shopkeeper, as a multiple of the shelf: its standing in the town the shop stands in.
    /// One when nobody's prices differ anywhere, which costs a comparison.
    /// </summary>
    public static double Factor(Mobile buyer, BaseVendor vendor)
    {
        // The fair's lower prices in its town while it is open (07.10.2026, events17): on top of the guild's standing, settled the
        // same way. One everywhere else. See BotFair.Counter.
        // And the town's tax by its steward's decree (07.10.2026, steward20), for every bot. See BotStewardship.Levy.
        var fair = BotFair.Counter(buyer, vendor) * BotStewardship.Levy(buyer, vendor);

        // A town relic held by the buyer's guild: its members pay less at that town's counters (07.10.2026, relics20), settled the
        // same way. See BotRelics.Counter.
        fair *= BotRelics.Counter(buyer, vendor);

        if (_priced == 0 && !BotCouncil.AnyOutcast || buyer?.Guild is not Guild guild || vendor == null
            || BotTowns.Of(vendor.Location) is not { } town)
        {
            return fair;
        }

        var factor = _priced != 0 && _standing.TryGetValue((guild.Name, town.Name), out var st) ? st.Factor : 1.0;

        // An outcast of the town's council pays its surcharge on top (07.10.2026, politics17), and the fair's rebate (events17).
        return factor * BotCouncil.Surcharge(buyer, guild.Name, town.Name) * fair;
    }

    /// <summary>
    /// The same for one kind: a town a caravan lately brought this kind to sells it a notch cheaper (07.10.2026, events17). Asked by
    /// the counter itself (<c>BotShops.Buy</c>). See <see cref="BotCaravans.Notched"/>.
    /// </summary>
    public static double Factor(Mobile buyer, BaseVendor vendor, Type kind) => Factor(buyer, vendor) * BotCaravans.Notched(vendor, kind);

    /// <summary>
    /// The engine has taken <paramref name="bill"/> for a purchase: the guild's standing is settled on top of it — a share handed
    /// back, or the rest taken. See the class for why neither makes gold.
    /// </summary>
    /// <summary>
    /// What one of a thing at <paramref name="price"/> costs this buyer at this vendor's counter, the town's standing on top —
    /// rounded up, as <see cref="Settle"/> takes it. <b>Asked by whoever offers the errand, not only at the counter (30.09.2026,
    /// build 344):</b> the shopper priced a restock at the shelf's own figure, so Kaelthas of The Wolf (×1.010 in Minoc) holding
    /// 11gp was offered a pair of scissors at 11 twelve times in 25 minutes on 342 and refused each time at the counter,
    /// "11gp will not buy one Scissors at 12gp", three in one second (`night-r20-failures.md`).
    /// </summary>
    public static int Dear(Mobile buyer, BaseVendor vendor, int price)
    {
        var factor = Factor(buyer, vendor);

        return factor > 1.0 ? (int)Math.Ceiling(price * factor) : price;
    }

    public static void Settle(Mobile buyer, int bill, double factor)
    {
        if (buyer == null || bill <= 0 || Math.Abs(factor - 1.0) < 0.0001)
        {
            return;
        }

        if (factor < 1.0)
        {
            var back = (int)Math.Floor(bill * (1.0 - factor));

            if (back > 0 && back < bill)
            {
                Banker.Deposit(buyer, back);
                Rebates++;
                Rebated += back;
                BotFair.Rebated(buyer, back);
                BotRelics.Rebated(buyer, back);
                BotStewardship.Settled(buyer, bill, factor);
            }

            return;
        }

        var more = (int)Math.Ceiling(bill * (factor - 1.0));

        if (more <= 0)
        {
            return;
        }

        if (BotMarket.Charge(buyer, more))
        {
            Surcharges++;
            Surcharged += more;

            // The outcast's share of it is the town's tax, into its purse rather than out of the world (07.10.2026).
            BotCouncil.Taxed(buyer, bill, factor, more);
            BotStewardship.Settled(buyer, bill, factor);

            return;
        }

        Unpaid++;
    }

    /// <summary>
    /// A guild laid claim to another guild's ground: every town charges it <paramref name="by"/> more, up to <see cref="Most"/>
    /// (Patrick, 06.10.2026: "every town's rating towards that guild falls"; <c>BotSeizure</c>). Written into the same factor a
    /// task moves, so it is kept by the store, read at every counter, and won back only by tasks done — half a percent each.
    /// Returns how many towns' standing rose; <paramref name="dearest"/> is the guild's highest factor after it, and
    /// <paramref name="added"/> the sum of what was actually added.
    /// </summary>
    public static int Shame(string guild, double by, out double dearest, out double added)
    {
        dearest = 1.0;
        added = 0.0;

        if (!Running || string.IsNullOrEmpty(guild) || by <= 0.0)
        {
            return 0;
        }

        var raised = 0;
        var towns = BotTowns.All;

        for (var i = 0; i < towns.Count; i++)
        {
            var st = Of(guild, towns[i]);
            var was = st.Factor;

            st.Factor = Math.Clamp(was + by, Least, Most);

            if (st.Factor - was > 0.0001)
            {
                raised++;
                added += st.Factor - was;
            }

            dearest = Math.Max(dearest, st.Factor);
        }

        Recount();

        return raised;
    }

    private static void Recount()
    {
        _priced = 0;

        foreach (var st in _standing.Values)
        {
            if (Math.Abs(st.Factor - 1.0) > 0.0001)
            {
                _priced++;
            }
        }
    }

    // ---- living there ---------------------------------------------------------------------------------------------------------

    /// <summary>Whether this guild is put out of this town now.</summary>
    public static bool Bars(Guild guild, BotTowns.Town town) =>
        guild != null && town != null && _standing.TryGetValue((guild.Name, town.Name), out var st) && st.Exiled;

    /// <summary>The same, asked when a bot chooses where to live, and counted when it turns a town away.</summary>
    public static bool Turns(BotMobile bot, BotTowns.Town town)
    {
        if (Exiles == 0 && _exiledAtLoad == 0 || !Bars(bot?.Guild as Guild, town))
        {
            return false;
        }

        TurnedAway++;

        return true;
    }

    private static int _exiledAtLoad;

    // ---- the goods -------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Goods went out to a buyer — over a shop's till by the bank, or at a carrier's meeting — from this seller. Counted towards its
    /// guild's supply task for the material, in whatever town it stands. Called from <c>BotShopkeep.Serve</c> and
    /// <c>BotHandover.Exchange</c> (the auction's List and Fill until 01.10.2026).
    /// </summary>
    public static void Supplied(Mobile seller, Type kind, int units)
    {
        if (Given == 0 && _tasksAtLoad == 0 || units <= 0 || seller?.Guild is not Guild guild)
        {
            return;
        }

        string material = null;

        foreach (var st in _standing.Values)
        {
            if (st.Task is not { Kind: BotTownTask.Kinds.Supply } task || st.Guild != guild.Name)
            {
                continue;
            }

            material ??= BotTownTask.MaterialOf(kind);

            if (material == null)
            {
                return;
            }

            if (task.Material == material)
            {
                task.Done += units;
                SuppliedUnits += units;
            }
        }
    }

    private static int _tasksAtLoad;

    /// <summary>Whether another guild is already working this square, or this material, for this town. See the catalogue's "unique".</summary>
    public static bool Taken(BotTowns.Town town, Point3D square, string material)
    {
        foreach (var st in _standing.Values)
        {
            if (st.Task is not { } task || !ReferenceEquals(task.Town, town))
            {
                continue;
            }

            if (material != null ? task.Material == material : task.Kind == BotTownTask.Kinds.Clear && BotQuad.Key(task.Map, task.Target) == BotQuad.Key(task.Map, square))
            {
                return true;
            }
        }

        return false;
    }

    // ---- telling ----------------------------------------------------------------------------------------------------------------

    /// <summary>Every guild's standing that is not plain, one clause each.</summary>
    public static string Tell()
    {
        using var say = Server.Text.ValueStringBuilder.Create(256);

        foreach (var st in _standing.Values)
        {
            // A town the guild never dealt with still shows when its prices to the guild moved: a seizure raises them everywhere.
            if (st.Task == null && st.Done + st.Failed == 0 && !st.Exiled && Math.Abs(st.Factor - 1.0) <= 0.0001)
            {
                continue;
            }

            say.Append(say.Length > 0 ? "; " : "");
            say.Append($"{st.Guild} in {st.Town} ×{st.Factor:F3} ({st.Done} done, {st.Failed} failed, {st.Fails} in a row)");

            if (st.Exiled)
            {
                say.Append($", put out until {st.Out:dd.MM HH:mm} UTC");
            }

            if (st.Task != null)
            {
                say.Append($", tasked to {st.Task.Offer} — {st.Task.Progress}, {(st.Task.Deadline - Core.TickCount) / 60000} min left");
            }
        }

        return say.Length == 0 ? "no guild has dealt with a town yet" : say.ToString();
    }

    public static string Describe() =>
        !Running
            ? "guilds do not deal with their towns"
            : $"the towns: {Tell()}; {Given} tasks set ({_givenBy[0]} to clear ground, {_givenBy[1]} to keep watch, {_givenBy[2]} to supply the market), {Succeeded} done and {FailedAll} failed (±{Step:P1} each, between ×{Least:F2} and ×{Most:F2}), {Exiles} guilds put out of a town for {ExileHours:0}h after {FailsToExile} in a row ({Reseated} seated elsewhere), {TurnedAway} choices of a home that passed a town over for it; "
            + $"{Rebates} purchases at a guild's better price ({Rebated}gp handed back), {Surcharges} at a worse one ({Surcharged}gp taken on top, {Unpaid} that could not be), {SuppliedUnits} units counted towards a supply; {BotCouncil.Describe()}";

    // ---- the store ------------------------------------------------------------------------------------------------------------

    internal static void Save(IGenericWriter writer)
    {
        var now = Core.TickCount;
        List<Standing> kept = [];

        foreach (var st in _standing.Values)
        {
            if (st.Task != null || st.Done + st.Failed > 0 || st.Exiled || Math.Abs(st.Factor - 1.0) > 0.0001)
            {
                kept.Add(st);
            }
        }

        writer.WriteEncodedInt(kept.Count);

        for (var i = 0; i < kept.Count; i++)
        {
            var st = kept[i];

            writer.Write(st.Guild);
            writer.Write(st.Town);
            writer.Write(st.Factor);
            writer.WriteEncodedInt(st.Fails);
            writer.WriteEncodedInt(st.Done);
            writer.WriteEncodedInt(st.Failed);
            writer.Write(st.Out ?? DateTime.MinValue);

            var task = st.Task;

            writer.Write(task != null);

            if (task == null)
            {
                continue;
            }

            writer.WriteEncodedInt((int)task.Kind);
            writer.Write(task.Id ?? "");
            writer.Write(task.Material ?? "");
            writer.Write(task.Target);
            writer.Write(task.Need);
            writer.Write(task.Done);
            writer.Write(task.Presence);
            writer.Write(task.StartReading);
            writer.Write(Math.Max(0, now - task.Began));
            writer.Write(Math.Max(0, task.Deadline - now));
            writer.Write(task.Charge ?? "");
        }
    }

    internal static int Load(IGenericReader reader)
    {
        _standing.Clear();

        var many = reader.ReadEncodedInt();
        var now = Core.TickCount;

        for (var i = 0; i < many; i++)
        {
            var guild = reader.ReadString();
            var town = reader.ReadString();
            var st = new Standing
            {
                Guild = guild,
                Town = town,
                Factor = reader.ReadDouble(),
                Fails = reader.ReadEncodedInt(),
                Done = reader.ReadEncodedInt(),
                Failed = reader.ReadEncodedInt()
            };

            var until = reader.ReadDateTime();

            st.Out = until == DateTime.MinValue ? null : until;
            st.Next = now + Utility.Random(Math.Max(1, FirstMs));

            if (reader.ReadBool())
            {
                var kind = (BotTownTask.Kinds)reader.ReadEncodedInt();
                var id = reader.ReadString();
                var material = reader.ReadString();
                var target = reader.ReadPoint3D();
                var need = reader.ReadDouble();
                var done = reader.ReadDouble();
                var presence = reader.ReadDouble();
                var start = reader.ReadDouble();
                var ran = reader.ReadLong();
                var left = reader.ReadLong();
                var charge = reader.ReadString();

                // The town is found by name once the towns are read; until then the task is kept by its name alone.
                st.Task = new BotTownTask
                {
                    Id = id,
                    Kind = kind,
                    Guild = guild,
                    Town = null,
                    Map = BotPopulation.Home,
                    Target = target,
                    Material = string.IsNullOrEmpty(material) ? null : material,
                    Need = need,
                    Done = done,
                    Presence = presence,
                    StartReading = start,
                    Began = now - ran,
                    Deadline = now + left,
                    Charge = charge
                };

                _tasksAtLoad++;
            }

            if (st.Exiled)
            {
                _exiledAtLoad++;
            }

            if (!string.IsNullOrEmpty(guild) && !string.IsNullOrEmpty(town))
            {
                _standing[(guild, town)] = st;
            }
        }

        Recount();

        return _standing.Count;
    }

    /// <summary>
    /// The tasks read back off the save are given their towns once the towns are read, and their map. Called by the module when
    /// the towns answer; a task whose town is no longer on the map is dropped.
    /// </summary>
    internal static void Resolve()
    {
        foreach (var st in _standing.Values)
        {
            if (st.Task is not { Town: null } task)
            {
                continue;
            }

            var town = BotTowns.Find(st.Town);

            if (town == null)
            {
                st.Task = null;

                continue;
            }

            st.Task = new BotTownTask
            {
                Id = task.Id,
                Kind = task.Kind,
                Guild = task.Guild,
                Town = town,
                Map = BotPopulation.Home,
                Target = task.Target,
                Material = task.Material,
                Need = task.Need,
                Done = task.Done,
                Presence = task.Presence,
                StartReading = task.StartReading,
                Began = task.Began,
                Deadline = task.Deadline,
                Charge = task.Charge
            };
        }
    }

    public static void Forget()
    {
        Given = 0;
        Succeeded = 0;
        FailedAll = 0;
        Exiles = 0;
        Reseated = 0;
        Array.Clear(_givenBy);
        Rebates = 0;
        Rebated = 0;
        Surcharges = 0;
        Surcharged = 0;
        Unpaid = 0;
        TurnedAway = 0;
        SuppliedUnits = 0;
    }

    /// <summary>Every guild's standing in every town forgotten, for Patrick's reset.</summary>
    public static int Wipe()
    {
        var gone = _standing.Count;

        _standing.Clear();
        _priced = 0;
        _exiledAtLoad = 0;
        _tasksAtLoad = 0;
        Forget();

        return gone;
    }
}
