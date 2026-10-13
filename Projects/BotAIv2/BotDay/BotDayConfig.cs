using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-day.json</c> may say. Its own file rather than a block of <c>bot-will.json</c>: the will's file is
/// the auction's numbers, and a reader opening it for <c>GoldPerSkillPoint</c> should not find the hours of the day there. A
/// missing key keeps the code's value; keys are PascalCase. See <see cref="BotDay"/>.
/// </summary>
public sealed class BotDaySettings
{
    /// <summary>The switch. Off, every hour is one and the rest clock orders by hours over, as before.</summary>
    public bool? Running { get; set; }

    /// <summary>The hours, local time, each part of the day begins; they must stand in order inside the day.</summary>
    public double? DawnAt { get; set; }

    public double? MorningAt { get; set; }

    public double? NoonAt { get; set; }

    public double? AfternoonAt { get; set; }

    public double? EveningAt { get; set; }

    public double? NightAt { get; set; }

    /// <summary>How far the hour leans a price that fits it: ×Lift up, ×(1/Lift) down.</summary>
    public double? Lift { get; set; }

    /// <summary>The range the whole factor is held in.</summary>
    public double? Least { get; set; }

    public double? Most { get; set; }

    /// <summary>Spendable gold under which no markdown touches a bot's paid work.</summary>
    public int? BrokeUnder { get; set; }

    /// <summary>How far either side of a meal hour supper is raised.</summary>
    public double? MealWindowHours { get; set; }

    /// <summary>A walk this long or longer is a journey whatever the work.</summary>
    public int? JourneyTiles { get; set; }

    /// <summary>How many hours later a night owl lives its day.</summary>
    public double? OwlShift { get; set; }

    /// <summary>The share of fighters, and of everybody else, drawn as night owls.</summary>
    public double? OwlShareFighters { get; set; }

    public double? OwlShareOthers { get; set; }

    /// <summary>The hour a bedtime is drawn from and the hours the draw spans, for day bots and for owls.</summary>
    public double? BedFrom { get; set; }

    public double? BedSpan { get; set; }

    public double? OwlBedFrom { get; set; }

    public double? OwlBedSpan { get; set; }

    /// <summary>Whether a bot at its bedtime may tire before its hours are up.</summary>
    public bool? BedtimeTires { get; set; }

    /// <summary>How long after its bed hour a bot counts as at its bedtime.</summary>
    public double? BedWindowHours { get; set; }

    /// <summary>The share of its hours a bot must have played before its bedtime may tire it.</summary>
    public double? BedPlayedShare { get; set; }

    /// <summary>How long after its wake hour a resting bot goes first under the floor.</summary>
    public double? WakeWindowHours { get; set; }
}

/// <summary>Reads the day's file, or writes one with the hours in it.</summary>
public static class BotDayConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDayConfig));

    private const string ConfigPath = "Configuration/bot-day.json";

    /// <summary>The starter carries every number written out, because the hours are what a reader opens this file for.</summary>
    private static BotDaySettings Starter() =>
        new()
        {
            Running = BotDay.Running,
            DawnAt = BotDay.DawnAt,
            MorningAt = BotDay.MorningAt,
            NoonAt = BotDay.NoonAt,
            AfternoonAt = BotDay.AfternoonAt,
            EveningAt = BotDay.EveningAt,
            NightAt = BotDay.NightAt,
            Lift = BotDay.Lift,
            Least = BotDay.Least,
            Most = BotDay.Most,
            BrokeUnder = BotDay.BrokeUnder,
            MealWindowHours = BotDay.MealWindowHours,
            JourneyTiles = BotDay.JourneyTiles,
            OwlShift = BotDay.OwlShift,
            OwlShareFighters = BotDay.OwlShareFighters,
            OwlShareOthers = BotDay.OwlShareOthers,
            BedFrom = BotDay.BedFrom,
            BedSpan = BotDay.BedSpan,
            OwlBedFrom = BotDay.OwlBedFrom,
            OwlBedSpan = BotDay.OwlBedSpan,
            BedtimeTires = BotDay.BedtimeTires,
            BedWindowHours = BotDay.BedWindowHours,
            BedPlayedShare = BotDay.BedPlayedShare,
            WakeWindowHours = BotDay.WakeWindowHours
        };

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotDaySettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, Starter());

            logger.Information("Wrote a starter day file to {Path} with the hours of the day and every dial in it", ConfigPath);

            return;
        }

        BotDay.Running = settings.Running ?? BotDay.Running;

        // The six hours together or not at all: one of them out of order would fold a part of the day into nothing.
        var dawn = BotDay.DawnAt;
        var morning = BotDay.MorningAt;
        var noon = BotDay.NoonAt;
        var afternoon = BotDay.AfternoonAt;
        var evening = BotDay.EveningAt;
        var night = BotDay.NightAt;

        BotDay.DawnAt = settings.DawnAt ?? dawn;
        BotDay.MorningAt = settings.MorningAt ?? morning;
        BotDay.NoonAt = settings.NoonAt ?? noon;
        BotDay.AfternoonAt = settings.AfternoonAt ?? afternoon;
        BotDay.EveningAt = settings.EveningAt ?? evening;
        BotDay.NightAt = settings.NightAt ?? night;

        if (!BotDay.HoursInOrder())
        {
            logger.Warning("The hours in {Path} do not stand in order inside the day (dawn < morning < noon < afternoon < evening < night, nought to twenty-four); the code's own stand", ConfigPath);

            BotDay.DawnAt = dawn;
            BotDay.MorningAt = morning;
            BotDay.NoonAt = noon;
            BotDay.AfternoonAt = afternoon;
            BotDay.EveningAt = evening;
            BotDay.NightAt = night;
        }

        BotDay.Lift = settings.Lift ?? BotDay.Lift;
        BotDay.Most = settings.Most ?? BotDay.Most;

        // A factor without a floor is a veto: whatever the file says, the least the hour may do is a tenth (review A, §3.8).
        var least = settings.Least ?? BotDay.Least;

        if (least < 0.1)
        {
            logger.Warning("Least in {Path} is {Least}, under the floor of 0.1 that keeps the hour from being a veto; 0.1 stands", ConfigPath, least);
            least = 0.1;
        }

        BotDay.Least = least;
        BotDay.BrokeUnder = settings.BrokeUnder ?? BotDay.BrokeUnder;
        BotDay.MealWindowHours = settings.MealWindowHours ?? BotDay.MealWindowHours;
        BotDay.JourneyTiles = settings.JourneyTiles ?? BotDay.JourneyTiles;
        BotDay.OwlShift = settings.OwlShift ?? BotDay.OwlShift;
        BotDay.OwlShareFighters = settings.OwlShareFighters ?? BotDay.OwlShareFighters;
        BotDay.OwlShareOthers = settings.OwlShareOthers ?? BotDay.OwlShareOthers;
        BotDay.BedFrom = settings.BedFrom ?? BotDay.BedFrom;
        BotDay.BedSpan = settings.BedSpan ?? BotDay.BedSpan;
        BotDay.OwlBedFrom = settings.OwlBedFrom ?? BotDay.OwlBedFrom;
        BotDay.OwlBedSpan = settings.OwlBedSpan ?? BotDay.OwlBedSpan;
        BotDay.BedtimeTires = settings.BedtimeTires ?? BotDay.BedtimeTires;
        BotDay.BedWindowHours = settings.BedWindowHours ?? BotDay.BedWindowHours;
        BotDay.BedPlayedShare = settings.BedPlayedShare ?? BotDay.BedPlayedShare;
        BotDay.WakeWindowHours = settings.WakeWindowHours ?? BotDay.WakeWindowHours;
    }
}
