using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The events of the world as a module (07.10.2026): registers itself from its own <c>Configure</c> (no line in <c>BotCore</c>),
/// reads <c>Configuration/bot-events.json</c>, offers the five pieces of work the events make (the militia, the fair, the caravan's
/// lead and its guards, the rumour), settles what the store read back, and starts the events' clock and the <c>Events:</c> line.
/// </summary>
public sealed class BotEventsModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotEventsModule));

    public override string Name => "Events";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Will", "Market", "Shops", "Squads", "Travel", "Population"];

    public override void Start()
    {
        BotEventsConfig.Load();

        BotWill.Offer(new BotMilitiaCall());
        BotWill.Offer(new BotFairCall());
        BotWill.Offer(new BotCaravanCall());
        BotWill.Offer(new BotGuardCall());
        BotWill.Offer(new BotRumourCall());

        BotEventsStore.Apply();
        BotSeasons.Start();

        logger.Information(
            "The events of the world are {State}: an invasion every {Invasion:F1} hours (±{Jitter:P0}, the first after {First} minutes) of at most {Odds:P0} of the militia that answers, {Pool}gp a raid from the city's purse; "
            + "a fair every {Fair:F0} hours for {Lasts:F1}, counters at {Discount:P0}, tickets {Ticket}gp; a caravan every {Caravan} minutes, {Guards} guards at {Wage}gp, its trader standing for a guard more than {Spread} tiles back, {GuardBeat}ms a beat and {GuardWait}s a run at most; night {From:00}–{To:00} at roads ×{Danger:F2} and carriage ×{Pay:F2}; a rumour every {Rumour} minutes of {Chest}gp from the crown",
            BotSeasons.Running ? "on" : "off",
            BotInvasion.EveryMs / 3600000.0,
            BotInvasion.Jitter,
            BotInvasion.FirstMs / 60000,
            System.Math.Min(1.0, BotInvasion.Odds),
            BotInvasion.Purse,
            BotFair.EveryMs / 3600000.0,
            BotFair.LastsMs / 3600000.0,
            BotFair.Discount,
            BotFair.Ticket,
            BotCaravans.EveryMs / 60000,
            BotCaravans.MostGuards,
            BotCaravans.Wage,
            BotCaravans.GuardSpread,
            BotCaravans.GuardWaitBeatMs,
            BotCaravans.GuardWaitMs / 1000,
            BotNight.From,
            BotNight.To,
            BotNight.NightDanger,
            BotNight.NightPay,
            BotRumour.EveryMs / 60000,
            BotRumour.ChestGold
        );
    }

    public override void Reset()
    {
        logger.Information("Events, before the reload: {What}", BotSeasons.Describe());

        BotSeasons.Stop();

        // Whatever an event holds goes back before its counters are forgotten: the purse's coin, the creatures, the tickets.
        BotInvasion.Stand();
        BotCaravans.Stand();
        BotFair.Stand();

        BotInvasion.Forget();
        BotCaravans.Forget();
        BotFair.Forget();
        BotNight.Forget();
        BotRumour.Forget();
        BotSeasons.Forget();
    }
}

/// <summary>The events' way in: the module and the store registered from outside <c>BotCore</c>, after it has read its own switch.</summary>
public static class BotEventsCore
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotEventsCore));

    /// <summary>After <c>BotCore.Configure</c> (40), which sets <c>BotCore.Enabled</c>; before the world is read, which the store needs.</summary>
    [CallPriority(45)]
    public static void Configure()
    {
        if (!BotCore.Enabled)
        {
            return;
        }

        BotEventsStore.Configure();
        BotModules.Register(new BotEventsModule());
    }
}

/// <summary><c>Configuration/bot-events.json</c>: every key optional, PascalCase, nothing written means the code's own number.</summary>
public sealed class BotEventsSettings
{
    public bool? Running { get; set; }

    public bool? Invasions { get; set; }

    public int? InvasionEveryMs { get; set; }

    public int? InvasionFirstMs { get; set; }

    public int? InvasionPurse { get; set; }

    public double? InvasionOdds { get; set; }

    public int? InvasionWaves { get; set; }

    public int? InvasionLifeMs { get; set; }

    public bool? Fairs { get; set; }

    public int? FairEveryMs { get; set; }

    public int? FairFirstMs { get; set; }

    public int? FairLastsMs { get; set; }

    public double? FairDiscount { get; set; }

    public int? FairTicket { get; set; }

    public bool? Caravans { get; set; }

    public int? CaravanEveryMs { get; set; }

    public int? CaravanWage { get; set; }

    public int? CaravanGuards { get; set; }

    /// <summary>How far a guard may drop back before its trader stands for it, and the most it stands a run (12.10.2026, w7). See <see cref="BotCaravans.GuardSpread"/>.</summary>
    public int? CaravanGuardSpread { get; set; }

    public int? CaravanGuardWaitMs { get; set; }

    public bool? Night { get; set; }

    public int? NightFrom { get; set; }

    public int? NightTo { get; set; }

    public double? NightDanger { get; set; }

    public double? NightPay { get; set; }

    public bool? Rumours { get; set; }

    public int? RumourEveryMs { get; set; }

    public int? RumourGold { get; set; }

    public int? CityReserve { get; set; }
}

public static class BotEventsConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotEventsConfig));

    private const string ConfigPath = "Configuration/bot-events.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var s = JsonConfig.Deserialize<BotEventsSettings>(path);

        if (s == null)
        {
            JsonConfig.Serialize(path, new BotEventsSettings());

            logger.Information("Wrote a starter events file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotSeasons.Running = s.Running ?? BotSeasons.Running;
        BotSeasons.CityReserve = s.CityReserve ?? BotSeasons.CityReserve;

        BotInvasion.Running = s.Invasions ?? BotInvasion.Running;
        BotInvasion.EveryMs = s.InvasionEveryMs ?? BotInvasion.EveryMs;
        BotInvasion.FirstMs = s.InvasionFirstMs ?? BotInvasion.FirstMs;
        BotInvasion.Purse = s.InvasionPurse ?? BotInvasion.Purse;
        BotInvasion.Odds = s.InvasionOdds ?? BotInvasion.Odds;
        BotInvasion.Waves = s.InvasionWaves ?? BotInvasion.Waves;
        BotInvasion.LifeMs = s.InvasionLifeMs ?? BotInvasion.LifeMs;

        BotFair.Running = s.Fairs ?? BotFair.Running;
        BotFair.EveryMs = s.FairEveryMs ?? BotFair.EveryMs;
        BotFair.FirstMs = s.FairFirstMs ?? BotFair.FirstMs;
        BotFair.LastsMs = s.FairLastsMs ?? BotFair.LastsMs;
        BotFair.Discount = s.FairDiscount ?? BotFair.Discount;
        BotFair.Ticket = s.FairTicket ?? BotFair.Ticket;

        BotCaravans.Running = s.Caravans ?? BotCaravans.Running;
        BotCaravans.EveryMs = s.CaravanEveryMs ?? BotCaravans.EveryMs;
        BotCaravans.Wage = s.CaravanWage ?? BotCaravans.Wage;
        BotCaravans.MostGuards = s.CaravanGuards ?? BotCaravans.MostGuards;
        BotCaravans.GuardSpread = s.CaravanGuardSpread ?? BotCaravans.GuardSpread;
        BotCaravans.GuardWaitMs = s.CaravanGuardWaitMs ?? BotCaravans.GuardWaitMs;

        BotNight.Running = s.Night ?? BotNight.Running;
        BotNight.From = s.NightFrom ?? BotNight.From;
        BotNight.To = s.NightTo ?? BotNight.To;
        BotNight.NightDanger = s.NightDanger ?? BotNight.NightDanger;
        BotNight.NightPay = s.NightPay ?? BotNight.NightPay;

        BotRumour.Running = s.Rumours ?? BotRumour.Running;
        BotRumour.EveryMs = s.RumourEveryMs ?? BotRumour.EveryMs;
        BotRumour.ChestGold = s.RumourGold ?? BotRumour.ChestGold;
    }
}
