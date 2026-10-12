using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-classes.json</c> is allowed to say.
///
/// One file per subsystem rather than one file for the whole population. The first version put every
/// knob on the shard into a single <c>bots.json</c>, which meant a balance pass on the archer's aim
/// and a change to how many bots exist were edits to the same object — and a syntax error in either
/// took the whole population down.
/// </summary>
public sealed class BotClassSettings
{
    /// <summary>Overrides by class name. Anything absent keeps the number the code chose.</summary>
    public Dictionary<string, BotClassOverride> Classes { get; set; } = [];
}

/// <summary>
/// Reads the class file and hands it to <see cref="BotClasses"/>.
///
/// The class layer owns its own configuration — loading it is not the entry point's business beyond
/// saying when. That is the shape every subsystem here follows: a folder, its classes, and the file it
/// reads.
/// </summary>
public static class BotClassConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotClassConfig));

    private const string ConfigPath = "Configuration/bot-classes.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);

        var settings = JsonConfig.Deserialize<BotClassSettings>(path);

        if (settings == null)
        {
            WriteStarter(path);
            return;
        }

        BotClasses.Override(settings.Classes);
    }

    /// <summary>
    /// Writes a file listing the nine class names with nothing set under any of them.
    ///
    /// Discoverable without being a second source of truth, and the distinction matters. A starter file
    /// restating every default would look authoritative, drift from the code the first time somebody
    /// edited a class, and then silently win — so the file that ships states only which names exist and
    /// leaves every number where the code put it.
    /// </summary>
    private static void WriteStarter(string path)
    {
        var settings = new BotClassSettings();
        var classes = BotClasses.All;

        for (var i = 0; i < classes.Count; i++)
        {
            settings.Classes[classes[i].Name] = new BotClassOverride();
        }

        JsonConfig.Serialize(path, settings);

        logger.Information(
            "Wrote a starter class file naming {Count} classes to {Path}; every number stays as the code has it",
            classes.Count,
            ConfigPath
        );
    }
}
