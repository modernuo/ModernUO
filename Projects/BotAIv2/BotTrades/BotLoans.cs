using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The moneylenders' book: who owes whom, how much is left, the lien that takes a share of every gain the borrower makes until the
/// debt is paid, and the defaulters, who are never lent to again (07.10.2026, trades17).
///
/// <para>
/// <b>Patrick, 07.10.2026: "a moneylender (loans to the poor against future spoils: an honest way out of poverty, not a
/// handout)".</b> His standing rule is that every coin comes from work and that a loan repaid with interest is work. So a loan here
/// is a risk the lender really takes: <see cref="Sum"/> at most, from a keeper of a shop by the bank holding
/// <see cref="LenderWealth"/> or more, to a bot under <see cref="PoorBar"/> that is short of the kit its trade needs and has a trade
/// that has paid it (<see cref="BotLedger.Best"/>); never to a bot already in debt or once defaulted. The coin goes pack to pack at
/// the shop. Back comes <see cref="Interest"/> on top, a <see cref="LienShare"/> of every rise in the borrower's wealth taken on the
/// clock (<see cref="CollectMs"/>) and paid into the lender's account — and when <see cref="DefaultHours"/> of the borrower's own
/// play pass with the debt unpaid, or the borrower leaves the island, the lender has lost what is left and the borrower is marked.
/// </para>
///
/// <para>
/// <b>Money moved by a decision outside the work in hand is booked aside</b> (<see cref="BotYield.Aside"/>): the principal out of
/// the lender's shop shift and into the borrower's errand, the lien out of whatever the borrower is doing when it is taken and into
/// whatever the lender is doing. Lending is a shop's side business, counted in the <c>Bot shops:</c> line and never in the
/// keeper's ledger: the keeper does not choose to lend, it is asked at its counter.
/// </para>
///
/// <para>
/// <b>And a lender minds its risk.</b> A loan is made only while what it may expect is above nought (<see cref="Expected"/>: the
/// interest on what is repaid, less the principal the measured default rate loses). At a fifth on top, defaults above one in six
/// make every loan a loss, and the desks stop lending by themselves until the dial moves — the market's answer, said in the line.
/// </para>
/// </summary>
public static class BotLoans
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotLoans));

    public sealed class Loan
    {
        public string Debtor;

        public string Lender;

        public int Principal;

        public int Owed;

        public int Repaid;

        /// <summary>Minutes the debtor has been in the world since it borrowed: the default clock, which does not run while it sleeps.</summary>
        public double Played;

        /// <summary>The debtor's wealth after the last take: the lien takes a share of what rises above it.</summary>
        public int Mark;

        public DateTime Taken;

        public int Left => Math.Max(0, Owed - Repaid);
    }

    // ---- Dials ------------------------------------------------------------------------------------------------------------

    /// <summary>The most a loan may be. A hundred and fifty, Patrick's figure: a kit's worth of bandages, bottles and herbs.</summary>
    public static int Sum { get; set; } = 150;

    /// <summary>The least a loan is made for: below it the kit is nearly in reach and the bot hunts for the rest.</summary>
    public static int Least { get; set; } = 30;

    /// <summary>What is repaid on top of the principal. A fifth.</summary>
    public static double Interest { get; set; } = 0.2;

    /// <summary>The share of every rise in the borrower's wealth the lien takes until the debt is paid. A half.</summary>
    public static double LienShare { get; set; } = 0.5;

    /// <summary>Hours of the borrower's own play after which a debt still owed is a default. Four.</summary>
    public static double DefaultHours { get; set; } = 4.0;

    /// <summary>The wealth a shop's keeper must hold, spendable, to lend from its shop. Twelve hundred.</summary>
    public static int LenderWealth { get; set; } = 1200;

    /// <summary>What a lender keeps after a loan: it never lends itself poor. Eight hundred.</summary>
    public static int LenderKeeps { get; set; } = 800;

    /// <summary>A bot under this much, spendable, is poor enough to borrow. A hundred.</summary>
    public static int PoorBar { get; set; } = 100;

    /// <summary>The most loans one lender has standing at once. Five.</summary>
    public static int PerLender { get; set; } = 5;

    /// <summary>The most loans standing on the island at once. Forty.</summary>
    public static int MostStanding { get; set; } = 40;

    /// <summary>How often the lien looks at every debtor. A minute.</summary>
    public static int CollectMs { get; set; } = 60000;

    /// <summary>The default rate assumed until <see cref="LeastSettled"/> loans have been repaid or defaulted. A quarter.</summary>
    public static double DefaultGuess { get; set; } = 0.1;

    /// <summary>Whether a lender refuses while the measured default rate makes a loan's expected return nought or less.</summary>
    public static bool MindsRisk { get; set; } = true;

    /// <summary>Loans refused at the counter because the default rate made them a loss.</summary>
    public static long Risky { get; private set; }

    public static int LeastSettled { get; set; } = 5;

    // ---- Counters ---------------------------------------------------------------------------------------------------------

    public static long Made { get; private set; }

    public static long MadeGold { get; private set; }

    public static long Repaid { get; private set; }

    public static long Takes { get; private set; }

    public static long TakenGold { get; private set; }

    /// <summary>Of the gold taken by the lien, what was over the principal: the lenders' interest.</summary>
    public static long InterestGold { get; private set; }

    public static long Defaulted { get; private set; }

    /// <summary>Principal the lenders lost to defaults.</summary>
    public static long LostGold { get; private set; }

    public static long Gone { get; private set; }

    /// <summary>Takes refused because the borrower's money was held for its wants on the board.</summary>
    public static long Held { get; private set; }

    /// <summary>Lien takes with no lender to pay — it left the island — so nothing was taken and the debt was forgiven.</summary>
    public static long Forgiven { get; private set; }

    /// <summary>Settled loans read back from the save, for the default rate across restarts.</summary>
    private static long _settledBefore;

    private static long _defaultedBefore;

    private static readonly Dictionary<string, Loan> _loans = new(StringComparer.Ordinal);

    private static readonly HashSet<string> _marked = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, BotMobile> _byName = new(StringComparer.Ordinal);

    private static long _lastCollect;

    private static bool _collected;

    public static int Standing => _loans.Count;

    public static int Marked => _marked.Count;

    /// <summary>The share of settled loans that defaulted, with a guess until there are enough.</summary>
    public static double DefaultRate
    {
        get
        {
            var settled = Repaid + Defaulted + _settledBefore;

            return settled < LeastSettled ? DefaultGuess : (double)(Defaulted + _defaultedBefore) / settled;
        }
    }

    /// <summary>What a loan of this sum is expected to earn its lender: the interest, less the measured default rate of the whole.</summary>
    public static int Expected(int sum) =>
        (int)Math.Round(sum * Math.Max(0.0, Interest) * (1.0 - DefaultRate) - sum * DefaultRate);

    public static bool InDebt(Mobile bot) => bot?.Name != null && _loans.ContainsKey(bot.Name);

    public static bool IsMarked(Mobile bot) => bot?.Name != null && _marked.Contains(bot.Name);

    public static Loan Of(Mobile bot) => bot?.Name != null && _loans.TryGetValue(bot.Name, out var loan) ? loan : null;

    /// <summary>How many loans this lender has standing.</summary>
    public static int Lending(Mobile lender)
    {
        if (lender?.Name == null)
        {
            return 0;
        }

        var n = 0;

        foreach (var (_, loan) in _loans)
        {
            if (string.Equals(loan.Lender, lender.Name, StringComparison.Ordinal))
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>Whether this bot could lend <paramref name="sum"/> now: rich enough to keep its keep after it, and under its count.</summary>
    public static bool CanLend(Mobile lender, int sum) =>
        lender is BotMobile { Deleted: false, Alive: true } && BotYield.Spendable(lender) >= Math.Max(LenderWealth, sum + LenderKeeps)
        && Lending(lender) < PerLender && !IsMarked(lender) && !InDebt(lender);

    /// <summary>
    /// The loan itself, with the two standing together: the principal out of the lender's pack and account, into the borrower's
    /// pack as coin. Returns whether it was made; <paramref name="why"/> says why not.
    /// </summary>
    public static bool Lend(BotMobile lender, BotMobile borrower, int sum, string trade, double pays, out string why)
    {
        why = null;

        if (!BotTrades.LendersOn)
        {
            why = "lending is shut by order";

            return false;
        }

        if (lender == null || borrower == null || ReferenceEquals(lender, borrower) || borrower.Name == null || lender.Name == null)
        {
            why = "nobody to lend";

            return false;
        }

        if (InDebt(borrower) || IsMarked(borrower))
        {
            why = InDebt(borrower) ? "it already owes" : "it defaulted once and is lent nothing";

            return false;
        }

        if (_loans.Count >= MostStanding)
        {
            why = $"{MostStanding} loans already stand on the island";

            return false;
        }

        if (MindsRisk && Expected(sum) <= 0)
        {
            Risky++;
            why = $"at a default rate of {DefaultRate:P0} a loan of {sum}gp at {Interest:P0} is a loss";

            return false;
        }

        if (!CanLend(lender, sum))
        {
            why = $"{lender.Name} would not lend {sum}gp now";

            return false;
        }

        if (lender.Map != borrower.Map || !lender.InRange(borrower.Location, BotHandover.MeetTiles))
        {
            why = $"{lender.Name} was not standing beside it";

            return false;
        }

        if (!BotMarket.Charge(lender, sum))
        {
            why = $"{lender.Name} could not put {sum}gp down";

            return false;
        }

        BotMarket.Pay(borrower, sum);

        // Decided outside the work of each: the shop shift did not lose the principal, the errand did not earn it.
        BotYield.Aside(lender, sum);
        BotYield.Aside(borrower, -sum);

        var owed = (int)Math.Ceiling(sum * (1.0 + Math.Max(0.0, Interest)));
        var loan = new Loan
        {
            Debtor = borrower.Name,
            Lender = lender.Name,
            Principal = sum,
            Owed = owed,
            Mark = BotYield.Wealth(borrower),
            Taken = DateTime.UtcNow
        };

        _loans[borrower.Name] = loan;
        Made++;
        MadeGold += sum;

        logger.Information(
            "{Lender} lent {Borrower} the {Class} {Sum}gp at {Rate:P0} against its next takings: {Owed}gp owed, {Lien:P0} of every gain until repaid, a default after {Hours}h of its play — poor at {Purse}gp, short of its kit, its {Trade} pays {Pays:F0}/min",
            lender.Name,
            borrower.Name,
            borrower.Class?.Name,
            sum,
            Interest,
            owed,
            LienShare,
            DefaultHours,
            BotYield.Spendable(borrower) - sum,
            trade,
            pays
        );

        BotEvents.Post("loan", borrower, $"borrowed {sum}gp from {lender.Name}, {owed}gp to repay out of its next takings");

        var fill = new Dictionary<string, string>
        {
            ["amount"] = sum.ToString(),
            ["owed"] = owed.ToString(),
            ["lender"] = lender.Name,
            ["borrower"] = borrower.Name
        };

        BotCampTalk.Say(lender, "loan:given", null, borrower, fill);
        BotCampTalk.Say(borrower, "loan:thanks", null, lender, fill);

        return true;
    }

    // ---- The lien ---------------------------------------------------------------------------------------------------------

    /// <summary>The lien's look at every debtor, on its clock. From the module's timer.</summary>
    internal static void Collect(long now)
    {
        if (_loans.Count == 0)
        {
            _lastCollect = now;
            _collected = true;

            return;
        }

        if (_collected && now - _lastCollect < CollectMs)
        {
            return;
        }

        var minutes = _collected ? (now - _lastCollect) / 60000.0 : 0.0;

        _lastCollect = now;
        _collected = true;

        _byName.Clear();

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false, Name: not null } bot)
            {
                _byName[bot.Name] = bot;
            }
        }

        List<string> ended = null;

        foreach (var (name, loan) in _loans)
        {
            var debtor = _byName.GetValueOrDefault(name);

            if (debtor == null)
            {
                // Gone from the island, not resting: the lender has lost it.
                if (!BotRest.Records.ContainsKey(name))
                {
                    (ended ??= []).Add(name);
                    Gone++;
                    Default(loan, null, "it is gone from the island");
                }

                continue;
            }

            // Asleep: neither the lien nor the default clock runs.
            if (debtor.Map == null || debtor.Map == Map.Internal)
            {
                continue;
            }

            loan.Played += minutes;

            if (Take(loan, debtor))
            {
                (ended ??= []).Add(name);

                continue;
            }

            if (loan.Played >= DefaultHours * 60.0 && loan.Left > 0)
            {
                (ended ??= []).Add(name);
                Default(loan, debtor, $"{DefaultHours:0.#} hours of its play passed with {loan.Left}gp still owed");
            }
        }

        if (ended != null)
        {
            for (var i = 0; i < ended.Count; i++)
            {
                _loans.Remove(ended[i]);
            }
        }
    }

    /// <summary>The lien's share of whatever the debtor gained since the last look. Returns whether the debt is now paid.</summary>
    private static bool Take(Loan loan, BotMobile debtor)
    {
        var wealth = BotYield.Wealth(debtor);

        if (wealth <= loan.Mark)
        {
            loan.Mark = wealth;

            return false;
        }

        var take = Math.Min(loan.Left, (int)Math.Ceiling((wealth - loan.Mark) * Math.Clamp(LienShare, 0.0, 1.0)));

        if (take <= 0)
        {
            return false;
        }

        var lender = _byName.GetValueOrDefault(loan.Lender);

        if (lender == null && !BotRest.Records.ContainsKey(loan.Lender))
        {
            // The lender left the island: coin paid to nobody would be coin destroyed. The debt goes with it.
            Forgiven++;

            logger.Information(
                "{Debtor}'s debt of {Left}gp to {Lender} is forgiven: the lender is gone from the island",
                loan.Debtor,
                loan.Left,
                loan.Lender
            );

            return true;
        }

        if (lender == null || !BotMarket.Charge(debtor, take))
        {
            Held++;

            return false;
        }

        BotMarket.Refund(lender, take);

        BotYield.Aside(debtor, take);
        BotYield.Aside(lender, -take);

        var before = loan.Repaid;

        loan.Repaid += take;
        loan.Mark = wealth - take;
        Takes++;
        TakenGold += take;
        InterestGold += Math.Max(0, loan.Repaid - Math.Max(loan.Principal, before));

        if (loan.Left > 0)
        {
            return false;
        }

        Repaid++;

        logger.Information(
            "{Debtor} repaid {Lender} in full: {Owed}gp for {Principal}gp lent, out of its takings over {Hours:F1}h of play",
            loan.Debtor,
            loan.Lender,
            loan.Owed,
            loan.Principal,
            loan.Played / 60.0
        );

        BotEvents.Post("loan", debtor, $"repaid {loan.Lender} in full: {loan.Owed}gp for {loan.Principal}gp lent");

        var fill = new Dictionary<string, string> { ["lender"] = loan.Lender, ["owed"] = loan.Owed.ToString() };

        BotCampTalk.Say(debtor, "loan:repaid", null, null, fill);

        return true;
    }

    /// <summary>The debt is not going to be paid: the lender has lost what is left of the principal, and the borrower is marked.</summary>
    private static void Default(Loan loan, BotMobile debtor, string why)
    {
        var lost = Math.Max(0, loan.Principal - loan.Repaid);

        Defaulted++;
        LostGold += lost;
        _marked.Add(loan.Debtor);

        logger.Information(
            "{Debtor} defaulted on {Lender}'s loan — {Why}: {Repaid} of {Owed}gp repaid, {Lost}gp of the principal lost; it is lent nothing again",
            loan.Debtor,
            loan.Lender,
            why,
            loan.Repaid,
            loan.Owed,
            lost
        );

        BotEvents.Post("loan", loan.Debtor, null, null, 0, 0, $"defaulted on {loan.Lender}'s loan: {loan.Repaid} of {loan.Owed}gp repaid", null, null);

        if (_byName.GetValueOrDefault(loan.Lender) is { Deleted: false, Alive: true } lender && lender.Map != Map.Internal)
        {
            var fill = new Dictionary<string, string> { ["borrower"] = loan.Debtor, ["lost"] = lost.ToString() };

            BotCampTalk.Say(lender, "loan:default", null, debtor, fill);
        }
    }

    // ---- Saved --------------------------------------------------------------------------------------------------------------

    internal static void Save(IGenericWriter writer)
    {
        writer.WriteEncodedInt(_loans.Count);

        foreach (var (_, loan) in _loans)
        {
            writer.Write(loan.Debtor ?? "");
            writer.Write(loan.Lender ?? "");
            writer.WriteEncodedInt(loan.Principal);
            writer.WriteEncodedInt(loan.Owed);
            writer.WriteEncodedInt(loan.Repaid);
            writer.Write(loan.Played);
            writer.WriteEncodedInt(loan.Mark);
            writer.Write(loan.Taken);
        }

        writer.WriteEncodedInt(_marked.Count);

        foreach (var name in _marked)
        {
            writer.Write(name);
        }

        writer.Write(Repaid + Defaulted + _settledBefore);
        writer.Write(Defaulted + _defaultedBefore);
    }

    internal static int Load(IGenericReader reader, out int marked)
    {
        _loans.Clear();
        _marked.Clear();

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var loan = new Loan
            {
                Debtor = reader.ReadString(),
                Lender = reader.ReadString(),
                Principal = reader.ReadEncodedInt(),
                Owed = reader.ReadEncodedInt(),
                Repaid = reader.ReadEncodedInt(),
                Played = reader.ReadDouble(),
                Mark = reader.ReadEncodedInt(),
                Taken = reader.ReadDateTime()
            };

            if (!string.IsNullOrEmpty(loan.Debtor) && !string.IsNullOrEmpty(loan.Lender) && loan.Left > 0)
            {
                _loans[loan.Debtor] = loan;
            }
        }

        marked = reader.ReadEncodedInt();

        for (var i = 0; i < marked; i++)
        {
            var name = reader.ReadString();

            if (!string.IsNullOrEmpty(name))
            {
                _marked.Add(name);
            }
        }

        _settledBefore = reader.ReadLong();
        _defaultedBefore = reader.ReadLong();

        return _loans.Count;
    }

    // ---- Words --------------------------------------------------------------------------------------------------------------

    public static Dictionary<string, string[]> Phrases() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["took:borrow"] = ["I need a stake to get back to work.", "Off to see a lender.", "A loan, then. I'll pay it back."],
            ["loan:cry"] = ["Loans here: up to {amount} gold against your next takings, a fifth on top.", "Short of your kit? I lend to those who work."],
            ["loan:given"] = ["There's {amount} gold, {other}. {owed} back out of your next takings.", "{amount} gold. Spend it on your kit, not on ale."],
            ["loan:thanks"] = ["Thank you, {other}. You'll have it back.", "I'll pay it back from my next spoils, {other}.", "Fair terms. Thank you."],
            ["loan:repaid"] = ["Debt's paid. {owed} gold back to {lender}.", "I owe nobody anything now.", "Paid {lender} back in full."],
            ["loan:default"] = ["{borrower} never paid me back. Never again.", "{lost} gold gone with {borrower}. That's lending for you."]
        };

    public static string Describe() =>
        $"lenders: {Made} loans made for {MadeGold}gp, {_loans.Count} standing ({Owing()}gp owed), {Repaid} repaid in full, {Defaulted} defaulted ({Gone} of them gone from the island) losing {LostGold}gp of principal, "
        + $"{Forgiven} forgiven (lender gone); the lien took {TakenGold}gp in {Takes} takes ({InterestGold}gp of it interest), {Held} takes held back for wants on the board; default rate {DefaultRate:P0} ({Risky} loans refused as a loss at it), {_marked.Count} marked; "
        + BotBorrower.Describe();

    private static long Owing()
    {
        long owed = 0;

        foreach (var (_, loan) in _loans)
        {
            owed += loan.Left;
        }

        return owed;
    }

    /// <summary>Counters only: the book itself is the save's, and a reload reads it back.</summary>
    public static void Forget()
    {
        Made = 0;
        MadeGold = 0;
        Repaid = 0;
        Takes = 0;
        TakenGold = 0;
        InterestGold = 0;
        Defaulted = 0;
        LostGold = 0;
        Gone = 0;
        Held = 0;
        Forgiven = 0;
        Risky = 0;
        _byName.Clear();
        _collected = false;
    }

    /// <summary>
    /// <b>Patrick's reset (07.10.2026, reset26): the moneylenders' book closed, so the save writes it empty.</b> <see cref="Forget"/>
    /// keeps the book for a reload, and the reset of 20:56 that night left <c>Saves/BotLoans</c> whole: a newcomer born under an
    /// old name would have been born owing an old lender, its takings liened from the first sale, or marked a defaulter and lent
    /// nothing. This empties exactly what <see cref="Save"/> writes — the loans, the marked names and the totals carried across
    /// restarts that the default rate is read from — and the borrowers' memory with it.
    ///
    /// <para>
    /// <b>No coin is held here.</b> A loan's principal went from the lender's purse to the borrower's when it was made, and every
    /// take since went back the same way; the book only says who owes whom. Both are bodies the reset deletes with everything they
    /// carry, so striking the debt makes and destroys nothing. Returns the loans struck off.
    /// </para>
    /// </summary>
    public static int Wipe()
    {
        var standing = _loans.Count;

        _loans.Clear();
        _marked.Clear();
        _settledBefore = 0;
        _defaultedBefore = 0;
        _lastCollect = 0;
        Forget();
        BotBorrower.Forget();

        return standing;
    }
}

/// <summary>Keeps the moneylenders' book across restarts: every loan standing, and the names of those who defaulted.</summary>
public sealed class BotLoanStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotLoanStore));

    private const int Shape = 1;

    private static BotLoanStore _store;

    public static void Configure() => _store ??= new BotLoanStore();

    public BotLoanStore() : base("BotLoans", 21)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotLoans.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning("The saved loans are shape {Found} and this build reads {Wanted}; every debt is forgotten", shape, Shape);

            return;
        }

        var standing = BotLoans.Load(reader, out var marked);

        logger.Information("Loans read back: {Standing} standing, {Marked} defaulters lent nothing again", standing, marked);
    }
}
