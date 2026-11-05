using System;
using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-trades.json</c> may say. Everything optional and PascalCase: a key in lower case is silently the code's
/// default, and the <c>Trades:</c> boot line is the proof of what the establishments run with.
/// </summary>
public sealed class BotTradesSettings
{
    public bool? Running { get; set; }

    public bool? Taverns { get; set; }

    public bool? Lenders { get; set; }

    public int? MostTaverns { get; set; }

    public double? TavernShiftMinutes { get; set; }

    public int? LeastMeals { get; set; }

    public int? GuestReach { get; set; }

    public double? GuestMinutes { get; set; }

    public double? TavernRestShare { get; set; }

    public int? TavernRegenPoints { get; set; }

    public double? SupperBuff { get; set; }

    public double? MealMarkup { get; set; }

    public double? SupperPrior { get; set; }

    public int? LoanSum { get; set; }

    public double? LoanInterest { get; set; }

    public double? LienShare { get; set; }

    public double? DefaultHours { get; set; }

    public int? LenderWealth { get; set; }

    public int? LenderKeeps { get; set; }

    public int? PoorBar { get; set; }

    public bool? Infirmaries { get; set; }

    public double? HealFeePerHit { get; set; }

    public int? RaiseFee { get; set; }

    public int? RaiseReach { get; set; }

    public int? PatientReach { get; set; }

    public double? PatientHurtUnder { get; set; }

    public int? RaiseHoldMs { get; set; }

    public bool? SpareKeepers { get; set; }

    public double? RaiseMarkup { get; set; }

    public int? ScrollKeep { get; set; }

    public bool? Libraries { get; set; }

    public double? LibraryShiftMinutes { get; set; }

    public double? LibraryInscribe { get; set; }

    public int? LibraryReach { get; set; }

    public double? LibraryMarkup { get; set; }

    public bool? Ragmen { get; set; }

    public int? MostRagmen { get; set; }

    public double? RagmanShare { get; set; }

    public int? RagPostNear { get; set; }

    public int? RagPostFar { get; set; }

    public int? RagSafeSlack { get; set; }

    public int? RagmanPurse { get; set; }

    /// <summary>Whether dice are played at tavern tables. See BotDice.Running.</summary>
    public bool? DiceGames { get; set; }

    /// <summary>The chance a table's line is a new game. See BotDice.GameChance.</summary>
    public double? DiceChance { get; set; }

    /// <summary>The stake's bounds, in gold. See BotDice.StakeMin and BotDice.StakeMax.</summary>
    public int? DiceStakeMin { get; set; }

    public int? DiceStakeMax { get; set; }
}

/// <summary>
/// The establishments as a module: reads <c>bot-trades.json</c>, puts their words into the voice's bank, offers the cook's tavern,
/// the guest's supper and the borrower's walk, and runs one clock — a line round each table, the lenders' call, the lien.
/// </summary>
public sealed class BotTradesModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTradesModule));

    private const string ConfigPath = "Configuration/bot-trades.json";

    /// <summary>The clock's period. A second: the table talk's own grain; the lien and the lenders' call keep their own clocks.</summary>
    public static int TickMs { get; set; } = 1000;

    /// <summary>How often the lenders' call is looked for. Thirty seconds.</summary>
    public static int CryMs { get; set; } = 30000;

    private static Timer _timer;

    private static long _cryTick;

    public override string Name => "Trades";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Will", "Movement", "Shopkeep"];

    public override void Start()
    {
        Load();

        var words = Words(BotTaverns.Phrases()) + Words(BotLoans.Phrases()) + Words(BotInfirmaries.Phrases()) + Words(BotLibraries.Phrases())
                    + Words(BotRagmen.Phrases());

        BotWill.Offer(new BotTaverner());
        BotWill.Offer(new BotSupperer());
        BotWill.Offer(new BotBorrower());
        BotWill.Offer(new BotInfirmarer());
        BotWill.Offer(new BotPatienter());
        BotWill.Offer(new BotLibrarian());
        BotWill.Offer(new BotReader());
        BotWill.Offer(new BotRagman());
        BotWill.Offer(new BotRagSeller());
        BotWill.Offer(new BotRagHauler());

        _timer?.Stop();
        _timer = new TradesTimer(TimeSpan.FromMilliseconds(Math.Max(250, TickMs)));
        _timer.Start();

        logger.Information(
            "Trades: establishments are {State}. Taverns {Taverns}: a cook with a skillet and {Least}+ meals keeps one at an inn for {Shift} min (at most {Most} on the island, {Spacing} tiles apart), "
            + "meals at {Markup:P0} of the market's price; a free bot within {Reach} tiles that is tiring, bored, hurt or has nothing cooked, and keeps {Keeps}gp after the plate, is asked to supper (at {Prior}/min and more), "
            + "sits {Guest} min with play given back at {Rest:P0} of the inn's rate and {Regen} points every {Every}s; a tired bot goes to an inn with a tavern within {Detour} tiles further than the nearest, eats first, and wakes to {Buff}x the rested buff. "
            + "Lenders {Lenders}: a keeper of a shop by the bank with {Wealth}gp spendable lends up to {Sum}gp ({Least2}+) at {Interest:P0} to a bot under {Poor}gp short of its kit with work that paid {Pays}/min; "
            + "the lien takes {Lien:P0} of every gain, a default after {Hours}h of the borrower's play loses the lender the rest and marks the borrower. "
            + "Infirmaries {Infirmaries}: a guild's Healer with {Bandages}+ bandages or a heal keeps one at its guild's seat for {InfShift} min; a wounded guildmate or ally within {PatientReach} tiles that cannot heal itself is bound there for {FeePerHit}gp a point ({FeeLeast}+), "
            + "and a fallen one within {RaiseReach} tiles is raised where it fell for {RaiseFee}gp, the revival held {Hold}s for it; {Words} occasions added to the voice's bank",
            BotTrades.Running ? "on" : "off",
            BotTrades.Taverns ? "on" : "off",
            BotTaverns.LeastMeals,
            BotTaverns.ShiftMinutes,
            BotTaverns.MostOpen,
            BotTaverns.Spacing,
            BotTaverns.Markup,
            BotTaverns.GuestReach,
            BotTaverns.GuestKeeps,
            BotSupperer.Prior,
            BotTaverns.GuestMinutes,
            BotTaverns.RestShare,
            BotTaverns.RegenPoints,
            BotTaverns.RegenEveryMs / 1000,
            BotTaverns.Detour,
            BotTaverns.SupperBuff,
            BotTrades.Lenders ? "on" : "off",
            BotLoans.LenderWealth,
            BotLoans.Sum,
            BotLoans.Least,
            BotLoans.Interest,
            BotLoans.PoorBar,
            BotBorrower.LeastPays,
            BotLoans.LienShare,
            BotLoans.DefaultHours,
            BotInfirmaries.Running ? "on" : "off",
            BotInfirmaries.LeastBandages,
            BotInfirmaries.ShiftMinutes,
            BotInfirmaries.PatientReach,
            BotInfirmaries.FeePerHit,
            BotInfirmaries.FeeLeast,
            BotInfirmaries.RaiseReach,
            BotInfirmaries.RaiseFee,
            BotInfirmaries.RaiseHoldMs / 1000,
            words
        );

        logger.Information(
            "Trades, the second three (life18b): infirmaries wait {Away}s for a healer called away, a healer at its open infirmary is {Spared} by a company's call, a guild's Healer stocks a resurrection scroll keeping {Keep}gp, "
            + "a raise read off a scroll costs its market price x{Markup}; patients under {Hurt:P0} of their hits. Libraries {Libraries}: a guild's scribe of Inscription {Inscribe}+ with {LeastScrolls}+ spare scrolls its guild's or allies' casters lack keeps one by the seat for {LibShift} min, "
            + "a caster within {LibReach} tiles short of one buys it there at {LibMarkup:P0} of the market's price. Ragmen {Ragmen}: a trader with {Purse}gp keeps a buying post {Near}-{Far} tiles from the busiest mouth (at most {MostPosts}), on ground clear for {Slack} tiles, "
            + "buys hunters' loot at {Share:P0} of the nearest counter's price, leaves at once at a danger in sight, and carries the goods to a counter after",
            BotInfirmaries.AwayMs / 1000,
            BotInfirmaries.SpareKeepers ? "left alone" : "taken",
            BotInfirmaries.ScrollKeep,
            BotInfirmaries.RaiseMarkup,
            BotPatienter.HurtUnder,
            BotTrades.Libraries ? "on" : "off",
            BotLibraries.LeastInscribe,
            BotLibraries.LeastScrolls,
            BotLibraries.ShiftMinutes,
            BotLibraries.ReaderReach,
            BotLibraries.Markup,
            BotTrades.Ragmen ? "on" : "off",
            BotRagmen.LeastPurse,
            BotRagmen.PostNear,
            BotRagmen.PostFar,
            BotRagmen.MostOpen,
            BotRagmen.SafeSlack,
            BotRagmen.Share
        );
    }

    private static int Words(Dictionary<string, string[]> bank)
    {
        var added = 0;

        foreach (var (key, lines) in bank)
        {
            if (!BotVoice.Has(key))
            {
                BotVoice.Phrases(new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { [key] = lines });
                added++;
            }
        }

        return added;
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotTradesSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotTradesSettings());
            logger.Information("Wrote a starter trades file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotTrades.Running = settings.Running ?? BotTrades.Running;
        BotTrades.Taverns = settings.Taverns ?? BotTrades.Taverns;
        BotTrades.Lenders = settings.Lenders ?? BotTrades.Lenders;
        BotTaverns.MostOpen = settings.MostTaverns ?? BotTaverns.MostOpen;
        BotTaverns.ShiftMinutes = settings.TavernShiftMinutes ?? BotTaverns.ShiftMinutes;
        BotTaverns.LeastMeals = settings.LeastMeals ?? BotTaverns.LeastMeals;
        BotTaverns.GuestReach = settings.GuestReach ?? BotTaverns.GuestReach;
        BotTaverns.GuestMinutes = settings.GuestMinutes ?? BotTaverns.GuestMinutes;
        BotTaverns.RestShare = settings.TavernRestShare ?? BotTaverns.RestShare;
        BotTaverns.RegenPoints = settings.TavernRegenPoints ?? BotTaverns.RegenPoints;
        BotTaverns.SupperBuff = settings.SupperBuff ?? BotTaverns.SupperBuff;
        BotTaverns.Markup = settings.MealMarkup ?? BotTaverns.Markup;
        BotSupperer.Prior = settings.SupperPrior ?? BotSupperer.Prior;
        BotLoans.Sum = settings.LoanSum ?? BotLoans.Sum;
        BotLoans.Interest = settings.LoanInterest ?? BotLoans.Interest;
        BotLoans.LienShare = settings.LienShare ?? BotLoans.LienShare;
        BotLoans.DefaultHours = settings.DefaultHours ?? BotLoans.DefaultHours;
        BotLoans.LenderWealth = settings.LenderWealth ?? BotLoans.LenderWealth;
        BotLoans.LenderKeeps = settings.LenderKeeps ?? BotLoans.LenderKeeps;
        BotLoans.PoorBar = settings.PoorBar ?? BotLoans.PoorBar;
        BotInfirmaries.Running = settings.Infirmaries ?? BotInfirmaries.Running;
        BotInfirmaries.FeePerHit = settings.HealFeePerHit ?? BotInfirmaries.FeePerHit;
        BotInfirmaries.RaiseFee = settings.RaiseFee ?? BotInfirmaries.RaiseFee;
        BotInfirmaries.RaiseReach = settings.RaiseReach ?? BotInfirmaries.RaiseReach;
        BotInfirmaries.PatientReach = settings.PatientReach ?? BotInfirmaries.PatientReach;
        BotPatienter.HurtUnder = settings.PatientHurtUnder ?? BotPatienter.HurtUnder;
        BotInfirmaries.RaiseHoldMs = settings.RaiseHoldMs ?? BotInfirmaries.RaiseHoldMs;
        BotInfirmaries.SpareKeepers = settings.SpareKeepers ?? BotInfirmaries.SpareKeepers;
        BotInfirmaries.RaiseMarkup = settings.RaiseMarkup ?? BotInfirmaries.RaiseMarkup;
        BotInfirmaries.ScrollKeep = settings.ScrollKeep ?? BotInfirmaries.ScrollKeep;
        BotTrades.Libraries = settings.Libraries ?? BotTrades.Libraries;
        BotLibraries.ShiftMinutes = settings.LibraryShiftMinutes ?? BotLibraries.ShiftMinutes;
        BotLibraries.LeastInscribe = settings.LibraryInscribe ?? BotLibraries.LeastInscribe;
        BotLibraries.ReaderReach = settings.LibraryReach ?? BotLibraries.ReaderReach;
        BotLibraries.Markup = settings.LibraryMarkup ?? BotLibraries.Markup;
        BotTrades.Ragmen = settings.Ragmen ?? BotTrades.Ragmen;
        BotRagmen.MostOpen = settings.MostRagmen ?? BotRagmen.MostOpen;
        BotRagmen.Share = settings.RagmanShare ?? BotRagmen.Share;
        BotRagmen.PostNear = settings.RagPostNear ?? BotRagmen.PostNear;
        BotRagmen.PostFar = settings.RagPostFar ?? BotRagmen.PostFar;
        BotRagmen.SafeSlack = settings.RagSafeSlack ?? BotRagmen.SafeSlack;
        BotRagmen.LeastPurse = settings.RagmanPurse ?? BotRagmen.LeastPurse;
        BotDice.Running = settings.DiceGames ?? BotDice.Running;
        BotDice.GameChance = settings.DiceChance ?? BotDice.GameChance;
        BotDice.StakeMin = settings.DiceStakeMin ?? BotDice.StakeMin;
        BotDice.StakeMax = settings.DiceStakeMax ?? BotDice.StakeMax;
    }

    public override void Reset()
    {
        logger.Information("Trades, before the reload: {What}", BotTrades.Clause());

        _timer?.Stop();
        _timer = null;

        BotTrades.Forget();
    }

    private sealed class TradesTimer : Timer
    {
        public TradesTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick()
        {
            var now = Core.TickCount;

            try
            {
                BotTaverns.Talk(now);
                BotInfirmaries.Scan(now);

                // The lien runs with the switch off too: a debt made before it was thrown is still owed.
                BotLoans.Collect(now);

                if (now - _cryTick >= CryMs)
                {
                    _cryTick = now;
                    BotBorrower.Cry();
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "The trades' clock threw; it carries on at the next tick");
            }
        }
    }
}
