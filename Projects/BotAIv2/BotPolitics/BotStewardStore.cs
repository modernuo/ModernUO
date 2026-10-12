using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the towns' stewardships across restarts (07.10.2026, steward20): each town's steward guild and how long it has held
/// it, its coffer (held out of the treasury, so read back it is counted as held again, <see cref="BotStewardship.Start"/>), its tax,
/// the town's mood, its bars with their wall-clock ends, its last decrees and its last holiday, and which guilds were deposed
/// where and how long ago. A coin held for a holiday not yet announced is kept as coffer. A shape this build cannot read is
/// dropped whole, and then the coffers' coin stays where the treasury's save left it: out of the purse, counted nowhere; the
/// shape is 1 and has never changed.
/// </summary>
public sealed class BotStewardStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotStewardStore));

    private const int Shape = 1;

    private static BotStewardStore _store;

    public static void Configure() => _store ??= new BotStewardStore();

    public BotStewardStore() : base("BotStewardship", 23)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotStewardship.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning("The saved stewardships are shape {Found} and this build reads {Wanted}; the towns choose again", shape, Shape);

            return;
        }

        var offices = BotStewardship.Load(reader);

        logger.Information("Stewardships read back: {Offices} towns", offices);
    }
}

/// <summary>The store registered from outside <c>BotCore</c>, after it has read its own switch, as the events' is.</summary>
public static class BotStewardCore
{
    /// <summary>After <c>BotCore.Configure</c> (40), which sets <c>BotCore.Enabled</c>; before the world is read, which the store needs.</summary>
    [CallPriority(45)]
    public static void Configure()
    {
        if (!BotCore.Enabled)
        {
            return;
        }

        BotStewardStore.Configure();
    }
}

/// <summary><c>Configuration/bot-steward.json</c>: every key optional, PascalCase; nothing written keeps the code's number.</summary>
public sealed class BotStewardSettings
{
    public bool? Running { get; set; }

    public double? EveryMinutes { get; set; }

    public double? ChooseHours { get; set; }

    public int? LeastDone { get; set; }

    public double? TaxFloor { get; set; }

    public double? TaxCeiling { get; set; }

    public int? TaxMostStep { get; set; }

    public int? Allowance { get; set; }

    public int? CityKeep { get; set; }

    public int? CofferMost { get; set; }

    public int? Floor { get; set; }

    public int? Rich { get; set; }

    public double? PoorMinutes { get; set; }

    public double? FallHours { get; set; }

    public double? FallLeast { get; set; }

    public double? DeposedHours { get; set; }

    public int? HireGold { get; set; }

    public int? BountyRaise { get; set; }

    public int? MilitiaGold { get; set; }

    public int? BarCost { get; set; }

    public double? BarHours { get; set; }

    public int? HolidayCost { get; set; }

    public int? CaravanGold { get; set; }

    public bool? ModelChooses { get; set; }

    public bool? WalkerBars { get; set; }
}

public static class BotStewardConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotStewardConfig));

    private const string ConfigPath = "Configuration/bot-steward.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var s = JsonConfig.Deserialize<BotStewardSettings>(path);

        if (s == null)
        {
            JsonConfig.Serialize(path, new BotStewardSettings());
            logger.Information("Wrote a starter stewards file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotStewardship.Running = s.Running ?? BotStewardship.Running;
        BotStewardship.EveryMs = Ms(s.EveryMinutes, 60000) ?? BotStewardship.EveryMs;
        BotStewardship.ChooseMs = Ms(s.ChooseHours, 3600000) ?? BotStewardship.ChooseMs;
        BotStewardship.LeastDone = s.LeastDone ?? BotStewardship.LeastDone;
        BotStewardship.TaxFloor = s.TaxFloor ?? BotStewardship.TaxFloor;
        BotStewardship.TaxCeiling = s.TaxCeiling ?? BotStewardship.TaxCeiling;
        BotStewardship.TaxMostStep = s.TaxMostStep ?? BotStewardship.TaxMostStep;
        BotStewardship.Allowance = s.Allowance ?? BotStewardship.Allowance;
        BotStewardship.CityKeep = s.CityKeep ?? BotStewardship.CityKeep;
        BotStewardship.CofferMost = s.CofferMost ?? BotStewardship.CofferMost;
        BotStewardship.Floor = s.Floor ?? BotStewardship.Floor;
        BotStewardship.Rich = s.Rich ?? BotStewardship.Rich;
        BotStewardship.PoorMs = Ms(s.PoorMinutes, 60000) ?? BotStewardship.PoorMs;
        BotStewardship.FallMs = Ms(s.FallHours, 3600000) ?? BotStewardship.FallMs;
        BotStewardship.FallLeast = s.FallLeast ?? BotStewardship.FallLeast;
        BotStewardship.DeposedMs = Ms(s.DeposedHours, 3600000) ?? BotStewardship.DeposedMs;
        BotStewardship.HireGold = s.HireGold ?? BotStewardship.HireGold;
        BotStewardship.BountyRaise = s.BountyRaise ?? BotStewardship.BountyRaise;
        BotStewardship.MilitiaGold = s.MilitiaGold ?? BotStewardship.MilitiaGold;
        BotStewardship.BarCost = s.BarCost ?? BotStewardship.BarCost;
        BotStewardship.BarHours = s.BarHours ?? BotStewardship.BarHours;
        BotStewardship.HolidayCost = s.HolidayCost ?? BotStewardship.HolidayCost;
        BotStewardship.CaravanGold = s.CaravanGold ?? BotStewardship.CaravanGold;
        BotStewardship.ModelChooses = s.ModelChooses ?? BotStewardship.ModelChooses;
        BotStewardship.WalkerBars = s.WalkerBars ?? BotStewardship.WalkerBars;
    }

    private static int? Ms(double? value, int unit) => value == null ? null : (int)Math.Max(0, Math.Min(int.MaxValue, value.Value * unit));
}
