using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The daily cycle as a module: reads <c>bot-day.json</c>, reads the clock every two seconds, writes the <c>Day:</c> line every
/// five minutes and says at boot what the hours are and how the timetables fall. See <see cref="BotDay"/> and
/// <see cref="BotTimetable"/>.
///
/// <para>
/// <b>Registers itself</b> from its own <c>Configure</c>, after <c>BotCore.Configure</c> (priority 42 against 40), as the
/// character module does: several agents build modules in the same week and one line each in the list in <c>BotCore</c> would be
/// as many patches colliding in one hunk. A World-phase module needs nothing from the Settings phase, so the order is harmless.
/// </para>
/// </summary>
public sealed class BotDayModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDayModule));

    /// <summary>The clock: the part of the day is read on it. Two seconds.</summary>
    public static int TickMs { get; set; } = 2000;

    private static DayTimer _timer;

    private static long _summaryTick;

    private static long _refreshTick;

    [CallPriority(42)]
    public static void Configure()
    {
        if (BotCore.Enabled)
        {
            BotModules.Register(new BotDayModule());
        }
    }

    public override string Name => "Day";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Will"];

    public override void Start()
    {
        BotDayConfig.Load();
        BotDay.Refresh(Core.Now);

        // Every living bot's timetable drawn now rather than at its first offer, so the boot line can say how they fall.
        var bots = BotPopulation.Bots;
        var drawn = 0;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && BotTimetables.Find(bot.Name) is not { Drawn: true } && BotTimetables.Of(bot) != null)
            {
                drawn++;
            }
        }

        var now = Core.TickCount;

        _summaryTick = now;
        _refreshTick = now;

        _timer?.Stop();
        _timer = new DayTimer(TimeSpan.FromMilliseconds(Math.Max(250, TickMs)));
        _timer.Start();

        logger.Information(
            "The daily cycle is {State}: {Schedule}, local time; the hour leans a price ×{Drop:F2}–×{Lift:F2} (held in ×{Least:F2}–×{Most:F2}), never a markdown on paid work for a bot under {Broke}gp or in need; "
            + "supper raised within {Meal:0.0}h of a meal hour; a walk of {Journey} tiles or more is a journey; night owls live {Shift:0}h later ({Fighters:P0} of fighters, {Others:P0} of the rest); bedtimes drawn from {BedFrom:0}h over {BedSpan:0}h (owls from {OwlBedFrom:0}h over {OwlBedSpan:0}h); "
            + "bedtime tires {Bedtime} ({Window:0.0}h window, after {Played:P0} of the hours), the wake hour counts for {Wake:0.0}h; {Drawn} timetables drawn just now. Now: {Now}",
            BotDay.Running ? "ON" : "OFF",
            BotDay.Schedule(),
            1.0 / Math.Max(1.0, BotDay.Lift),
            BotDay.Lift,
            BotDay.Least,
            BotDay.Most,
            BotDay.BrokeUnder,
            BotDay.MealWindowHours,
            BotDay.JourneyTiles,
            BotDay.OwlShift,
            BotDay.OwlShareFighters,
            BotDay.OwlShareOthers,
            BotDay.BedFrom,
            BotDay.BedSpan,
            BotDay.OwlBedFrom,
            BotDay.OwlBedSpan,
            BotDay.BedtimeTires ? "on" : "off",
            BotDay.BedWindowHours,
            BotDay.BedPlayedShare,
            BotDay.WakeWindowHours,
            drawn,
            BotDay.Describe()
        );
    }

    /// <summary>The clock's work, every <see cref="TickMs"/>.</summary>
    public static void Tick(long now)
    {
        BotDay.Refresh(Core.Now);

        if (now - _refreshTick >= 60000)
        {
            _refreshTick = now;
            BotTimetables.Refresh();
        }

        if (now - _summaryTick >= Math.Max(60000, BotBeat.SummaryMs))
        {
            _summaryTick = now;

            logger.Information("Day: {What}", BotDay.Describe());
        }
    }

    public override void Reset()
    {
        logger.Information("Day, before the reload: {What}", BotDay.Describe());

        _timer?.Stop();
        _timer = null;

        // The timetables themselves are kept: they are what the save holds, and a reload reads the world, not the timetables.
        BotDay.Forget();
    }

    private sealed class DayTimer : Timer
    {
        public DayTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => BotDayModule.Tick(Core.TickCount);
    }
}
