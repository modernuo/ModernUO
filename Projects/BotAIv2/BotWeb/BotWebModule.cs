using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The web dashboard as a module: the event stream, the snapshot clock, the map picture and the listener.
///
/// <para>
/// Last of the modules, and it reads only: it divides and lists numbers the others keep, so it wants them
/// started. It is registered here rather than with the debugger because a shard watched by nobody should
/// still be able to be looked at from a browser.
/// </para>
/// </summary>
public sealed class BotWebModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWebModule));

    /// <summary>How often the clock looks. The snapshot, the pages and the map slices each keep their own cadence off it.</summary>
    public static int TickMs { get; set; } = 250;

    private static WebTimer _timer;

    public override string Name => "Web";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Will", "Movement"];

    public override void Start()
    {
        BotWebConfig.Load();

        // The stream is opened even when the listener is off: the file is the record, the page is a window.
        BotEvents.Open();

        if (!BotWebConfig.Enabled)
        {
            logger.Information("The web dashboard is off: bot-web.json says Enabled is false; events still go to {Path}", BotEvents.Path ?? "nowhere");

            return;
        }

        BotWebSnapshot.Start();
        BotWebServer.Start();
        BotWebMap.Prepare(BotPopulation.Home ?? Map.Felucca, BotWebServer.FolderPath);

        _timer ??= new WebTimer();
        _timer.Start();

        logger.Information(
            "The web dashboard is up at {Url}: a snapshot every {Snapshot}ms, the paths and craft pages every {Pages}ms, a minute of history kept for {Hours}h",
            BotWebServer.Url,
            BotWebSnapshot.SnapshotMs,
            BotWebSnapshot.PagesMs,
            BotWebHistory.Keep * BotWebHistory.EveryMs / 3600000
        );
    }

    public override void Reset()
    {
        BotWebHistory.Reset();
    }

    private sealed class WebTimer : Timer
    {
        public WebTimer() : base(TimeSpan.FromMilliseconds(TickMs), TimeSpan.FromMilliseconds(TickMs))
        {
        }

        protected override void OnTick() => BotWebSnapshot.Tick(Core.TickCount);
    }
}
