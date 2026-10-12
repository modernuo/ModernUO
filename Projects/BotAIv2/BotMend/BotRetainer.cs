using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A fighter with money hiring a healer to stand by it, and the wage that changes hands.
///
/// <para>
/// <b>Patrick's order of 15.09.2026: the well-off fighters take healers into their service.</b> Standing by a
/// fighter was unpaid work until now — a healer went to whoever was already fighting within sixty tiles, for
/// nothing but the skill of the wounds it bound — and unpaid work is refused by nobody and learned by nobody. A hire
/// is the same stint with coin in it: the fighter pays a wage out of its own pack for every minute the healer stands
/// by, the healer's ledger learns what escorting is worth, and the fighter walks into its next fight with somebody
/// behind it.
/// </para>
///
/// <para>
/// <b>Who hires is read off the fighter, not decided by it.</b> There is no errand on the fighter's side and no
/// negotiation: a fighter that is out — hunting, prowling, in a company or already in a fight — and can afford it
/// is hiring, and the nearest free healer that reads that fact takes the stint. The fighter never stops to think
/// about it, because the fighter's decisions are about fights, and this is the healer's decision about where to be.
/// </para>
///
/// <para>
/// <b>Paid out of the pack, a minute at a time, and booked aside for the fighter.</b> The engine pays from the
/// pack and so does this; a fighter whose pack runs dry ends the stint rather than running a debt. The wage is
/// booked through <see cref="BotYield.Aside"/> so the fighter's own work — the hunt it was on — is not taught that
/// hunting costs a healer's wage; the healer's side is the escort's own takings, which is the whole point.
/// </para>
/// </summary>
public static class BotRetainer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRetainer));

    /// <summary>Whether fighters hire at all. Off, and a healer only ever stands by a fighter for nothing.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// What a fighter has to be worth, pack and account together, before it hires. Three hundred: a bot is born with
    /// about 200 in its pack and loses its purse at a restart, so this is a fighter that has earned a hundred over its
    /// birth; at 600 nobody hired in the first six minutes of build 67 and the number had to be dialled down live,
    /// and again after the next restart.
    /// </summary>
    public static int Affluent { get; set; } = 300;

    /// <summary>The wage, in gold a minute. A five-minute stint is a bandage's price at a counter, five times over.</summary>
    public static int Wage { get; set; } = 5;

    /// <summary>How many minutes' wage the pack must hold before the fighter is read as hiring, so no stint is taken that cannot be paid once.</summary>
    public static int Minutes { get; set; } = 3;

    /// <summary>
    /// How far a healer looks for a fighter that is hiring. A hundred: 150 sent healers after fighters they never
    /// reached (11 "could not keep up" and 18 "could not get nearer" in the first 23 minutes of build 67).
    /// </summary>
    public static int Reach { get; set; } = 100;

    /// <summary>How often the wage is paid.</summary>
    public static int PayEveryMs { get; set; } = 60000;

    /// <summary>How long a hired healer keeps standing by a fighter that has gone quiet — home, a shop, a shelf — before the stint is over.</summary>
    public static int GraceMs { get; set; } = 60000;

    /// <summary>How near the fighter a hired healer has to be, at the minute, for that minute to be a served one. Fifteen: a heal reaches eight and a following healer trails a few.</summary>
    public static int Near { get; set; } = 15;

    /// <summary>Minutes of a hire that went unpaid because the healer was further than <see cref="Near"/> from the fighter.</summary>
    public static long Unserved { get; set; }

    /// <summary>Stints begun with a wage in them.</summary>
    public static long Hired { get; private set; }

    /// <summary>Gold that went from fighters to healers.</summary>
    public static long Paid { get; private set; }

    /// <summary>Payments made.</summary>
    public static long Payments { get; private set; }

    /// <summary>Stints ended because the fighter's pack could not pay the next minute.</summary>
    public static long Broke { get; private set; }

    /// <summary>Fighters looked at by a healer wanting a hire. A count of looks, not of fighters.</summary>
    public static long Looked { get; private set; }

    /// <summary>Looks that found the fighter at home or at a counter rather than out.</summary>
    public static long Idle { get; private set; }

    /// <summary>Looks that found the fighter unable to afford a healer.</summary>
    public static long Poor { get; private set; }

    /// <summary>Kinds of work a fighter is out on. A hired healer follows the fighter through all of these.</summary>
    private static readonly HashSet<string> _afield = new(StringComparer.OrdinalIgnoreCase)
    {
        BotSlay.Trade, BotProwl.Trade, BotBand.Trade, BotRescue.Trade, BotHarrow.Trade, BotSweep.Trade,
        BotDelve.Trade, BotRally.Trade, BotQuarrel.Trade, BotEnlist.Trade, BotFreedom.Trade, BotScout.Trade
    };

    /// <summary>Whether this fighter is out where a healer is wanted: in a company, in a fight, or on work that leads to one.</summary>
    public static bool Afield(BotMobile fighter) =>
        fighter is { Deleted: false, Alive: true }
        && (fighter.Squad != null || BotAccompany.Engaged(fighter) || _afield.Contains(fighter.Resolve?.Deed?.Kind ?? ""));

    /// <summary>
    /// Whether this fighter is somewhere a healer can join it: afield, and not prowling. A prowl is a walk that never
    /// stops, and a healer hired onto one trails it for the whole stint — 147 "fell behind" in the first 23 minutes of
    /// build 67 — so the hire waits until the fighter has found its fight or its company; the stint then follows it
    /// through the next prowl, which is what <see cref="Afield"/> is for.
    /// </summary>
    public static bool Joinable(BotMobile fighter) =>
        Afield(fighter) && !string.Equals(fighter.Resolve?.Deed?.Kind, BotProwl.Trade, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The wage this fighter is offering a healer right now, or nought.
    ///
    /// Read off the fighter's class, purse and work: a fighter — never a medic, a producer or anybody paid nothing by
    /// its class — that is out and can pay is hiring. Nothing on the fighter remembers this; it is asked whenever a
    /// healer looks.
    /// </summary>
    public static int Hiring(BotMobile fighter)
    {
        if (!Running || fighter is not { Deleted: false, Alive: true, Fallen: false } || fighter.Class is not { } klass)
        {
            return 0;
        }

        if (klass.Role is BotRole.Medic or BotRole.Producer || klass.Unpaid)
        {
            return 0;
        }

        Looked++;

        if (!Joinable(fighter))
        {
            Idle++;

            return 0;
        }

        // What it has free, pack and account together (02.10.2026, build next): a fighter banks before it sets out now (BotPurse.BeforeTrip)
        // and its wants' money sits in its account (BotMarket.Secure), and the wage is paid the way the market pays, the pack first.
        var free = BotYield.Spendable(fighter);

        if (free < Wage * Minutes || free < Affluent)
        {
            Poor++;

            return 0;
        }

        return Wage;
    }

    /// <summary>
    /// One minute's wage, from the fighter's pack into the healer's. False when the fighter cannot pay, which ends
    /// the stint.
    /// </summary>
    public static bool Pay(Mobile fighter, Mobile healer, int wage)
    {
        var pack = fighter?.Backpack;
        var purse = healer?.Backpack;

        if (pack == null || purse == null || wage <= 0)
        {
            return false;
        }

        // The pack first and the account after, never out of what the fighter's wants hold (build next). See BotMarket.Charge.
        if (!BotMarket.Charge(fighter, wage))
        {
            Broke++;

            return false;
        }

        purse.DropItem(new Gold(wage));

        // Taken out of the fighter by a decision outside its work: its hunt must not learn that hunting costs a wage.
        BotYield.Aside(fighter, wage);

        Paid += wage;
        Payments++;

        return true;
    }

    /// <summary>A stint with a wage in it has begun. For the count and the log.</summary>
    public static void Began(Mobile healer, Mobile fighter, int wage)
    {
        Hired++;

        logger.Information(
            "{Healer} is hired by {Fighter} at {Wage}gp a minute, out of a pack holding {Pocket}gp",
            healer?.Name,
            fighter?.Name,
            wage,
            fighter?.Backpack?.GetAmount(typeof(Gold)) ?? 0
        );
    }

    public static string Describe() =>
        !Running
            ? "no fighter hires a healer"
            : $"{Hired} healers hired at {Wage}gp a minute by fighters worth {Affluent}gp or more, {Paid}gp paid in {Payments} payments, "
              + $"{Broke} stints ended for want of pay, {Unserved} minutes unpaid for the healer being further than {Near} tiles off; of {Looked} looks at a fighter {Idle} found it not out and {Poor} found it unable to pay";

    public static void Forget()
    {
        Hired = 0;
        Paid = 0;
        Payments = 0;
        Broke = 0;
        Looked = 0;
        Idle = 0;
        Poor = 0;
        Unserved = 0;
    }
}
