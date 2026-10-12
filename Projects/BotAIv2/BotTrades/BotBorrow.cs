using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// A poor bot's walk to a moneylender — a rich keeper's shop by the bank — and the loan made there, hand to hand (07.10.2026,
/// trades17). Not about money in the auction's sense (<see cref="BotDeed.Unpaid"/>): the coin borrowed is booked outside the
/// errand, so nothing learns that borrowing pays; what the loan buys is the kit, and the shopper's next look sends the bot for it.
/// </summary>
public sealed class BotBorrow : BotDeed
{
    public const string Trade = "borrow";

    private readonly BotStorefront _front;

    private readonly int _sum;

    private readonly double _claim;

    private readonly string _trade;

    private readonly double _pays;

    private long _began;

    public BotBorrow(BotStorefront front, int sum, double claim, string trade, double pays)
    {
        _front = front;
        _sum = sum;
        _claim = claim;
        _trade = trade;
        _pays = pays;
    }

    public override string Kind => Trade;

    public override Map Map => _front?.Map;

    public override Point3D Where => _front?.Location ?? Point3D.Zero;

    public override double Expects => _claim;

    public override double Minutes => 1.0;

    public override bool Unpaid => true;

    public override bool Hurries => true;

    public override string Stage => $"going to {_front?.Keeper?.Name} by the bank in {_front?.Town} to borrow {_sum}gp for its kit";

    public override bool Repeats(BotDeed other) => other is BotBorrow;

    public override void Taken(IBotWilful bot)
    {
        _began = Core.TickCount;

        if (bot?.Self is { } body)
        {
            BotBorrower.Went(body);
        }
    }

    public override void Resumed(IBotWilful bot) => _began = Core.TickCount;

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || !body.Alive || _front == null)
        {
            return BotDoing.Failed("no body, or no lender");
        }

        var lender = _front.Keeper;

        if (_front.Shut || lender is not { Deleted: false, Alive: true } || lender.Map != body.Map)
        {
            BotBorrower.NoteShut();

            return BotDoing.Failed($"{lender?.Name ?? "the lender"}'s shop shut before it got there");
        }

        if (!body.InRange(lender, BotHandover.MeetTiles))
        {
            if (Core.TickCount - _began >= BotBorrower.WalkMs)
            {
                BotBorrower.NoteNeverGot();

                return BotDoing.Failed($"could not get to {lender.Name} in {BotBorrower.WalkMs / 60000} minutes");
            }

            return BotDoing.Walk(lender.Map, lender, BotArrival.Within(BotHandover.MeetTiles), $"to {lender.Name} to borrow {_sum}gp");
        }

        if (!BotLoans.Lend(lender, body, _sum, _trade, _pays, out var why))
        {
            BotBorrower.NoteRefused();

            return BotDoing.Failed($"{lender.Name} would not lend: {why}");
        }

        return BotDoing.Done($"borrowed {_sum}gp from {lender.Name} for its kit, to repay out of its next takings");
    }
}

/// <summary>
/// Offers a poor bot short of its kit, with a trade its own record says pays, the walk to a moneylender (07.10.2026, trades17).
///
/// <para>
/// <b>Every reason a loan is refused is asked here first, and counted apart</b>: a borrower in debt or once defaulted, a bot that is
/// not poor or not short of its kit or could buy it from what it has, a bot whose own work has never paid it
/// <see cref="LeastPays"/> a minute (no loan without a trade), and no lender in reach. The sum is what the kit costs over what the
/// bot can spend (<c>BotProvision.Bill</c>), between <see cref="BotLoans.Least"/> and <see cref="BotLoans.Sum"/>.
/// </para>
/// </summary>
public sealed class BotBorrower : IBotProposer
{
    public string Name => "Borrower";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>What the walk to a lender is worth a minute: enough to beat idling and a poor bot's hunt for a few coins, not real work.</summary>
    public static double Prior { get; set; } = 40.0;

    /// <summary>The least the bot's best work must have paid a minute, measured at least <see cref="LeastSettled"/> times.</summary>
    public static double LeastPays { get; set; } = 10.0;

    public static int LeastSettled { get; set; } = 2;

    /// <summary>How far a borrower may be from a lender's shop. Two hundred tiles: a walk into Britain.</summary>
    public static int Reach { get; set; } = 200;

    /// <summary>How long between one bot's walks to a lender, whatever came of the last. Half an hour.</summary>
    public static int EveryMs { get; set; } = 1800000;

    public static int WalkMs { get; set; } = 300000;

    public static long Asked { get; private set; }

    public static long TooSoon { get; private set; }

    public static long Owing { get; private set; }

    public static long Marked { get; private set; }

    public static long NotPoor { get; private set; }

    public static long Kitted { get; private set; }

    public static long NoTrade { get; private set; }

    public static long NoLender { get; private set; }

    public static long Full { get; private set; }

    public static long Offered { get; private set; }

    public static long Walked { get; private set; }

    public static long Shut { get; private set; }

    public static long NeverGot { get; private set; }

    public static long Refused { get; private set; }

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

    internal static void NoteRefused() => Refused++;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotTrades.LendersOn || bot?.Self is not BotMobile who || !who.Alive || who.Tired || who.Map == null || who.Map == Map.Internal)
        {
            return null;
        }

        if (who.Squad != null || BotDelveParty.Delving(who) || BotOutlaw.Jailed(who) || who.Combatant != null || who.Resolve?.Deed is BotBorrow)
        {
            return null;
        }

        Asked++;

        // Most answers: not poor. Asked first, because it is the cheapest and the commonest.
        var spendable = BotYield.Spendable(who);

        if (spendable >= BotLoans.PoorBar)
        {
            NotPoor++;

            return null;
        }

        var now = Core.TickCount;

        if (_last.TryGetValue(who.Serial, out var when) && now - when < EveryMs)
        {
            TooSoon++;

            return null;
        }

        if (BotLoans.InDebt(who))
        {
            Owing++;

            return null;
        }

        if (BotLoans.IsMarked(who))
        {
            Marked++;

            return null;
        }

        if (BotLoans.Standing >= BotLoans.MostStanding)
        {
            Full++;

            return null;
        }

        // Short of the kit its trade takes into a fight, and unable to buy it with what it has.
        if (!BotProvision.Short(who))
        {
            Kitted++;

            return null;
        }

        var need = BotProvision.Bill(who) - spendable;

        if (need <= 0)
        {
            Kitted++;

            return null;
        }

        // No loan without a trade: the bot's own record must show work that paid.
        string trade = null;
        var pays = who.Resolve?.Ledger is { } ledger ? ledger.Best(LeastSettled, out trade) : 0.0;

        if (pays < LeastPays)
        {
            NoTrade++;

            return null;
        }

        var sum = Math.Clamp(need, BotLoans.Least, BotLoans.Sum);
        var front = Lender(who, sum);

        if (front == null)
        {
            NoLender++;

            return null;
        }

        Offered++;

        return new BotBorrow(front, sum, Prior, trade, pays);
    }

    /// <summary>The nearest open shop by a bank whose keeper could lend this sum, within reach, welcome and on joined ground.</summary>
    private static BotStorefront Lender(BotMobile who, int sum)
    {
        var open = BotShopkeep.Open;
        BotStorefront best = null;
        var nearest = int.MaxValue;

        for (var i = 0; i < open.Count; i++)
        {
            var front = open[i];

            if (front is not { Shut: false, Keeper: { } keeper } || ReferenceEquals(keeper, who) || front.Map != who.Map)
            {
                continue;
            }

            var away = Math.Max(Math.Abs(front.Location.X - who.X), Math.Abs(front.Location.Y - who.Y));

            if (away > Reach || away >= nearest || !BotLoans.CanLend(keeper, sum) || !BotCamp.Welcome(who, keeper)
                || !BotGates.Joined(who.Map, who.Location, front.Location))
            {
                continue;
            }

            best = front;
            nearest = away;
        }

        return best;
    }

    private static readonly HashSet<BotStorefront> _cried = [];

    /// <summary>A rich keeper says once a shift, in the trade room, that it lends. From the module's clock.</summary>
    internal static void Cry()
    {
        if (!BotTrades.LendersOn)
        {
            return;
        }

        _cried.RemoveWhere(f => f.Shut);

        var open = BotShopkeep.Open;

        for (var i = 0; i < open.Count; i++)
        {
            var front = open[i];

            if (front is not { Shut: false, Keeper: { } keeper } || _cried.Contains(front) || !BotLoans.CanLend(keeper, BotLoans.Sum))
            {
                continue;
            }

            _cried.Add(front);

            var fill = new Dictionary<string, string> { ["amount"] = BotLoans.Sum.ToString(), ["other"] = "friend" };
            var line = BotVoice.Phrase("loan:cry", null, keeper, fill);

            if (line != null)
            {
                BotVoice.Say(keeper, "trade", line, topic: "loan");
            }
        }
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been looked at for a loan"
            : $"{Asked} looks for a loan: {Offered} sent to a lender, {Walked} went; {NotPoor} not poor (over {BotLoans.PoorBar}gp), {TooSoon} within {EveryMs / 60000} minutes of their last, "
              + $"{Owing} already owing, {Marked} marked defaulters, {Full} found {BotLoans.MostStanding} loans standing, {Kitted} carried their kit or could buy it, "
              + $"{NoTrade} had no trade paying {LeastPays:F0}/min, {NoLender} found no keeper able to lend within {Reach} tiles; {Shut} found the shop shut, {NeverGot} could not get there, {Refused} were refused at the counter";

    public static void Forget()
    {
        _last.Clear();
        _cried.Clear();
        Asked = 0;
        TooSoon = 0;
        Owing = 0;
        Marked = 0;
        NotPoor = 0;
        Kitted = 0;
        NoTrade = 0;
        NoLender = 0;
        Full = 0;
        Offered = 0;
        Walked = 0;
        Shut = 0;
        NeverGot = 0;
        Refused = 0;
    }
}
