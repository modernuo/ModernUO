using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-web.json</c> may say. Everything optional; an empty file keeps the code's numbers.
/// Keys are PascalCase and a wrong key is silent — the boot line is the proof of what took.
/// </summary>
public sealed class BotWebSettings
{
    /// <summary>Whether the web dashboard listens at all.</summary>
    public bool? Enabled { get; set; }

    /// <summary>The address to listen on. Only the machine itself unless you know what you are doing.</summary>
    public string Bind { get; set; }

    public int? Port { get; set; }

    /// <summary>How often the whole snapshot is rebuilt, in milliseconds.</summary>
    public int? SnapshotMs { get; set; }

    /// <summary>How often the paths and craft pages are rebuilt.</summary>
    public int? PagesMs { get; set; }

    /// <summary>How often a sample is added to the history, and how many samples are kept.</summary>
    public int? HistoryMs { get; set; }

    public int? KeepHistory { get; set; }

    /// <summary>How many events, failed searches and alarm lines are kept in memory for the page.</summary>
    public int? KeepEvents { get; set; }

    public int? KeepPaths { get; set; }

    /// <summary>Whether a picture of the map is drawn from the client's files for the page to draw the bots on.</summary>
    public bool? MapImage { get; set; }

    /// <summary>Tiles per pixel in that picture. Four gives 1536 by 1024 for Felucca.</summary>
    public int? MapScale { get; set; }

    /// <summary>Where the page and the map picture live, under the Distribution folder.</summary>
    public string Folder { get; set; }

    /// <summary>Whether the live window (a frame a second for a watched bot, and the debuggers' page) is built at all.</summary>
    public bool? Live { get; set; }

    /// <summary>Whether the live window also draws the bot's surroundings in the client's own pictures.</summary>
    public bool? LivePicture { get; set; }
}

public static class BotWebConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWebConfig));

    private const string ConfigPath = "Configuration/bot-web.json";

    public static bool Enabled { get; private set; } = true;

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotWebSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotWebSettings { Enabled = true, Bind = "127.0.0.1", Port = 2599 });

            logger.Information("Wrote a starter web file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        Enabled = settings.Enabled ?? Enabled;
        BotWebServer.Bind = string.IsNullOrWhiteSpace(settings.Bind) ? BotWebServer.Bind : settings.Bind.Trim();
        BotWebServer.Port = settings.Port ?? BotWebServer.Port;
        BotWebServer.Folder = string.IsNullOrWhiteSpace(settings.Folder) ? BotWebServer.Folder : settings.Folder.Trim();
        BotWebSnapshot.SnapshotMs = settings.SnapshotMs ?? BotWebSnapshot.SnapshotMs;
        BotWebSnapshot.PagesMs = settings.PagesMs ?? BotWebSnapshot.PagesMs;
        BotWebHistory.EveryMs = settings.HistoryMs ?? BotWebHistory.EveryMs;
        BotWebHistory.Keep = settings.KeepHistory ?? BotWebHistory.Keep;
        BotEvents.Keep = settings.KeepEvents ?? BotEvents.Keep;
        BotEvents.KeepPaths = settings.KeepPaths ?? BotEvents.KeepPaths;
        BotWebMap.Enabled = settings.MapImage ?? BotWebMap.Enabled;
        BotWebMap.Scale = settings.MapScale ?? BotWebMap.Scale;
        BotWebLive.Enabled = settings.Live ?? BotWebLive.Enabled;
        BotWebLive.Picture = settings.LivePicture ?? BotWebLive.Picture;
    }
}
