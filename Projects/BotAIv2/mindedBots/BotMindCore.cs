using Server.BotAI.V2;
using Server.Logging;

namespace Server.BotAI.Mind;

/// <summary>
/// The way in, and the whole of this assembly's contact with the rest of the shard.
///
/// <para>
/// The server finds this through <c>Data/assemblies.json</c> and calls <see cref="Configure"/> and
/// <see cref="Initialize"/> by reflection, exactly as it does BotAIv2 — and because that file is read in
/// order, BotAIv2 has already registered its own modules and subscribed to the world by the time this runs.
/// So one call is all that is needed: a module goes onto the same list, and everything after that is the
/// bot system's ordinary machinery.
/// </para>
///
/// <para>
/// <b>Switched off by removing one line from one file.</b> Take <c>BotMindAI.dll</c> out of
/// <c>assemblies.json</c> and nothing anywhere refers to any of this; the fifteen bots carry on with the
/// warrior and the archer among them, ordinary again. That property is worth more than any amount of
/// configuration, and it is the reason this is a separate assembly rather than a folder inside the other one.
/// </para>
/// </summary>
public static class BotMindCore
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMindCore));

    /// <summary>Master switch of its own, beside the bot system's. Written back on first boot.</summary>
    public static bool Enabled { get; private set; }

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("bots.mind.thinking", true);

        if (!Enabled)
        {
            return;
        }

        // Nothing to think for if the population is switched off. Said rather than assumed: a module
        // registered into a bot system that is not running would wait for a world phase that never comes,
        // and "the minds never said anything" is not a diagnosis.
        if (!BotCore.Enabled)
        {
            Enabled = false;

            logger.Information("Thinking bots are switched on but the bot system is not, so nothing is registered");

            return;
        }

        BotModules.Register(new BotMindModule());
    }

    public static void Initialize()
    {
        if (!Enabled)
        {
            logger.Information("BotMindAI loaded (disabled): the fifteen carry on without anybody thinking about it");

            return;
        }

        logger.Information("BotMindAI loaded (enabled): two of the population will be thought about, the rest are unchanged");
    }
}
