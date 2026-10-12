using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// A brewer's glass and herbs for a draught somebody has asked for, paid out of that want's own reservation and settled at the
/// hand-over; what is not settled so the brewer pays back.
///
/// <para>
/// <b>Patrick, 03.10.2026: "proposal 3 — a potion want's reserve pays the alchemist's bottles. Do it."</b> Measured on build 396
/// (04:19–12:47, 210 bots): 64,759 looks at the mortar, 23 took an order off the board and 67 brewed on spec; 1,946 looks were
/// brewers holding their herbs whose purse would not carry the glass (the fattest 24gp), and 53,006 held neither. The island
/// bought 4.4k gp of heal and cure potions an hour over counters (flow9) while 109 potion wants were said in eight hours and a
/// fifth of their lines failed "offered, nothing brought" or "taken by a carrier, not brought in time". A draught costs about
/// eight to make (a bottle at five, a herb at three) and is asked for at eight to eleven.
/// </para>
///
/// <para>
/// <b>An advance, not a gift.</b> At the counter the buyer's account pays what the brewer is short of for the draughts it took
/// on (<see cref="Lend"/>: the buyer's coin, drawn on its want's own reservation as the meeting would draw on it, into the
/// brewer's pack and over the counter in the same breath; what the counter did not take goes straight back). The want then holds
/// that much less (<see cref="On"/>, read by <c>BotMarket.Holds</c>), because the money is already out. At the hand-over the
/// advance comes off the bill (<see cref="Credit"/>, <see cref="Settle"/>): the buyer pays the rest of the offer and the brewer is
/// paid the rest. A brewer that does not hand the draughts over owes the money: called when the want is gone, when the want asks
/// less than is owed, or past <see cref="CallMs"/> with nothing on its way, and paid back out of the brewer's free money as soon as
/// it has any (<see cref="Sweep"/>). One loan a brewer at a time, for no more draughts than <see cref="MostUnits"/>, and never
/// for more than the hand-over would pay.
/// </para>
/// </summary>
public static class BotAdvance
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotAdvance));

    /// <summary>One brewer's loan against one want.</summary>
    public sealed class Loan
    {
        public Mobile Brewer;

        public Mobile Buyer;

        public BotWant Want;

        public Type Kind;

        /// <summary>Draughts still owed in kind.</summary>
        public int Units;

        /// <summary>Gold still owed: what the counter took, less what hand-overs settled and repayments returned.</summary>
        public int Gold;

        /// <summary>What was lent at the counter, for the log.</summary>
        public int Lent;

        public long Tick;

        public bool Called;
    }

    private static readonly Dictionary<Serial, Loan> _loans = [];

    /// <summary>Whether a want's reservation may pay a brewer's materials. Off: no new advance; loans standing are still settled.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How long a loan stands with nothing carrying it before it is called. Twenty minutes: a brew and a walk are five.</summary>
    public static int CallMs { get; set; } = 1200000;

    /// <summary>The least chance the engine gives the brewer of the draught, so the buyer's herbs are not lent to a likely miss.</summary>
    public static double LeastChance { get; set; } = 0.8;

    /// <summary>The most draughts one advance is for.</summary>
    public static int MostUnits { get; set; } = 5;

    /// <summary>
    /// How long a want stands before a brewer may brew for it on its advance. Three minutes: a quarter of the potion lines on build
    /// 396 were met by a carrier holding the draughts inside 3.2 minutes (median 9.3, 90th 38.8), and a carrier with stock in its
    /// pack is quicker than a counter and a mortar.
    /// </summary>
    public static int WaitMs { get; set; } = 180000;

    // ---- Counters ------------------------------------------------------------------------------------------------------------

    /// <summary>Funded rounds offered to a brewer, and the draughts they were for.</summary>
    public static long Offered { get; private set; }

    public static long OfferedUnits { get; private set; }

    /// <summary>Looks that found a want the brewer could brew for and passed it over, by why.</summary>
    public static long Unlikely { get; private set; }

    public static long Young { get; private set; }

    public static long Underpaid { get; private set; }

    public static long Unfunded { get; private set; }

    public static long NoCounter { get; private set; }

    /// <summary>Advances lent at a counter, their gold, and the bottles and herbs they bought; gold given back unspent at once.</summary>
    public static long Lends { get; private set; }

    public static long LentGold { get; private set; }

    public static long Bottles { get; private set; }

    public static long Herbs { get; private set; }

    public static long Unspent { get; private set; }

    /// <summary>Lends the buyer's purse refused at the counter.</summary>
    public static long Refused { get; private set; }

    /// <summary>Hand-overs that settled an advance, the gold they settled and the draughts handed.</summary>
    public static long Settles { get; private set; }

    public static long SettledGold { get; private set; }

    public static long SettledUnits { get; private set; }

    /// <summary>Loans called (the want gone, shrunk, or nothing carried past <see cref="CallMs"/>), repayments and their gold.</summary>
    public static long Calls { get; private set; }

    public static long Repaid { get; private set; }

    public static long RepaidGold { get; private set; }

    /// <summary>Loans closed with gold still owed because the brewer or the buyer left the world.</summary>
    public static long WrittenOffGold { get; private set; }

    public static long CancelledGold { get; private set; }

    /// <summary>Draughts handed over against a want: by a brewer on the want's advance, and by anybody else.</summary>
    public static long HandedOnAdvance { get; private set; }

    public static long HandedOther { get; private set; }

    // ---- Questions ---------------------------------------------------------------------------------------------------------

    /// <summary>The loan this brewer has standing, or null.</summary>
    public static Loan Of(Mobile brewer) => brewer != null && _loans.TryGetValue(brewer.Serial, out var loan) ? loan : null;

    /// <summary>The gold out against this want: its reservation holds that much less, because the money is already spent on it.</summary>
    public static int On(BotWant want)
    {
        if (want == null || _loans.Count == 0)
        {
            return 0;
        }

        var total = 0;

        foreach (var loan in _loans.Values)
        {
            if (ReferenceEquals(loan.Want, want))
            {
                total += Math.Max(0, loan.Gold);
            }
        }

        return total;
    }

    /// <summary>
    /// Draughts of this kind this brewer owes in kind on a want still standing: the porter does not keep them for its kit
    /// (<c>BotUnload.Keeps</c>), so the carrier's look sees them as the buyer's.
    /// </summary>
    public static int Owing(Mobile brewer, Type kind)
    {
        var loan = Of(brewer);

        return loan != null && loan.Kind == kind && loan.Units > 0 && BotMarket.Stands(loan.Want) ? loan.Units : 0;
    }

    /// <summary>The buyer's gold this carrier already holds against its want for this kind: what comes off the bill at the meeting.</summary>
    public static int Credit(IBotWilful carrier, Mobile buyer, Type kind)
    {
        var loan = Of(carrier?.Self);

        return loan != null && kind != null && loan.Kind == kind && ReferenceEquals(loan.Buyer, buyer) ? Math.Max(0, loan.Gold) : 0;
    }

    /// <summary>The same, for one want. See <c>BotMarket.Affords</c>.</summary>
    public static int Credit(IBotWilful carrier, BotWant want)
    {
        var loan = Of(carrier?.Self);

        return loan != null && ReferenceEquals(loan.Want, want) ? Math.Max(0, loan.Gold) : 0;
    }

    /// <summary>Whether this kind is a draught the island drinks: the only kinds an advance is for.</summary>
    public static bool Draught(Type kind)
    {
        if (kind == null)
        {
            return false;
        }

        var families = BotArsenal.Draughts;

        for (var i = 0; i < families.Count; i++)
        {
            if (BotArsenal.Potion(families[i]) == kind)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>How often one brewer looks for a want to brew on its advance, while there is one it could take.</summary>
    public static int LookMs { get; set; } = 30000;

    private static readonly Dictionary<Serial, long> _looked = [];

    /// <summary>Whether this brewer may look again: the counter search behind a look is not asked on every review.</summary>
    internal static bool Due(Mobile body)
    {
        if (body == null)
        {
            return false;
        }

        var now = Core.TickCount;

        if (_looked.TryGetValue(body.Serial, out var last) && now - last < LookMs)
        {
            return false;
        }

        if (_looked.Count > 2048)
        {
            _looked.Clear();
        }

        _looked[body.Serial] = now;

        return true;
    }

    /// <summary>
    /// The recipe for this draught that this bot's Alchemy carries, with the same margin as every look at the mortar
    /// (<see cref="BotFlask.Margin"/>), whatever the pack holds; and the engine's chance of it. Null when the skill does not reach.
    /// </summary>
    public static Server.Engines.Craft.CraftItem Able(Mobile body, Type kind, out double chance)
    {
        chance = 0.0;

        var system = BotFlask.System;

        if (body == null || system == null || kind == null)
        {
            return null;
        }

        var able = body.Skills[SkillName.Alchemy].Value;
        var recipes = system.CraftItems;

        for (var i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];

            if (recipe.ItemType != kind || !BotFlask.Twofold(recipe, out var reagent, out _, out _))
            {
                continue;
            }

            if (BotFlask.Requirement(recipe) > able - BotFlask.Margin)
            {
                return null;
            }

            chance = recipe.GetSuccessChance(body, reagent, system, false, out var all);

            return all ? recipe : null;
        }

        return null;
    }

    // ---- What changes ------------------------------------------------------------------------------------------------------

    internal static void Looked(int units)
    {
        Offered++;
        OfferedUnits += units;
    }

    internal static void PassedUnlikely() => Unlikely++;

    internal static void PassedYoung() => Young++;

    internal static void PassedUnderpaid() => Underpaid++;

    internal static void PassedUnfunded() => Unfunded++;

    internal static void PassedNoCounter() => NoCounter++;

    /// <summary>
    /// The buyer's account pays <paramref name="gold"/> into the brewer's pack, standing at the counter, for
    /// <paramref name="units"/> draughts of its want. Drawn on the want's own reservation (<c>BotMarket.Charge</c> against its
    /// kind), as the meeting would draw on it. Returns false, and moves nothing, when the buyer cannot put it down.
    /// </summary>
    internal static bool Lend(IBotWilful brewer, BotWant want, int units, int gold)
    {
        var body = brewer?.Self;
        var buyer = want?.Buyer?.Self;

        if (body == null || buyer is not { Deleted: false } || gold <= 0 || units <= 0 || Of(body) != null || !BotMarket.Stands(want))
        {
            return false;
        }

        // The meeting's own rule (BotMarket.Charge): what the buyer has free and what this want holds, nothing its other wants hold.
        if ((long)BotYield.Spendable(buyer) + BotMarket.ReservedFor(buyer, want.Kind) < gold || !Draw(buyer, gold))
        {
            Refused++;

            return false;
        }

        BotMarket.Pay(body, gold);

        // Taken out of the buyer by a decision outside whatever it is doing: its work does not learn from it. See BotYield.Aside.
        BotYield.Aside(buyer, gold);

        if (_loans.Count > 1024)
        {
            Prune();
        }

        _loans[body.Serial] = new Loan
        {
            Brewer = body,
            Buyer = buyer,
            Want = want,
            Kind = want.Kind,
            Units = units,
            Gold = gold,
            Lent = gold,
            Tick = Core.TickCount
        };

        Lends++;
        LentGold += gold;
        BotMarket.Touched();

        return true;
    }

    /// <summary>
    /// What the counter took of the advance, and what it bought: the rest goes back to the buyer at once, out of the brewer's pack
    /// (or account, where the counter's own withdrawal left it), never out of the brewer's own money.
    /// </summary>
    internal static void Spent(IBotWilful brewer, int spent, int bottles, int herbs)
    {
        var body = brewer?.Self;
        var loan = Of(body);

        if (loan == null)
        {
            return;
        }

        Bottles += Math.Max(0, bottles);
        Herbs += Math.Max(0, herbs);

        var back = loan.Gold - Math.Max(0, spent);

        if (back <= 0)
        {
            return;
        }

        back = Take(body, back);

        if (back > 0)
        {
            BotMarket.Refund(loan.Buyer, back);
            BotYield.Aside(loan.Buyer, -back);
            loan.Gold -= back;
            Unspent += back;
            BotMarket.Touched();
        }

        if (loan.Gold <= 0)
        {
            _loans.Remove(body.Serial);
        }
    }

    /// <summary>A hand-over from the brewer to its buyer took <paramref name="gold"/> of the advance off the bill, for this many draughts.</summary>
    internal static void Settle(IBotWilful seller, Mobile buyer, Type kind, int gold, int units)
    {
        var body = seller?.Self;
        var loan = Of(body);

        if (loan == null || gold <= 0 || loan.Kind != kind || !ReferenceEquals(loan.Buyer, buyer))
        {
            return;
        }

        loan.Gold = Math.Max(0, loan.Gold - gold);
        loan.Units = Math.Max(0, loan.Units - Math.Max(0, units));

        Settles++;
        SettledGold += gold;
        SettledUnits += Math.Max(0, units);
        BotMarket.Touched();

        if (loan.Gold <= 0)
        {
            _loans.Remove(body.Serial);
        }
    }

    /// <summary>A draught handed over against a want, counted by whether its seller held the want's advance.</summary>
    internal static void Handed(Type kind, int units, bool onAdvance)
    {
        if (units <= 0 || !Draught(kind))
        {
            return;
        }

        if (onAdvance)
        {
            HandedOnAdvance += units;
        }
        else
        {
            HandedOther += units;
        }
    }

    /// <summary>
    /// The market's beat: loans whose want is gone, whose want asks less than is owed, or that nothing has carried past
    /// <see cref="CallMs"/>, are called and paid back out of the brewer's free money as far as it goes; a brewer or buyer that has
    /// left the world closes its loan, counted.
    /// </summary>
    internal static void Sweep(long now)
    {
        if (_loans.Count == 0)
        {
            return;
        }

        List<Serial> closed = null;

        foreach (var (serial, loan) in _loans)
        {
            if (loan.Gold <= 0)
            {
                (closed ??= []).Add(serial);

                continue;
            }

            if (loan.Brewer is not { Deleted: false })
            {
                WrittenOffGold += loan.Gold;
                (closed ??= []).Add(serial);

                continue;
            }

            if (loan.Buyer is not { Deleted: false })
            {
                CancelledGold += loan.Gold;
                (closed ??= []).Add(serial);

                continue;
            }

            var stands = BotMarket.Stands(loan.Want);
            var owedByWant = stands ? (long)loan.Want.Amount * loan.Want.Offer : 0;
            var carrying = Carrying(loan);
            var due = !stands ? loan.Gold : owedByWant < loan.Gold ? (int)(loan.Gold - owedByWant) : 0;

            if (due <= 0 && !carrying && now - loan.Tick >= CallMs)
            {
                due = loan.Gold;
            }

            if (due <= 0)
            {
                continue;
            }

            if (!loan.Called)
            {
                loan.Called = true;
                Calls++;
            }

            // Out of what the brewer has free: its own wants' money is not touched, and what it cannot pay yet it pays later.
            var pay = Math.Min(due, BotYield.Spendable(loan.Brewer));

            if (pay <= 0 || !BotMarket.Charge(loan.Brewer, pay))
            {
                continue;
            }

            BotMarket.Refund(loan.Buyer, pay);
            BotYield.Aside(loan.Brewer, pay);
            BotYield.Aside(loan.Buyer, -pay);

            loan.Gold -= pay;
            RepaidGold += pay;
            BotMarket.Touched();

            if (loan.Gold <= 0)
            {
                Repaid++;
                (closed ??= []).Add(serial);

                logger.Information(
                    "{Brewer} paid back the {Lent}gp {Buyer}'s want for {Kind} had advanced for its glass and herbs",
                    loan.Brewer.Name,
                    loan.Lent,
                    loan.Buyer.Name,
                    loan.Kind.Name
                );
            }
        }

        if (closed == null)
        {
            return;
        }

        for (var i = 0; i < closed.Count; i++)
        {
            _loans.Remove(closed[i]);
        }

        BotMarket.Touched();
    }

    /// <summary>Whether the brewer's work in hand, or the work it has put down, is still bringing this want's draughts.</summary>
    private static bool Carrying(Loan loan)
    {
        if (loan.Brewer is not BotMobile { } bot)
        {
            return false;
        }

        var resolve = bot.Resolve;
        var held = resolve?.Deed as IBotWantCarrier ?? resolve?.Paused?.Deed as IBotWantCarrier;

        return held != null && held.Carries(loan.Want);
    }

    /// <summary>
    /// The buyer's side of the advance: drawn on its account first, where its wants' money is kept (<see cref="BotMarket.Secure"/>)
    /// and a cheque reaches it wherever it stands, and the pocket only for what the account lacks. All or nothing.
    /// </summary>
    private static bool Draw(Mobile buyer, int gold)
    {
        var pack = buyer?.Backpack;

        if (buyer == null || gold <= 0)
        {
            return false;
        }

        var fromBank = Math.Min(gold, Banker.GetBalance(buyer));

        if (fromBank > 0 && !Banker.Withdraw(buyer, fromBank))
        {
            fromBank = 0;
        }

        var rest = gold - fromBank;

        if (rest <= 0)
        {
            return true;
        }

        if (pack != null && pack.GetAmount(typeof(Gold)) >= rest && pack.ConsumeTotal(typeof(Gold), rest))
        {
            return true;
        }

        // Put back whole: the account, or the pack when the account will not take it. See BotMarket.Refund.
        BotMarket.Refund(buyer, fromBank);

        return false;
    }

    /// <summary>Coin out of the pack, then the account: the buyer's own advance going back, so the brewer's reservations do not bar it.</summary>
    private static int Take(Mobile who, int amount)
    {
        var pack = who?.Backpack;

        if (pack == null || amount <= 0)
        {
            return 0;
        }

        var coin = Math.Min(amount, pack.GetAmount(typeof(Gold)));

        if (coin > 0 && !pack.ConsumeTotal(typeof(Gold), coin))
        {
            coin = 0;
        }

        var rest = amount - coin;

        if (rest > 0)
        {
            var balance = Banker.GetBalance(who);
            var draw = Math.Min(rest, balance);

            if (draw > 0 && Banker.Withdraw(who, draw))
            {
                coin += draw;
            }
        }

        return coin;
    }

    private static void Prune()
    {
        List<Serial> gone = [];

        foreach (var (serial, loan) in _loans)
        {
            if (loan.Brewer is not { Deleted: false } || loan.Buyer is not { Deleted: false })
            {
                gone.Add(serial);
            }
        }

        for (var i = 0; i < gone.Count; i++)
        {
            _loans.Remove(gone[i]);
        }
    }

    /// <summary>Gold out on loans now, for the summary.</summary>
    private static (int Loans, long Gold) Standing()
    {
        long gold = 0;

        foreach (var loan in _loans.Values)
        {
            gold += Math.Max(0, loan.Gold);
        }

        return (_loans.Count, gold);
    }

    public static string Describe()
    {
        var (loans, gold) = Standing();

        return $"draughts on a want's advance: {Offered} rounds offered to a brewer for {OfferedUnits} draughts somebody asked for "
               + $"(looks passed over: {Young} wants younger than {WaitMs / 60000} minutes, left to carriers holding the draughts, {Unlikely} for less than {LeastChance:P0} of brewing it, {Underpaid} with the offer under the glass and herbs, {Unfunded} the buyer's money would not carry it, {NoCounter} with no counter in reach selling both); "
               + $"{Lends} advances lent at a counter for {LentGold}gp ({Bottles} bottles and {Herbs} herbs bought, {Unspent}gp given back unspent, {Refused} refused by the buyer's purse); "
               + $"{Settles} hand-overs settled {SettledGold}gp of it against {SettledUnits} draughts; {Calls} loans called, {Repaid} repaid by their brewer ({RepaidGold}gp), {WrittenOffGold}gp written off with the brewer gone and {CancelledGold}gp with the buyer gone; "
               + $"{loans} loans standing for {gold}gp; draughts handed against wants: {HandedOnAdvance} by a brewer on the want's advance, {HandedOther} by anybody else";
    }

    public static void Forget()
    {
        _loans.Clear();
        _looked.Clear();
        Offered = 0;
        OfferedUnits = 0;
        Unlikely = 0;
        Young = 0;
        Underpaid = 0;
        Unfunded = 0;
        NoCounter = 0;
        Lends = 0;
        LentGold = 0;
        Bottles = 0;
        Herbs = 0;
        Unspent = 0;
        Refused = 0;
        Settles = 0;
        SettledGold = 0;
        SettledUnits = 0;
        Calls = 0;
        Repaid = 0;
        RepaidGold = 0;
        WrittenOffGold = 0;
        CancelledGold = 0;
        HandedOnAdvance = 0;
        HandedOther = 0;
    }
}
