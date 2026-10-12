using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Caravans (07.10.2026, Patrick's third idea: "a town needs goods carried to another town, the caravan hires guards from the bots,
/// it is attacked on the way"). Every <see cref="EveryMs"/> the shops are read for a kind the bots have lately found a shelf empty
/// of (<c>BotShops.Dry</c>) that one town's shopkeeper is short of — under <see cref="LowShare"/> of its shelf — and another town's
/// sells; that town posts the caravan and holds its pay out of the city's purse at once.
///
/// <para>
/// <b>A trader leads; fighters guard it for a wage.</b> A trader near the source is offered the lead (<see cref="BotCaravan"/>): it
/// buys the goods at the source's counter with its own coin, waits up to <see cref="GatherMs"/> for guards, and walks the road. The
/// fighters near it are offered the guard (<see cref="BotGuard"/>), at most <see cref="MostGuards"/>: each follows the trader and
/// sets on whatever attacks it, if the guards between them bring half again what the thing is reckoned at (company16's ×1.5). A
/// trader is of the neutral class and raises no company (<c>BotSquads.Form</c> refuses it), so the guards walk with it rather than
/// under it. The creatures of the road attack a caravan as they would anyone.
/// </para>
///
/// <para>
/// <b>Paid on arrival, from the hold.</b> At the destination the town takes the goods onto its shopkeeper's shelf, pays the trader
/// what they cost and its premium (<see cref="Markup"/> over cost, at least <see cref="LeastPremium"/>), and pays each guard still
/// within <see cref="PayReach"/> its <see cref="Wage"/>; at night both are <c>BotNight.Pay</c> times as much. The rest of the hold
/// goes back. The town's price for that kind falls a notch (<see cref="Notch"/> for <see cref="NotchMs"/>) at its counters: what the
/// caravan carried is on the shelf. A trader who dies, drops the work or runs past <see cref="CapMs"/> loses the caravan; the hold
/// goes back and the goods stay where its own coin put them.
/// </para>
/// </summary>
public static class BotCaravans
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCaravans));

    public static bool Running { get; set; } = true;

    /// <summary>How often a town may post a caravan, and how long after a boot the first may be.</summary>
    public static int EveryMs { get; set; } = 2700000;

    public static int FirstMs { get; set; } = 1200000;

    /// <summary>How many of the kind a caravan carries at most, and the most stones of them.</summary>
    public static int Units { get; set; } = 10;

    public static int MostStones { get; set; } = 60;

    /// <summary>A shelf under this share of its own most is short.</summary>
    public static double LowShare { get; set; } = 0.25;

    /// <summary>
    /// A town whose steward ordered a caravan (07.10.2026, steward20): while set, only a shortage in that town is posted. Set with
    /// <see cref="CallNow"/> by <c>BotStewardship</c> and cleared by it the minute after.
    /// </summary>
    public static string For { get; set; }

    /// <summary>The least straight distance between the two towns.</summary>
    public static int LeastRoad { get; set; } = 120;

    /// <summary>The trader's premium over cost, and the least premium.</summary>
    public static double Markup { get; set; } = 0.25;

    public static int LeastPremium { get; set; } = 100;

    /// <summary>
    /// A guard's least wage, and how many guards at least and at most. life18a, 07.10.2026: the wage is the larger of this and
    /// <see cref="GuardRate"/> a minute of the trip as caravans have measured it (<see cref="Trip"/>): 100gp for a trip of four to
    /// seven minutes and a wait was 30gp a minute against a hunt's 60–100, and 2 of 43 posts offered were taken.
    /// </summary>
    public static int Wage { get; set; } = 100;

    /// <summary>What a guard is paid a minute of the trip: a hunt's rate (the militia's prior), so the post can win a fighter's hour.</summary>
    public static double GuardRate { get; set; } = 60.0;

    /// <summary>The trip's minutes before any caravan has arrived; afterwards the mean of the last arrivals (led to arrived).</summary>
    public static double TripPrior { get; set; } = 8.0;

    public static int LeastGuards { get; set; } = 2;

    public static int MostGuards { get; set; } = 3;

    /// <summary>How long a trader waits at the source for its guards; how long a posted caravan waits for a trader.</summary>
    public static int GatherMs { get; set; } = 150000;

    public static int PostedMs { get; set; } = 1200000;

    /// <summary>The longest a caravan may take from being led to arriving.</summary>
    public static int CapMs { get; set; } = 2700000;

    /// <summary>How far from the source a trader is offered the lead; how near the trader a fighter is offered the guard.</summary>
    public static int Reach { get; set; } = 300;

    public static int GuardReach { get; set; } = 60;

    /// <summary>How near the destination's square the caravan has arrived, and how near the trader a guard must be to be paid.</summary>
    public static int ArriveReach { get; set; } = 8;

    public static int PayReach { get; set; } = 24;

    /// <summary>A guard further than this from the trader on the road for <see cref="BehindMs"/> has fallen behind.</summary>
    public static int Behind { get; set; } = 60;

    public static int BehindMs { get; set; } = 180000;

    /// <summary>
    /// <b>How far a guard may drop back on the road before its trader stands for it (12.10.2026, w7).</b> Eight tiles, the delve's
    /// march spread (<c>BotDelve.MarchSpread</c>). On 11–12.10 the traders rode ahead of their guards — Daeron nine, seventeen,
    /// nineteen, twenty-one tiles behind Naevys Silverleaf in eleven seconds on the road north through Britain — and every guard but a
    /// handful ended "could not get nearer" behind it (16 of 167 finished in L1, 13 of 113 in the two runs since). A caravan goes
    /// at its guards' pace: the trader's walk is held where it stands (<see cref="BotJourney.Hold"/>, the plan kept) while a
    /// guard is further back than this and not past <see cref="Behind"/>, for <see cref="GuardWaitMs"/> a run at most. Nought
    /// turns it off.
    /// </summary>
    public static int GuardSpread { get; set; } = 8;

    /// <summary>The most a trader stands for its guards over one run: a minute and a half, the delve leader's own hold (<c>BotDelve.HoldMs</c>).</summary>
    public static int GuardWaitMs { get; set; } = 90000;

    /// <summary>How long one beat of the trader's holds its walk; renewed by the next, lapsing by itself.</summary>
    public static int GuardWaitBeatMs { get; set; } = 1500;

    /// <summary>
    /// life18a: how long the town keeps a guard's wage for a guard still on its way when the trader arrives (a gate puts the trader
    /// a thousand tiles on in a step; its guards come through after it). Paid when the guard reaches the square; handed back after.
    /// </summary>
    public static int LateMs { get; set; } = 300000;

    /// <summary>How near the trader an attacker must be for a guard to set on it; the odds the guards between them must bring.</summary>
    public static int DefendReach { get; set; } = 12;

    public static double GuardOdds { get; set; } = 1.5;

    /// <summary>The town's price for the kind after a caravan, as a share; and for how long.</summary>
    public static double Notch { get; set; } = 0.95;

    public static int NotchMs { get; set; } = 7200000;

    /// <summary>What leading a caravan and guarding one are reckoned at before the ledger has seen them; the minutes either is expected to take.</summary>
    public static double Prior { get; set; } = 40.0;

    public static double GuardPrior { get; set; } = 30.0;

    public static double WorkMinutes { get; set; } = 15.0;

    // ---- counters ----------------------------------------------------------------------------------------------------------

    public static long Posted { get; private set; }

    public static long NothingShort { get; private set; }

    public static long NoPurse { get; private set; }

    public static long Untaken { get; private set; }

    public static long Led { get; private set; }

    public static long Unbought { get; private set; }

    public static long SetOut { get; private set; }

    public static long Unguarded { get; private set; }

    public static long Arrived { get; private set; }

    public static long Lost { get; private set; }

    public static long GuardsHired { get; private set; }

    public static long GuardsPaid { get; private set; }

    public static long GuardsBehind { get; private set; }

    public static long Defended { get; private set; }

    public static long Wages { get; private set; }

    public static long Premiums { get; private set; }

    public static long GoodsCost { get; private set; }

    public static long Carried { get; private set; }

    public static long NightRuns { get; private set; }

    public static long NotchPriced { get; private set; }

    public static long LeadOffered { get; private set; }

    public static long GuardOffered { get; private set; }

    /// <summary>life18a: guards that put the post down before the road's end; wages kept for guards on their way, paid late, lapsed.</summary>
    public static long GuardsDropped { get; private set; }

    /// <summary>Runs whose trader stood on the road for a guard more than <see cref="GuardSpread"/> tiles back; of them, those that stood <see cref="GuardWaitMs"/> and went on; and the seconds stood in all.</summary>
    public static long GuardWaits { get; private set; }

    public static long GuardWaitSpent { get; private set; }

    public static long GuardWaitedMs { get; private set; }

    public static long GuardsOwed { get; private set; }

    public static long GuardsPaidLate { get; private set; }

    public static long LateLapsed { get; private set; }

    private static double _tripSum;

    private static int _trips;

    /// <summary>The trip's minutes, led to arrived: the mean of the arrivals measured since the boot, the prior until one.</summary>
    public static double Trip => Math.Clamp(_trips > 0 ? _tripSum / _trips : TripPrior, 3.0, 30.0);

    /// <summary>A guard's wage for a caravan posted now: <see cref="GuardRate"/> a minute of <see cref="Trip"/>, at least <see cref="Wage"/>.</summary>
    public static int WageNow => Math.Max(Wage, (int)Math.Ceiling(GuardRate * Trip));

    /// <summary>A wage kept for a guard still on its way when its trader arrived.</summary>
    private sealed class Owed
    {
        public Run Run;

        public Mobile Guard;

        public int Due;

        public long Until;
    }

    private static readonly List<Owed> _owed = [];

    public enum Phase
    {
        Posted,
        Buying,
        Gathering,
        Road,
        Over
    }

    /// <summary>One caravan: what, from where to where, the hold, the trader and its guards.</summary>
    public sealed class Run
    {
        public Type Kind;

        public int Units;

        public int Price;

        public BotTowns.Town From;

        public BaseVendor Source;

        public BotTowns.Town To;

        public BaseVendor Buyer;

        public int Hold;

        public int Paid;

        /// <summary>The guard's wage this caravan was posted at (life18a).</summary>
        public int GuardWage;

        /// <summary>Of the hold, kept for guards still on their way at the arrival (life18a).</summary>
        public int Kept;

        public int Bought;

        public Phase Phase;

        public Mobile Leader;

        public readonly List<Mobile> Guards = [];

        public long PostedTick;

        public long LedTick;

        public long GatherTick;

        public long RoadTick;

        public string Ending;

        /// <summary>The trader's stand for its guards on the road (12.10.2026, w7): stood in all, the beat it last stood, and whether it is standing, has stood, or has stood its fill.</summary>
        public long WaitedMs;

        public long WaitTick;

        public bool Waiting;

        public bool Waited;

        public bool WaitSpent;

        public bool Over => Phase == Phase.Over;

        public int Premium => Math.Max(LeastPremium, (int)Math.Ceiling(Units * Price * Markup));

        public string Tell => $"{Units} {Kind?.Name} from {From?.Name} to {To?.Name}";
    }

    private static Run _run;

    private static long _nextTick;

    private static bool _seeded;

    private static readonly Dictionary<(string Town, Type Kind), long> _notches = [];

    /// <summary>The caravan standing now, or null.</summary>
    public static Run Now => _run is { Over: false } r ? r : null;

    /// <summary>What the standing caravan still holds of the city's purse, and the wages kept for late guards, for the store.</summary>
    public static int Holding => (_run is { Over: false } r ? Math.Max(0, r.Hold - r.Paid) : 0) + OwedNow;

    private static int OwedNow
    {
        get
        {
            var sum = 0;

            for (var i = 0; i < _owed.Count; i++)
            {
                sum += _owed[i].Due;
            }

            return sum;
        }
    }

    public static void Beat(long now)
    {
        if (!_seeded)
        {
            _seeded = true;
            _nextTick = now + Math.Max(0, FirstMs);
        }

        Lapse(now);

        if (_run is { Over: false } run)
        {
            Watch(run, now);

            return;
        }

        _run = null;

        if (!Running || now - _nextTick < 0)
        {
            return;
        }

        _nextTick = now + Math.Max(600000, EveryMs);
        Post(now);
    }

    /// <summary>Set by hand to post a caravan at the next beat.</summary>
    public static bool CallNow
    {
        get => false;
        set
        {
            if (value)
            {
                _seeded = true;
                _nextTick = Core.TickCount;
            }
        }
    }

    private static void Post(long now)
    {
        var map = BotPopulation.Home;

        if (map == null || !Need(map, out var run))
        {
            NothingShort++;

            return;
        }

        var nightly = Math.Max(1.0, BotNight.NightPay);
        run.GuardWage = WageNow;
        var hold = run.Units * run.Price + (int)Math.Ceiling(run.Premium * nightly) + (int)Math.Ceiling(MostGuards * run.GuardWage * nightly);
        var held = BotSeasons.Hold(hold, hold);

        if (held <= 0)
        {
            NoPurse++;

            logger.Information("{To} wanted {Tell} carried, but the city's purse ({Purse}gp, keeping {Reserve}) cannot hold the {Hold}gp it would pay", run.To.Name, run.Tell, BotCity.Purse, BotSeasons.CityReserve, hold);

            return;
        }

        run.Hold = held;
        run.PostedTick = now;
        run.Phase = Phase.Posted;
        _run = run;
        Posted++;

        logger.Information(
            "{To} posts a caravan: {Tell} ({Price}gp each at {Source}'s counter), a trader's premium of {Premium}gp, {Guards} guards at {Wage}gp each; {Hold}gp held out of the city's purse",
            run.To.Name,
            run.Tell,
            run.Price,
            run.Source.Name,
            run.Premium,
            MostGuards,
            run.GuardWage,
            held
        );

        BotSeasons.Announce(
            "caravan",
            map,
            run.From.Square,
            $"{run.To.Name} is short of {run.Kind.Name}: a caravan of {run.Units} from {run.From.Name} wants a trader to lead it and {MostGuards} guards at {run.GuardWage}gp."
        );
    }

    /// <summary>A kind the bots found a shelf empty of, short at one town's shopkeeper and sold at another town's.</summary>
    private static bool Need(Map map, out Run run)
    {
        run = null;

        var dry = BotShops.Dry();
        var shops = BotShops.Shops;

        for (var d = 0; d < dry.Count && d < 12; d++)
        {
            var kind = dry[d].Kind;

            if (!BotTrading.Goods(kind))
            {
                continue;
            }

            BaseVendor shortAt = null;
            BotTowns.Town shortTown = null;

            for (var i = 0; i < shops.Count && shortAt == null; i++)
            {
                var v = shops[i];

                if (v is not { Deleted: false } || v.Map != map || BotTowns.Of(v.Location) is not { FromHome: true } town)
                {
                    continue;
                }

                // Only the town a steward ordered it for, while one did (steward20). See For.
                if (For != null && !string.Equals(town.Name, For, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Entry(v, kind) is { } e && e.MaxAmount > 0 && e.Amount < e.MaxAmount * LowShare)
                {
                    shortAt = v;
                    shortTown = town;
                }
            }

            if (shortAt == null)
            {
                continue;
            }

            for (var i = 0; i < shops.Count; i++)
            {
                var v = shops[i];

                if (v is not { Deleted: false } || v.Map != map || BotTowns.Of(v.Location) is not { FromHome: true } town || town == shortTown)
                {
                    continue;
                }

                var far = Math.Max(Math.Abs(town.Square.X - shortTown.Square.X), Math.Abs(town.Square.Y - shortTown.Square.Y));

                if (far < LeastRoad || !BotGates.Joined(map, town.Square, shortTown.Square))
                {
                    continue;
                }

                if (Entry(v, kind) is not { Price: > 0 } e)
                {
                    continue;
                }

                var weight = Math.Max(0.1, BotTrading.Weight(kind));
                var units = Math.Min(Math.Min(Units, e.Amount), (int)(MostStones / weight));

                if (units <= 0)
                {
                    continue;
                }

                run = new Run { Kind = kind, Units = units, Price = e.Price, From = town, Source = v, To = shortTown, Buyer = shortAt };

                return true;
            }
        }

        return false;
    }

    private static GenericBuyInfo Entry(BaseVendor vendor, Type kind)
    {
        if (!vendor.IsActiveSeller)
        {
            return null;
        }

        var offered = vendor.GetBuyInfo();

        for (var i = 0; i < offered.Length; i++)
        {
            if (offered[i] is GenericBuyInfo info && info.Type == kind)
            {
                return info;
            }
        }

        return null;
    }

    private static void Watch(Run run, long now)
    {
        if (run.Phase == Phase.Posted)
        {
            if (now - run.PostedTick >= PostedMs)
            {
                Untaken++;
                End(run, $"no trader took it in {PostedMs / 60000} minutes");
            }

            return;
        }

        if (run.Leader is not { Deleted: false, Alive: true })
        {
            Lost++;
            End(run, $"{run.Leader?.Name ?? "its trader"} fell on the road");

            return;
        }

        if (now - run.LedTick >= CapMs)
        {
            Lost++;
            End(run, $"it had not arrived in {CapMs / 60000} minutes");
        }
    }

    // ---- the trader ----------------------------------------------------------------------------------------------------------

    internal static void OfferedLead() => LeadOffered++;

    internal static void OfferedGuard() => GuardOffered++;

    internal static void Lead(Run run, Mobile trader)
    {
        run.Leader = trader;
        run.LedTick = Core.TickCount;
        run.Phase = Phase.Buying;
        Led++;

        logger.Information("{Name} leads {To}'s caravan: {Tell}, {Premium}gp premium on arrival", trader?.Name, run.To.Name, run.Tell, run.Premium);

        BotSeasons.Say(trader, "world", $"I lead a caravan of {run.Kind.Name} from {run.From.Name} to {run.To.Name}. Guards wanted at {run.GuardWage}gp — meet me at {run.Source.Name}'s counter!");
    }

    internal static void Bought(Run run, int units)
    {
        run.Bought = units;
        run.Phase = Phase.Gathering;
        run.GatherTick = Core.TickCount;
        GoodsCost += units * run.Price;
    }

    internal static void Unbuyable(Run run, string why)
    {
        Unbought++;
        End(run, $"the goods could not be bought: {why}");
    }

    internal static void Setting(Run run)
    {
        run.Phase = Phase.Road;
        run.RoadTick = Core.TickCount;
        SetOut++;

        if (run.Guards.Count < LeastGuards)
        {
            Unguarded++;
        }

        logger.Information(
            "{Name}'s caravan sets out from {From} for {To} with {Guards} guards ({Names}): {Units} {Kind}",
            run.Leader?.Name,
            run.From.Name,
            run.To.Name,
            run.Guards.Count,
            Names(run.Guards),
            run.Bought,
            run.Kind.Name
        );

        BotSeasons.Say(run.Leader, "local", run.Guards.Count > 0 ? $"Move out! {run.Guards.Count} guards, eyes on the road to {run.To.Name}." : $"No guards came. I set out for {run.To.Name} alone.");
    }

    /// <summary>The caravan at the destination: the goods onto the shelf, the trader and the guards paid out of the hold, the rest handed back.</summary>
    internal static int Deliver(Run run, Mobile trader)
    {
        var pack = trader.Backpack;
        var have = pack == null ? 0 : Math.Min(run.Bought, pack.GetAmount(run.Kind));

        if (have > 0)
        {
            pack.ConsumeTotal(run.Kind, have);

            if (run.Buyer is { Deleted: false } buyer && Entry(buyer, run.Kind) is { } shelf)
            {
                shelf.Amount += have;
            }
        }

        var night = BotNight.Pay;

        if (night > 1.0)
        {
            NightRuns++;
        }

        var share = run.Bought > 0 ? have / (double)run.Bought : 0.0;
        var owed = have * run.Price + (int)Math.Ceiling(run.Premium * share * night);
        var left = run.Hold - run.Paid;
        var paid = Math.Min(owed, left);

        if (paid > 0 && BotSeasons.Disburse(trader, paid))
        {
            run.Paid += paid;
            Premiums += Math.Max(0, paid - have * run.Price);
        }

        var wage = (int)Math.Ceiling(Math.Max(Wage, run.GuardWage) * night);
        var guards = 0;
        var late = 0;
        var trip = (Core.TickCount - run.LedTick) / 60000.0;

        if (trip > 0.0)
        {
            _tripSum += trip;
            _trips++;
        }

        for (var i = 0; i < run.Guards.Count; i++)
        {
            var guard = run.Guards[i];

            if (guard is not { Deleted: false, Alive: true })
            {
                continue;
            }

            var due = Math.Min(wage, run.Hold - run.Paid - run.Kept);

            if (due <= 0)
            {
                continue;
            }

            // life18a: a guard by the trader or already on the square is paid now; one still on the road (behind a gate, a fight)
            // has its wage kept for LateMs and is paid when it reaches the square. Until now a guard not within 24 tiles of the
            // trader the moment it arrived was paid nothing, and its wage went back.
            if (guard.Map == trader.Map && (guard.InRange(trader.Location, PayReach) || guard.InRange(run.To.Square, PayReach)))
            {
                if (BotSeasons.Disburse(guard, due))
                {
                    run.Paid += due;
                    Wages += due;
                    GuardsPaid++;
                    guards++;
                }

                continue;
            }

            run.Kept += due;
            _owed.Add(new Owed { Run = run, Guard = guard, Due = due, Until = Core.TickCount + Math.Max(0, LateMs) });
            GuardsOwed++;
            late++;
        }

        Carried += have;
        Arrived++;

        if (have > 0)
        {
            _notches[(run.To.Name, run.Kind)] = Core.TickCount + Math.Max(0, NotchMs);
        }

        logger.Information(
            "{Name}'s caravan reached {To} after {Minutes:F0} minutes: {Have} of {Units} {Kind} onto {Buyer}'s shelf, {Paid}gp paid out of the {Hold}gp hold ({Guards} guards at {Wage}gp{Night}, {Late} more on their way, {Kept}gp kept for them); {Kind}'s price there ×{Notch:F2} for {Hours:F0} hours",
            trader.Name,
            run.To.Name,
            trip,
            have,
            run.Bought,
            run.Kind.Name,
            run.Buyer?.Name,
            run.Paid,
            run.Hold,
            guards,
            wage,
            night > 1.0 ? ", a night's pay" : "",
            late,
            run.Kept,
            run.Kind.Name,
            Notch,
            NotchMs / 3600000.0
        );

        BotSeasons.Announce("caravan", trader.Map, run.To.Square, $"A caravan from {run.From.Name} reached {run.To.Name}: {have} {run.Kind.Name} on the shelves, {guards} guards paid.");

        End(run, null);

        return paid;
    }

    internal static void Dropped(Run run, Mobile trader, string why)
    {
        if (run.Over)
        {
            return;
        }

        Lost++;
        End(run, $"{trader?.Name ?? "its trader"} let it go: {why}");
    }

    private static void End(Run run, string why)
    {
        run.Phase = Phase.Over;

        var back = Math.Max(0, run.Hold - run.Paid - run.Kept);

        BotSeasons.Return(back);

        if (why != null)
        {
            run.Ending = why;

            logger.Information("{To}'s caravan ({Tell}) is lost or withdrawn: {Why}; {Back}gp back to the purse", run.To?.Name, run.Tell, why, back);

            if (run.Leader != null)
            {
                BotSeasons.Announce("caravan", run.Leader.Map, run.Leader.Location, $"The caravan to {run.To?.Name} is lost: {why}.");
            }
        }
    }

    // ---- the guards -------------------------------------------------------------------------------------------------------------

    internal static void Hired(Run run, Mobile guard)
    {
        if (guard == null || run.Guards.Contains(guard))
        {
            return;
        }

        run.Guards.Add(guard);
        GuardsHired++;

        logger.Information("{Name} guards {Leader}'s caravan to {To} for {Wage}gp ({Count} of {Most})", guard.Name, run.Leader?.Name, run.To.Name, Wage, run.Guards.Count, MostGuards);

        BotSeasons.Say(guard, "local", $"I'll guard your caravan to {run.To.Name}, {run.Leader?.Name}.");
    }

    /// <summary>
    /// The trader on the road stands for a guard that has dropped back more than <see cref="GuardSpread"/> tiles and not past
    /// <see cref="Behind"/>, for <see cref="GuardWaitMs"/> a run at most. Returns whether it stood. See <see cref="GuardSpread"/>.
    /// </summary>
    internal static bool WaitForGuards(Run run, Mobile leader)
    {
        if (GuardSpread <= 0 || run == null || run.WaitSpent || leader is not BotMobile bot || bot.Journey is not { Active: true } journey)
        {
            return false;
        }

        var straggler = false;

        for (var i = 0; i < run.Guards.Count; i++)
        {
            if (run.Guards[i] is not { Deleted: false, Alive: true } guard || guard.Map != leader.Map)
            {
                continue;
            }

            var back = Math.Max(Math.Abs(guard.X - leader.X), Math.Abs(guard.Y - leader.Y));

            if (back > GuardSpread && back <= Behind)
            {
                straggler = true;

                break;
            }
        }

        if (!straggler)
        {
            run.Waiting = false;

            return false;
        }

        var now = Core.TickCount;

        if (run.Waiting)
        {
            var stood = Math.Min(Math.Max(0L, now - run.WaitTick), 5000L);

            run.WaitedMs += stood;
            GuardWaitedMs += stood;
        }
        else if (!run.Waited)
        {
            run.Waited = true;
            GuardWaits++;
        }

        run.Waiting = true;
        run.WaitTick = now;

        if (run.WaitedMs >= GuardWaitMs)
        {
            run.WaitSpent = true;
            run.Waiting = false;
            GuardWaitSpent++;

            return false;
        }

        journey.Hold(GuardWaitBeatMs);

        return true;
    }

    internal static void Left(Run run, Mobile guard, bool behind)
    {
        if (!run.Guards.Remove(guard))
        {
            return;
        }

        if (behind)
        {
            GuardsBehind++;
        }
        else
        {
            GuardsDropped++;
        }

        logger.Information(
            "{Name} leaves {Leader}'s caravan to {To} ({Phase}): {Why}; {Count} guards left",
            guard?.Name,
            run.Leader?.Name,
            run.To?.Name,
            run.Phase,
            behind ? $"more than {Behind} tiles behind for {BehindMs / 1000} seconds" : "it put the post down",
            run.Guards.Count
        );
    }

    /// <summary>Whether the town keeps a wage for this guard, which is on its way to the square after the trader arrived.</summary>
    internal static bool Owes(Run run, Mobile guard)
    {
        for (var i = 0; i < _owed.Count; i++)
        {
            if (ReferenceEquals(_owed[i].Run, run) && ReferenceEquals(_owed[i].Guard, guard))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A late guard on the square: its kept wage paid. Returns what was paid.</summary>
    internal static int PayLate(Run run, Mobile guard)
    {
        for (var i = 0; i < _owed.Count; i++)
        {
            var owed = _owed[i];

            if (!ReferenceEquals(owed.Run, run) || !ReferenceEquals(owed.Guard, guard))
            {
                continue;
            }

            _owed.RemoveAt(i);
            run.Kept -= owed.Due;

            if (!BotSeasons.Disburse(guard, owed.Due))
            {
                BotSeasons.Return(owed.Due);
                LateLapsed++;

                return 0;
            }

            run.Paid += owed.Due;
            Wages += owed.Due;
            GuardsPaid++;
            GuardsPaidLate++;

            logger.Information("{Name} reached {To} behind {Leader}'s caravan and is paid its {Due}gp", guard.Name, run.To?.Name, run.Leader?.Name, owed.Due);
            BotSeasons.Say(guard, "local", $"Made it to {run.To?.Name}. The road was long.");

            return owed.Due;
        }

        return 0;
    }

    /// <summary>A late guard that gave the walk up, or never came: its kept wage back to the purse.</summary>
    internal static void Forfeit(Run run, Mobile guard)
    {
        for (var i = _owed.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(_owed[i].Run, run) && ReferenceEquals(_owed[i].Guard, guard))
            {
                Release(i);
            }
        }
    }

    private static void Lapse(long now)
    {
        for (var i = _owed.Count - 1; i >= 0; i--)
        {
            var owed = _owed[i];

            if (now - owed.Until >= 0 || owed.Guard is not { Deleted: false, Alive: true })
            {
                Release(i);
            }
        }
    }

    private static void Release(int i)
    {
        var owed = _owed[i];

        _owed.RemoveAt(i);
        owed.Run.Kept -= owed.Due;
        BotSeasons.Return(owed.Due);
        LateLapsed++;

        logger.Information("{Name} never reached {To} after {Leader}'s caravan: its {Due}gp back to the purse", owed.Guard?.Name, owed.Run.To?.Name, owed.Run.Leader?.Name, owed.Due);
    }

    internal static void Defend() => Defended++;

    /// <summary>The guards' summed strength, for the odds a guard sets on an attacker at.</summary>
    internal static double Guarding(Run run)
    {
        var sum = 0.0;

        for (var i = 0; i < run.Guards.Count; i++)
        {
            if (run.Guards[i] is { Deleted: false, Alive: true } g)
            {
                sum += BotThreat.Power(g);
            }
        }

        return sum;
    }

    // ---- the town's price ------------------------------------------------------------------------------------------------------

    /// <summary>A town's price for a kind a caravan brought lately: <see cref="Notch"/> at its counters. One otherwise; cheap.</summary>
    public static double Notched(BaseVendor vendor, Type kind)
    {
        if (_notches.Count == 0 || vendor == null || kind == null || BotTowns.Of(vendor.Location) is not { } town
            || !_notches.TryGetValue((town.Name, kind), out var until))
        {
            return 1.0;
        }

        if (Core.TickCount - until >= 0)
        {
            _notches.Remove((town.Name, kind));

            return 1.0;
        }

        NotchPriced++;

        return Math.Clamp(Notch, 0.5, 1.0);
    }

    private static string Names(List<Mobile> who)
    {
        List<string> names = [];

        for (var i = 0; i < who.Count; i++)
        {
            names.Add(who[i]?.Name ?? "?");
        }

        return names.Count == 0 ? "none" : string.Join(", ", names);
    }

    /// <summary>Called off by a reload or a stop: the hold handed back.</summary>
    public static void Stand()
    {
        if (_run is { Over: false } run)
        {
            Lost++;
            End(run, "the shard stopped it");
        }

        for (var i = _owed.Count - 1; i >= 0; i--)
        {
            Release(i);
        }

        _run = null;
    }

    public static string Describe() =>
        !Running
            ? "off"
            : $"{Posted} posted ({NothingShort} looks found nothing short at one town and sold at another, {NoPurse} the purse could not hold the pay), {Untaken} untaken; {LeadOffered} leads offered, {Led} led, {Unbought} could not buy the goods; "
              + $"{SetOut} set out ({Unguarded} with fewer than {LeastGuards} guards), {Arrived} arrived, {Lost} lost; {GuardOffered} guard posts offered, {GuardsHired} hired at {WageNow}gp now ({GuardRate:F0}gp a minute of a {Trip:F1}-minute trip), {GuardsPaid} paid ({GuardsPaidLate} on reaching the square behind the trader, {GuardsOwed} owed on arrival, {LateLapsed} lapsed), {GuardsBehind} fell behind, {GuardsDropped} put the post down, {Defended} times a guard set on what attacked its trader; {GuardWaits} traders stood on the road for a guard more than {GuardSpread} tiles back ({GuardWaitedMs / 1000}s in all, {GuardWaitSpent} stood {GuardWaitMs / 1000}s and went on); "
              + $"{Carried} things carried, {GoodsCost}gp of goods bought by the traders, {Premiums}gp premiums and {Wages}gp wages paid ({NightRuns} at night's pay); {NotchPriced} counter prices at ×{Notch:F2} after a caravan, {_notches.Count} notches standing"
              + (_run is { Over: false } r ? $"; now: {r.Tell}, {r.Phase}, led by {r.Leader?.Name ?? "nobody yet"}, {r.Guards.Count} guards" : "");

    public static void Forget()
    {
        _run = null;
        _seeded = false;
        _notches.Clear();
        Posted = NothingShort = NoPurse = Untaken = Led = Unbought = SetOut = Unguarded = Arrived = Lost = 0;
        GuardsHired = GuardsPaid = GuardsBehind = Defended = Wages = Premiums = GoodsCost = Carried = NightRuns = NotchPriced = 0;
        LeadOffered = GuardOffered = 0;
        GuardsDropped = GuardsOwed = GuardsPaidLate = LateLapsed = 0;
        GuardWaits = GuardWaitSpent = GuardWaitedMs = 0;
        _tripSum = 0;
        _trips = 0;
        _owed.Clear();
    }
}
