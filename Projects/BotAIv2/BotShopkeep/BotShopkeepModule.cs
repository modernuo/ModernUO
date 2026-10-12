using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Bots' own shops as a module: reads its numbers, offers the shift and the customer's walk, and says every five minutes what
/// the shops did. Since 01.10.2026 the shops by Britain's bank are the market itself — the auction and its cap on the kinds a
/// bot may hold out are gone (see <c>BotMarket</c>).
/// </summary>
public sealed class BotShopkeepModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotShopkeepModule));

    public override string Name => "Shopkeep";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Will", "Shops", "Market", "Population"];

    /// <summary>How often the shops are summed up in the log. Five minutes, with every other summary.</summary>
    public static int SayEveryMs { get; set; } = 300000;

    private static Timer _timer;

    public override void Start()
    {
        BotShopkeepConfig.Load();

        BotWill.Offer(new BotShopkeeper());
        BotWill.Offer(new BotPatron());

        logger.Information(
            "Bots' shops are {State}, and they are the market: a bot with {Worth}gp or more of what the island is short of or somebody has asked for keeps shop by Britain's bank, or a guild's member by its guild's own town's, for {Shift} minutes, {Rest} minutes between shifts, at most {PerBank} to a bank and {Most} on the island; a customer looks {Seek} tiles for one, counts a minute's walk at {Walk:F0}gp, and buys within {Serve} tiles of the keeper; prices open under the nearest shopkeeper's ask or at the market's reckoning, whichever is less, and move by the market's own steps",
            BotShopkeep.Running ? "open" : "shut",
            BotShopkeep.OpenWorth,
            BotShopkeep.BankTiles,
            BotShopkeep.ShiftMinutes,
            BotShopkeep.RestMs / 60000,
            BotShopkeep.PerBank,
            BotShopkeep.MostOpen,
            BotShopkeep.SeekTiles,
            BotShopkeep.WalkGold,
            BotShopkeep.ServeReach
        );

        _timer?.Stop();
        _timer = new ShopsTimer(TimeSpan.FromMilliseconds(Math.Max(10000, SayEveryMs)));
        _timer.Start();
    }

    public override void Reset()
    {
        logger.Information("Bot shops, before the reload: {State}", BotShopkeep.Describe());

        _timer?.Stop();
        _timer = null;

        BotShopkeep.Forget();
    }

    private sealed class ShopsTimer : Timer
    {
        public ShopsTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        // The establishments' clause after the shops' own (07.10.2026, trades17). See BotTrades.Clause.
        protected override void OnTick() => logger.Information("Bot shops: {What}", BotShopkeep.Describe() + BotTrades.Clause());
    }
}
