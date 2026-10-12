using Server.BotAI.V2;
using Server.Logging;

namespace Server.BotAI.Mind;

/// <summary>
/// The debugger's way in, and the whole of its contact with the rest of the shard.
///
/// <para>
/// The server finds this by reflection — every public static parameterless <c>Configure</c> in every loaded
/// assembly is called — so one class is all the wiring there is. It registers a module and does nothing
/// else, exactly as <see cref="BotMindCore"/> does for the thinking bots. The two are switched on and off
/// separately: <c>bots.debugger.enabled</c> takes the debugger away and leaves the minds, and taking
/// <c>BotMindAI.dll</c> out of <c>assemblies.json</c> takes both.
/// </para>
///
/// <para>
/// <b>Off by default is deliberately not the setting.</b> A watcher that has to be remembered is a watcher
/// that is off on the night something goes wrong. It costs one pass over the roll every two seconds and one
/// question every two minutes, and both are cheaper than the evening it saves.
/// </para>
/// </summary>
public static class BotDebugCore
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDebugCore));

    /// <summary>Its own switch, beside the minds' and the bot system's. Written back on first boot.</summary>
    public static bool Enabled { get; private set; }

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("bots.debugger.enabled", true);

        if (!Enabled)
        {
            return;
        }

        // Nothing to watch if the population is switched off. Said rather than assumed: a module registered
        // into a bot system that is not running waits for a world phase that never comes, and "the debugger
        // never said anything" is not a diagnosis.
        if (!BotCore.Enabled)
        {
            Enabled = false;

            logger.Information("The debugger is switched on but the bot system is not, so there is nobody to watch");

            return;
        }

        BotModules.Register(new BotDebugModule());

        // The championship's record and the last places of the watchers' events, kept by the world save itself: a file of their
        // own under Saves/ went into the backups at every save, and every boot forgot the champion (02.10.2026). See BotTourneyStore.
        BotTourneyStore.Configure();
    }

    public static void Initialize()
    {
        logger.Information(
            Enabled
                ? "The debugger is loaded: one invisible watcher, no work, no wage and no opinions about anybody's trade"
                : "The debugger is loaded but switched off; nothing is watching the population"
        );
    }
}
