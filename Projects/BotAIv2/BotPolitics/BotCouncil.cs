using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Guilds;
using Server.Logging;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// The towns' councils: in each town the guild with the best standing collects the town's taxes for a tenth of them, the next
/// captains its guard for the town's wage, and a guild the post-holders cannot abide pays more at the town's counters.
///
/// <para>
/// <b>Patrick's order of 07.10.2026, night: "a town council: the guilds with the best standing in a town get a post (tax
/// collector, captain of the guard) and an income from it; a tax on an outcast guild".</b> The standing is the one the towns
/// already keep (<c>BotBurgh</c>): what the town's shops charge the guild, moved half a percent by every task done or failed
/// and raised by every seizure. Every <see cref="TermMs"/> the posts are given again: in each town, among the guilds that have
/// done <see cref="LeastDone"/> of its tasks or more and are not put out of it, the cheapest (then the most tasks done) is its
/// <b>tax collector</b> and the next its <b>captain of the guard</b>.
/// </para>
///
/// <para>
/// <b>The money is the town's own.</b> The island has one treasury (<c>BotCity</c>), and what it is taxed — ground, houses,
/// outlaws' fines, the outcasts' surcharge — is the towns' tax income. At the end of each term the treasury's tax take over the
/// term is shared evenly among the towns that have a collector, and each collector's chest is paid <see cref="TaxShare"/> of
/// its town's share out of the purse (<c>BotCity.Escrow</c> and <c>Disbursed</c>: the purse goes down by what the chest goes
/// up by; an empty purse pays nothing, counted). The captain's members are offered the town's guard for a wage of
/// <see cref="Wage"/>gp a watch, held from the same purse when the watch is taken, up to <see cref="WageBudget"/>gp a town a
/// term (<see cref="BotGuardDuty"/>).
/// </para>
///
/// <para>
/// <b>Outcasts.</b> A guild put out of a town (<c>BotBurgh.Bars</c>), or thought <see cref="OutcastRegard"/> of or worse by
/// either of the town's post-holders, pays <see cref="OutcastFactor"/> times at the town's counters — on top of its standing,
/// through the same factor every counter reads (<c>BotBurgh.Factor</c>) — and that part of the bill goes into the treasury
/// as tax (<see cref="Taxed"/>) rather than out of the world.
/// </para>
/// </summary>
public static class BotCouncil
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCouncil));

    /// <summary>Whether the towns give posts at all. Off: no posts, no commission, no wage, no outcasts.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>A council's term: two hours of running time.</summary>
    public static int TermMs { get; set; } = 7200000;

    /// <summary>The first posts are given this long after the module starts: a quarter of an hour.</summary>
    public static int FirstMs { get; set; } = 900000;

    /// <summary>The collector's share of its town's tax income.</summary>
    public static double TaxShare { get; set; } = 0.10;

    /// <summary>Tasks a guild must have done for a town to hold a post in it.</summary>
    public static int LeastDone { get; set; } = 1;

    /// <summary>A post-holder's regard of a guild at or under which the guild is the town's outcast.</summary>
    public static double OutcastRegard { get; set; } = -60.0;

    /// <summary>What an outcast pays at the town's counters, as a multiple.</summary>
    public static double OutcastFactor { get; set; } = 1.05;

    /// <summary>Whether the captain's members are offered the town's guard for a wage.</summary>
    public static bool Captaincy { get; set; } = true;

    /// <summary>The town's wage for one watch.</summary>
    public static int Wage { get; set; } = 20;

    /// <summary>The most a town pays in wages in one term.</summary>
    public static int WageBudget { get; set; } = 400;

    /// <summary>Members of the captain's guild on the town's guard at once.</summary>
    public static int MostOnDuty { get; set; } = 2;

    /// <summary>How far from a town its captain's members are offered the guard.</summary>
    public static int Reach { get; set; } = 250;

    /// <summary>One town's council for this term.</summary>
    private sealed class Seat
    {
        public string Town;

        public string Collector;

        public string Captain;

        /// <summary>Wages held or paid this term.</summary>
        public int Wages;

        public int OnDuty;

        /// <summary>The wage budget a steward added this term, for its captain or its guard line (07.10.2026, steward20).</summary>
        public int Extra;

        /// <summary>The captain a steward hired, kept through the council's terms until <see cref="HiredUntil"/>.</summary>
        public string Hired;

        public long HiredUntil;
    }

    private static readonly Dictionary<string, Seat> _seats = new(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<(string Guild, string Town)> _outcast = [];

    private static readonly HashSet<(string Guild, string Town)> _saidOutcast = [];

    private static readonly List<BotBurgh.Standing> _ranked = [];

    private static long _termAt;

    private static long _taxMark;

    private static bool _opened;

    private static (Serial Buyer, double Factor, long Tick) _quote;

    /// <summary>Terms given, posts given and posts that changed hands.</summary>
    public static long Terms { get; private set; }

    public static long Posts { get; private set; }

    public static long Changed { get; private set; }

    /// <summary>Commissions paid to collectors' chests, their gold, and commissions the purse could not pay.</summary>
    public static long Commissions { get; private set; }

    public static long CommissionGold { get; private set; }

    public static long CommissionsUnpaid { get; private set; }

    /// <summary>Watches of the town's guard paid, their wages, watches the purse could not hold a wage for, and town-terms whose wage budget was spent.</summary>
    public static long Watches { get; private set; }

    public static long WageGold { get; private set; }

    public static long PurseShort { get; private set; }

    public static long BudgetSpent { get; private set; }

    /// <summary>Purchases at an outcast's surcharge, and the gold of the surcharge that went to the treasury as tax.</summary>
    public static long OutcastBuys { get; private set; }

    public static long OutcastGold { get; private set; }

    /// <summary>Whether any guild is an outcast anywhere now: the counter skips the lookup when none is.</summary>
    public static bool AnyOutcast => Running && _outcast.Count > 0;

    /// <summary>The module started: the first term begins <see cref="FirstMs"/> from now and the tax is counted from here.</summary>
    public static void Open(long now)
    {
        _termAt = now - Math.Max(0, TermMs) + Math.Max(0, FirstMs);
        _taxMark = BotCity.Taxed;
        _opened = true;
    }

    // ---- the counter -------------------------------------------------------------------------------------------------------

    /// <summary>
    /// What this guild pays at this town's counters on top of its standing: <see cref="OutcastFactor"/> for an outcast, else one.
    /// Asked by <c>BotBurgh.Factor</c>; remembers the quote so the bill's settling can tax the outcast's share.
    /// </summary>
    public static double Surcharge(Mobile buyer, string guild, string town)
    {
        if (!Running || _outcast.Count == 0 || buyer == null || !_outcast.Contains((guild, town)) || OutcastFactor <= 1.0)
        {
            return 1.0;
        }

        _quote = (buyer.Serial, OutcastFactor, Core.TickCount);

        return OutcastFactor;
    }

    /// <summary>
    /// A surcharge of <paramref name="more"/> on a bill of <paramref name="bill"/> at <paramref name="factor"/> was taken from the
    /// buyer: the outcast's share of it goes into the treasury. From <c>BotBurgh.Settle</c>.
    /// </summary>
    public static void Taxed(Mobile buyer, int bill, double factor, int more)
    {
        if (buyer == null || more <= 0 || _quote.Buyer != buyer.Serial || _quote.Tick != Core.TickCount || _quote.Factor <= 1.0)
        {
            return;
        }

        var share = (int)Math.Min(more, Math.Ceiling(bill * (factor - factor / _quote.Factor)));

        _quote = default;

        if (share <= 0)
        {
            return;
        }

        BotCity.Tax(share);
        OutcastBuys++;
        OutcastGold += share;
    }

    // ---- the posts ---------------------------------------------------------------------------------------------------------

    /// <summary>Whether any town has a captain of its guard now: the proposer's first, cheapest question.</summary>
    public static bool AnyCaptain { get; private set; }

    /// <summary>The nearest town within <paramref name="reach"/> of <paramref name="at"/> whose guard this guild captains, or null.</summary>
    public static BotTowns.Town CaptainOf(string guild, Point3D at, int reach)
    {
        if (!AnyCaptain || guild == null)
        {
            return null;
        }

        BotTowns.Town best = null;
        var bestDistance = int.MaxValue;

        foreach (var (_, seat) in _seats)
        {
            if (seat.Captain != guild || BotTowns.Find(seat.Town) is not { } town)
            {
                continue;
            }

            var distance = Math.Max(Math.Abs(town.Square.X - at.X), Math.Abs(town.Square.Y - at.Y));

            if (distance <= reach && distance < bestDistance)
            {
                best = town;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>Whether this guild captains this town's guard. Asked by <see cref="BotGuardDuty"/>.</summary>
    public static bool Captains(string guild, BotTowns.Town town) =>
        Running && Captaincy && guild != null && town != null && _seats.TryGetValue(town.Name, out var seat) && seat.Captain == guild;

    /// <summary>Whether this town can hold one more watch of its captain's members, wage and budget allowing.</summary>
    public static bool Room(BotTowns.Town town)
    {
        if (town == null || !_seats.TryGetValue(town.Name, out var seat))
        {
            return false;
        }

        // Asked by the proposer on the beat, so nothing is counted here; refusals are counted where a watch is held.
        return seat.OnDuty < MostOnDuty && seat.Wages + Wage <= WageBudget + seat.Extra && BotCity.Purse >= Wage;
    }

    /// <summary>A watch was taken: its wage is held out of the purse and against the town's budget. False if the purse cannot.</summary>
    public static bool Hold(BotTowns.Town town)
    {
        if (town == null || !_seats.TryGetValue(town.Name, out var seat) || !BotCity.Escrow(Wage))
        {
            PurseShort++;

            return false;
        }

        seat.Wages += Wage;
        seat.OnDuty++;

        // The town's budget for the term is spent with this watch: counted once a town a term.
        if (seat.Wages + Wage > WageBudget + seat.Extra)
        {
            BudgetSpent++;
        }

        return true;
    }

    /// <summary>A watch ended. Paid: the held wage was handed to the watchman. Not paid: it goes back into the purse.</summary>
    public static void Release(BotTowns.Town town, bool paid)
    {
        _seats.TryGetValue(town?.Name ?? "", out var seat);

        if (seat != null)
        {
            seat.OnDuty = Math.Max(0, seat.OnDuty - 1);
        }

        if (paid)
        {
            BotCity.Disbursed(Wage);
            Watches++;
            WageGold += Wage;

            return;
        }

        BotCity.Refund(Wage);

        if (seat != null)
        {
            seat.Wages = Math.Max(0, seat.Wages - Wage);
        }
    }

    /// <summary>Once a minute: the term's end and the new posts when due, and the outcasts read again.</summary>
    public static void Look(long now)
    {
        if (!Running)
        {
            _outcast.Clear();

            return;
        }

        if (!_opened)
        {
            Open(now);
        }

        if (BotTowns.Surveyed && now - _termAt >= TermMs)
        {
            EndTerm(now);
            Assign(now);
            Recount();
        }

        Outcasts();
    }

    /// <summary>The term's commission: a tenth of each collector's town's share of the tax taken over the term.</summary>
    private static void EndTerm(long now)
    {
        var taxed = BotCity.Taxed - _taxMark;

        _taxMark = BotCity.Taxed;

        var collectors = 0;

        foreach (var (_, seat) in _seats)
        {
            if (seat.Collector != null)
            {
                collectors++;
            }
        }

        if (taxed <= 0 || collectors == 0 || TaxShare <= 0.0)
        {
            return;
        }

        var fee = (int)Math.Min(int.MaxValue, (long)(TaxShare * taxed / collectors));

        if (fee <= 0)
        {
            return;
        }

        foreach (var (_, seat) in _seats)
        {
            if (seat.Collector == null || BotGuilds.Named(seat.Collector) is not { Disbanded: false })
            {
                continue;
            }

            if (!BotCity.Escrow(fee))
            {
                CommissionsUnpaid++;

                logger.Information(
                    "{Guild} collected {Town}'s taxes this term and the treasury could not pay its {Fee}gp: {Purse}gp in the purse",
                    seat.Collector,
                    seat.Town,
                    fee,
                    BotCity.Purse
                );

                continue;
            }

            BotCity.Disbursed(fee);
            BotChest.Put(seat.Collector, fee);
            Commissions++;
            CommissionGold += fee;

            logger.Information(
                "{Guild} was paid {Fee}gp for collecting {Town}'s taxes: {Share:P0} of the town's share of {Taxed}gp taxed over the term among {Towns} towns; its chest holds {Chest}gp",
                seat.Collector,
                fee,
                seat.Town,
                TaxShare,
                taxed,
                collectors,
                BotChest.Holds(seat.Collector)
            );
        }
    }

    private static void Assign(long now)
    {
        _termAt = now;
        Terms++;

        var towns = BotTowns.All;

        for (var t = 0; t < towns.Count; t++)
        {
            var town = towns[t];

            _ranked.Clear();

            foreach (var st in BotBurgh.All)
            {
                if (st.Town != town.Name || st.Exiled || st.Done < LeastDone || BotGuilds.Named(st.Guild) is not { Disbanded: false } g
                    || BotUnderworld.Band(g))
                {
                    continue;
                }

                var at = _ranked.Count;

                while (at > 0 && Better(st, _ranked[at - 1]))
                {
                    at--;
                }

                _ranked.Insert(at, st);
            }

            _seats.TryGetValue(town.Name, out var seat);

            var collector = _ranked.Count > 0 ? _ranked[0].Guild : null;
            var captain = _ranked.Count > 1 ? _ranked[1].Guild : null;

            // A captain the town's steward hired keeps the post, and its wages, until the hire is out (07.10.2026, steward20).
            if (seat?.Hired != null && seat.HiredUntil - now > 0 && seat.Hired != collector && BotGuilds.Named(seat.Hired) is { Disbanded: false })
            {
                captain = seat.Hired;
            }
            else if (seat != null)
            {
                seat.Hired = null;
                seat.Extra = 0;
            }

            if (seat == null)
            {
                if (collector == null)
                {
                    continue;
                }

                _seats[town.Name] = seat = new Seat { Town = town.Name };
            }

            var wasCollector = seat.Collector;
            var wasCaptain = seat.Captain;

            seat.Collector = collector;
            seat.Captain = captain;
            seat.Wages = 0;

            Posts += (collector != null ? 1 : 0) + (captain != null ? 1 : 0);

            if (collector == wasCollector && captain == wasCaptain)
            {
                continue;
            }

            Changed++;

            logger.Information(
                "{Town}'s council for the next {Hours}h: {Collector} collects its taxes{CollectorWhy}, {Captain} captains its guard{CaptainWhy}{Was}",
                town.Name,
                TermMs / 3600000,
                collector ?? "nobody",
                _ranked.Count > 0 ? $" (×{_ranked[0].Factor:F3}, {_ranked[0].Done} tasks done)" : "",
                captain ?? "nobody",
                _ranked.Count > 1 ? $" (×{_ranked[1].Factor:F3}, {_ranked[1].Done} tasks done)" : "",
                wasCollector != null || wasCaptain != null ? $"; before, {wasCollector ?? "nobody"} and {wasCaptain ?? "nobody"}" : ""
            );

            var fill = new Dictionary<string, string> { ["town"] = town.Name };

            if (collector != null && collector != wasCollector)
            {
                BotVoice.ToGuild(collector, "politics:collector", fill);
                BotEvents.Post("politics", BotGuilds.Head(BotGuilds.Named(collector)), $"{collector} collects {town.Name}'s taxes");
            }

            if (captain != null && captain != wasCaptain)
            {
                BotVoice.ToGuild(captain, "politics:captain", fill);
                BotEvents.Post("politics", BotGuilds.Head(BotGuilds.Named(captain)), $"{captain} captains {town.Name}'s guard");
            }
        }
    }

    /// <summary>Whether any seat has a captain: read after the posts change.</summary>
    private static void Recount()
    {
        AnyCaptain = false;

        foreach (var (_, seat) in _seats)
        {
            if (seat.Captain != null)
            {
                AnyCaptain = true;

                return;
            }
        }
    }

    // ---- the steward's word (07.10.2026, steward20) -------------------------------------------------------------------------

    /// <summary>The guild that captains this town's guard now, or null.</summary>
    public static string CaptainIn(string town) => Running && town != null && _seats.TryGetValue(town, out var seat) ? seat.Captain : null;

    /// <summary>The guild that collects this town's taxes now, or null.</summary>
    public static string CollectorIn(string town) => Running && town != null && _seats.TryGetValue(town, out var seat) ? seat.Collector : null;

    /// <summary>Whether this guild is this town's outcast now.</summary>
    public static bool IsOutcast(string guild, string town) => Running && guild != null && town != null && _outcast.Contains((guild, town));

    /// <summary>What is left of this town's wage budget this term, a steward's money included.</summary>
    public static int BudgetLeft(string town) =>
        town != null && _seats.TryGetValue(town, out var seat) ? Math.Max(0, WageBudget + seat.Extra - seat.Wages) : 0;

    /// <summary>Whether this guild may hold a post in this town: the council's own test (tasks done, not put out, not the band).</summary>
    public static bool Eligible(Guild guild, BotTowns.Town town)
    {
        if (guild is not { Disbanded: false } || town == null || BotUnderworld.Band(guild))
        {
            return false;
        }

        foreach (var st in BotBurgh.All)
        {
            if (st.Guild == guild.Name && st.Town == town.Name)
            {
                return !st.Exiled && st.Done >= LeastDone;
            }
        }

        return false;
    }

    /// <summary>
    /// The town's steward hires this guild as captain of the guard until <paramref name="until"/>, and raises the town's wage
    /// budget by <paramref name="budget"/> — coin the steward has handed back to the purse, paid out per watch stood like any
    /// wage. Kept through the council's next term while the hire stands. False when the captaincy is off.
    /// </summary>
    public static bool Appoint(BotTowns.Town town, string guild, int budget, long until)
    {
        if (!Running || !Captaincy || town == null || guild == null)
        {
            return false;
        }

        if (!_seats.TryGetValue(town.Name, out var seat))
        {
            _seats[town.Name] = seat = new Seat { Town = town.Name };
        }

        var was = seat.Captain;

        seat.Captain = guild;
        seat.Hired = guild;
        seat.HiredUntil = until;
        seat.Extra += Math.Max(0, budget);
        Recount();

        if (was != guild)
        {
            Changed++;
            BotVoice.ToGuild(guild, "politics:captain", new Dictionary<string, string> { ["town"] = town.Name });

            logger.Information("{Town}'s steward hired {Guild} to captain its guard instead of {Was}, with {Budget}gp more for its watches", town.Name, guild, was ?? "nobody", budget);
        }

        return true;
    }

    /// <summary>A steward's money for the guard's watches this term, for its guard line. Nothing when the town has no council seat.</summary>
    public static void Fund(string town, int budget)
    {
        if (town != null && budget > 0 && _seats.TryGetValue(town, out var seat))
        {
            seat.Extra += budget;
        }
    }

    /// <summary>The better standing: the cheaper, then the more tasks done, then the fewer failed.</summary>
    private static bool Better(BotBurgh.Standing a, BotBurgh.Standing b) =>
        a.Factor < b.Factor - 0.0001 || Math.Abs(a.Factor - b.Factor) <= 0.0001 && (a.Done > b.Done || a.Done == b.Done && a.Failed < b.Failed);

    /// <summary>Who each town's post-holders will not abide, read afresh once a minute. See the class note.</summary>
    private static void Outcasts()
    {
        _outcast.Clear();

        if (_seats.Count == 0)
        {
            return;
        }

        foreach (var (_, seat) in _seats)
        {
            if (seat.Collector == null && seat.Captain == null)
            {
                continue;
            }

            var town = BotTowns.Find(seat.Town);

            foreach (var guild in BotGuilds.Standing)
            {
                if (guild is not { Disbanded: false } || guild.Name == seat.Collector || guild.Name == seat.Captain)
                {
                    continue;
                }

                var outcast = town != null && BotBurgh.Bars(guild, town)
                              || seat.Collector != null && BotRegard.Of(seat.Collector, guild.Name) <= OutcastRegard
                              || seat.Captain != null && BotRegard.Of(seat.Captain, guild.Name) <= OutcastRegard;

                if (!outcast)
                {
                    continue;
                }

                _outcast.Add((guild.Name, seat.Town));

                if (_saidOutcast.Add((guild.Name, seat.Town)))
                {
                    logger.Information(
                        "{Guild} is {Town}'s outcast: {Why}; it pays ×{Factor:F2} at the town's counters",
                        guild.Name,
                        seat.Town,
                        town != null && BotBurgh.Bars(guild, town) ? "put out of the town" : $"its post-holders think {OutcastRegard:F0} of it or worse",
                        OutcastFactor
                    );

                    BotVoice.ToGuild(guild.Name, "politics:outcast", new Dictionary<string, string> { ["town"] = seat.Town });
                }
            }
        }

        // A guild no longer an outcast may be told again if it becomes one.
        _saidOutcast.IntersectWith(_outcast);
    }

    // ---- telling -----------------------------------------------------------------------------------------------------------

    public static void Write(Utf8JsonWriter w, string guild)
    {
        w.WritePropertyName("posts");
        w.WriteStartArray();

        foreach (var (_, seat) in _seats)
        {
            if (seat.Collector == guild)
            {
                w.WriteStartObject();
                w.WriteString("town", seat.Town);
                w.WriteString("post", "tax collector");
                w.WriteEndObject();
            }

            if (seat.Captain == guild)
            {
                w.WriteStartObject();
                w.WriteString("town", seat.Town);
                w.WriteString("post", "captain of the guard");
                w.WriteNumber("onDuty", seat.OnDuty);
                w.WriteNumber("wages", seat.Wages);
                w.WriteEndObject();
            }
        }

        w.WriteEndArray();

        w.WritePropertyName("outcastIn");
        w.WriteStartArray();

        foreach (var (g, town) in _outcast)
        {
            if (g == guild)
            {
                w.WriteStringValue(town);
            }
        }

        w.WriteEndArray();
    }

    /// <summary>The <c>Diplomacy:</c> line's clause, after the towns'.</summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "the towns' councils are OFF";
        }

        using var say = ValueStringBuilder.Create(512);

        say.Append(
            $"councils every {TermMs / 60000}m: {Terms} terms, {Posts} posts given ({Changed} councils changed); {Commissions} commissions of {TaxShare:P0} paid to collectors ({CommissionGold}gp from the treasury, {CommissionsUnpaid} the purse could not pay); "
        );
        say.Append(
            Captaincy
                ? $"{Watches} watches of the town's guard paid {Wage}gp each ({WageGold}gp, up to {WageBudget}gp a town a term, {MostOnDuty} on duty at once; {BotGuardDuty.Taken} taken, {PurseShort} the purse could not hold a wage for, {BudgetSpent} town-terms with the budget spent, {BotGuardDuty.Unpaid} ended unpaid); "
                : "the captaincy's wage is OFF; "
        );
        say.Append($"outcasts at regard {OutcastRegard:F0} or put out pay ×{OutcastFactor:F2}: {_outcast.Count} now, {OutcastBuys} purchases taxed {OutcastGold}gp into the treasury; posts: ");

        var first = true;

        foreach (var (_, seat) in _seats)
        {
            say.Append(first ? "" : ", ");
            first = false;
            say.Append($"{seat.Town} {seat.Collector ?? "-"}/{seat.Captain ?? "-"}");
        }

        if (first)
        {
            say.Append("none yet");
        }

        return say.ToString();
    }

    // ---- the store ---------------------------------------------------------------------------------------------------------

    /// <summary>The watches' wages read back as held at the last save, handed back to the purse at the module's start. See <see cref="Settle"/>.</summary>
    private static int _wagesAtLoad;

    /// <summary>What the watches on duty hold of the purse now: <see cref="Wage"/> a watch, as <see cref="Release"/> hands it back.</summary>
    private static int HeldWages()
    {
        long held = 0;

        foreach (var (_, seat) in _seats)
        {
            held += (long)Math.Max(0, seat.OnDuty) * Math.Max(0, Wage);
        }

        return (int)Math.Min(int.MaxValue, held);
    }

    /// <summary>
    /// The watches' wages held at the save (07.10.2026, reset26 review; <c>BotPoliticsStore</c> shape 2). Written after the
    /// tributes, so that a shape-1 save still reads as it did.
    /// </summary>
    // The wages read back and not yet settled ride along too (review B of reset26b, 07.10.2026): with the politics module
    // off at boot nobody settles them, and a save writing only the live watches' wages would drop them.
    internal static void SaveHeld(IGenericWriter writer) => writer.WriteEncodedInt((int)Math.Min(int.MaxValue, (long)HeldWages() + _wagesAtLoad));

    internal static int LoadHeld(IGenericReader reader)
    {
        _wagesAtLoad = Math.Max(0, reader.ReadEncodedInt());

        return _wagesAtLoad;
    }

    /// <summary>
    /// <b>The wages a restart caught on duty, back into the purse (07.10.2026, reset26 review).</b> A watch holds its wage out of
    /// the purse (<see cref="Hold"/>); the purse was saved without it and nothing wrote it down, so at every ordinary restart that
    /// coin was destroyed. It is written now (<see cref="SaveHeld"/>), and handed back here rather than held again: the watch
    /// itself does not survive a restart — a bot comes back knowing only the kind of work it was doing, not the deed — so a wage
    /// held again would have nobody to pay it or give it back, and would stand counted as held for ever. Counted as held and
    /// refunded in one step, so the purse's held count is left as it was and the purse rises by the coin. From the module's start,
    /// once the treasury is read. Returns the gold.
    /// </summary>
    public static int Settle()
    {
        var gold = _wagesAtLoad;

        _wagesAtLoad = 0;

        if (gold <= 0)
        {
            return 0;
        }

        BotCity.Held(gold);
        BotCity.Refund(gold);

        logger.Information("Councils: {Gold}gp of wages held for watches on duty at the last save handed back to the treasury", gold);

        return gold;
    }

    internal static void Save(IGenericWriter writer)
    {
        writer.Write(Math.Max(0L, Core.TickCount - _termAt));
        writer.WriteEncodedInt(_seats.Count);

        foreach (var (_, seat) in _seats)
        {
            writer.Write(seat.Town);
            writer.Write(seat.Collector ?? "");
            writer.Write(seat.Captain ?? "");
        }
    }

    internal static int Load(IGenericReader reader)
    {
        var now = Core.TickCount;
        var ago = reader.ReadLong();
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var town = reader.ReadString();
            var collector = reader.ReadString();
            var captain = reader.ReadString();

            if (string.IsNullOrEmpty(town))
            {
                continue;
            }

            _seats[town] = new Seat
            {
                Town = town,
                Collector = string.IsNullOrEmpty(collector) ? null : collector,
                Captain = string.IsNullOrEmpty(captain) ? null : captain
            };
        }

        // The term goes on where it was; the tax is counted afresh from this boot (BotCity.Taxed starts again at nought).
        _termAt = now - Math.Clamp(ago, 0L, (long)int.MaxValue);
        _taxMark = BotCity.Taxed;
        _opened = true;
        Recount();

        return count;
    }

    public static void Forget()
    {
        Terms = 0;
        Posts = 0;
        Changed = 0;
        Commissions = 0;
        CommissionGold = 0;
        CommissionsUnpaid = 0;
        Watches = 0;
        WageGold = 0;
        PurseShort = 0;
        BudgetSpent = 0;
        OutcastBuys = 0;
        OutcastGold = 0;
        _outcast.Clear();
        _saidOutcast.Clear();
    }

    /// <summary>
    /// Every town's council forgotten, for Patrick's reset (07.10.2026, reset26): the collectors and the captains, exactly what
    /// <see cref="Save"/> writes, and the term's clock, opened again at once as at a first start.
    ///
    /// <para>
    /// <b>The watches' wages held go back first.</b> A watch taken holds its <see cref="Wage"/> out of the city's purse
    /// (<see cref="Hold"/>) until it ends; the reset deletes the watchmen, and a deleted bot's deed is forgotten, never dropped, so
    /// nothing would ever hand that coin back — the purse short by a wage a watch and its held count up by as much, for nothing.
    /// Each seat's watches on duty are handed back here (<c>BotCity.Refund</c>) before the seat is forgotten. A commission is
    /// held and paid in one step, and a steward's hire went back into the purse when it was decreed, so nothing else is held.
    /// See <see cref="BotPolitics.Wipe"/>. Returns the gold handed back.
    /// </para>
    /// </summary>
    public static int Wipe()
    {
        var back = 0;

        var held = HeldWages();

        if (held > 0)
        {
            BotCity.Refund(held);
            back += held;
        }

        // A save read back but not yet settled (a reset inside the module's first minute) is handed back too.
        back += Settle();

        _seats.Clear();
        _outcast.Clear();
        _saidOutcast.Clear();
        _ranked.Clear();
        _quote = default;

        // Opened now rather than left for the next look (reset26 review): the save is taken before the politics clock looks again,
        // and it writes the term's age from _termAt, so a closed clock would have carried the old world's term into the new one.
        Open(Core.TickCount);
        Recount();
        Forget();

        return back;
    }
}
