using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.Mind;

/// <summary>
/// What <c>Configuration/bot-debugger.json</c> is allowed to say. Everything optional; an empty file means
/// "keep the numbers the code chose", which is what is written on the first boot.
///
/// <para>
/// <b>PascalCase, and it is not a style question on this shard.</b> The deserialiser matches these names as
/// written, so a key in lower case is not an error and not a warning — it is a value silently left at its
/// default, and a configuration file that appears to have been read is worse than one that fails to load.
/// </para>
///
/// <para>
/// <b>Its own file rather than a section of the minds'.</b> The two are switched on and off separately and
/// tuned for opposite reasons: the minds are tuned for how often a bot may change its mind, the debugger for
/// how much of one graphics card a watcher may spend. A single file would mean editing the thinking bots'
/// cadence to change how often the debugger writes a paragraph, which is exactly the mistake the whole
/// project's one-file-per-subsystem rule exists to prevent.
/// </para>
/// </summary>
public sealed class BotDebugSettings
{
    /// <summary>What the debugger is called, in the world and in its log.</summary>
    public string Name { get; set; }

    /// <summary>
    /// Which model it thinks with. Not the population's — see <c>BotVigil.Model</c> for why, and for what
    /// the three candidates actually did when they were measured on this question.
    /// </summary>
    public string Model { get; set; }

    /// <summary>How long its model may hold the card after answering. Keep this in seconds.</summary>
    public string KeepAlive { get; set; }

    /// <summary>How long it will wait for its own answer, cold load included.</summary>
    public int? TimeoutMs { get; set; }

    /// <summary>The hue of the robe, and of the figure inside it.</summary>
    public int? RobeHue { get; set; }

    /// <summary>The helpers raised beside the lead, by name. Patrick's order of 13.09.2026: two of them.</summary>
    public string[] Helpers { get; set; }

    /// <summary>The robes, lead first.</summary>
    public int[] Hues { get; set; }

    /// <summary>The share of each guild's spare money the crown takes every quarter hour for prizes. Nought stops it.</summary>
    public double? TaxShare { get; set; }

    /// <summary>How often the population is measured, in milliseconds.</summary>
    public int? SampleMs { get; set; }

    /// <summary>How often the debugger goes and stands beside somebody else.</summary>
    public int? HoverMs { get; set; }

    /// <summary>How often it is asked what the worst thing in front of it is.</summary>
    public int? ReportMs { get; set; }

    /// <summary>How often it is asked the expensive thinking question.</summary>
    public int? ReflectMs { get; set; }

    /// <summary>How many bots are described in full in one report.</summary>
    public int? Rows { get; set; }

    /// <summary>How long a bot may stand on a tile, while its journey wants it elsewhere, before it counts.</summary>
    public int? FrozenMs { get; set; }

    /// <summary>How long work may answer "working, here" before it counts as suspect.</summary>
    public int? ImmortalMs { get; set; }

    /// <summary>How long a bot must be watched before "it has not improved" means anything.</summary>
    public int? SettledMs { get; set; }

    /// <summary>How often every bot is asked whether it arrived, finished, and changed at all.</summary>
    public int? WindowMs { get; set; }

    /// <summary>How many stuck bots may be reminded or shaken in one window.</summary>
    public int? MostTouched { get; set; }

    /// <summary>How long a bot is left alone after being touched, so a cure has time to work or not.</summary>
    public int? RestMs { get; set; }

    /// <summary>How long the title bout waits for the champion and its challenger to be free together (02.10.2026). See BotTourney.</summary>
    public int? TitleWaitMs { get; set; }

    /// <summary>Whether the watchers choose where events are held, or hold them at the old fixed places (02.10.2026). See BotVenues.</summary>
    public bool? Venues { get; set; }

    /// <summary>Whether the championship is held at the round arenas once a day, or as the hourly town-ring knockout (07.10.2026). See BotTourney.Arena.</summary>
    public bool? Arena { get; set; }

    /// <summary>The fewest free bots the day's championship is held with (07.10.2026, Patrick: 50). See BotTourney.MinEntrants.</summary>
    public int? MinEntrants { get; set; }

    /// <summary>The local hour from which the calendar holds the day's championship (07.10.2026). See BotTourney.StartHourLocal.</summary>
    public int? StartHourLocal { get; set; }
}

/// <summary>Reads the debugger's file and moves the numbers it names.</summary>
public static class BotDebugConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDebugConfig));

    private const string ConfigPath = "Configuration/bot-debugger.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotDebugSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotDebugSettings());

            logger.Information(
                "Wrote a starter debugger file to {Path}; every number stays as the code has it",
                ConfigPath
            );

            return;
        }

        BotVigil.Name = settings.Name ?? BotVigil.Name;
        BotVigil.Model = settings.Model ?? BotVigil.Model;
        BotVigil.KeepAlive = settings.KeepAlive ?? BotVigil.KeepAlive;
        BotVigil.TimeoutMs = settings.TimeoutMs ?? BotVigil.TimeoutMs;
        BotVigil.SampleMs = settings.SampleMs ?? BotVigil.SampleMs;
        BotVigil.HoverMs = settings.HoverMs ?? BotVigil.HoverMs;
        BotVigil.ReportMs = settings.ReportMs ?? BotVigil.ReportMs;
        BotVigil.ReflectMs = settings.ReflectMs ?? BotVigil.ReflectMs;
        BotVigil.Rows = settings.Rows ?? BotVigil.Rows;

        BotDebugger.RobeHue = settings.RobeHue ?? BotDebugger.RobeHue;
        BotVigil.Helpers = settings.Helpers ?? BotVigil.Helpers;
        BotVigil.Hues = settings.Hues ?? BotVigil.Hues;
        BotRevel.TaxShare = settings.TaxShare ?? BotRevel.TaxShare;

        BotWatch.FrozenMs = settings.FrozenMs ?? BotWatch.FrozenMs;
        BotWatch.ImmortalMs = settings.ImmortalMs ?? BotWatch.ImmortalMs;
        BotWatch.SettledMs = settings.SettledMs ?? BotWatch.SettledMs;

        BotAudit.WindowMs = settings.WindowMs ?? BotAudit.WindowMs;
        BotAudit.MostTouched = settings.MostTouched ?? BotAudit.MostTouched;
        BotAudit.RestMs = settings.RestMs ?? BotAudit.RestMs;

        BotTourney.TitleWaitMs = settings.TitleWaitMs ?? BotTourney.TitleWaitMs;
        BotVenues.Running = settings.Venues ?? BotVenues.Running;
        BotTourney.Arena = settings.Arena ?? BotTourney.Arena;
        BotTourney.MinEntrants = settings.MinEntrants ?? BotTourney.MinEntrants;
        BotTourney.StartHourLocal = settings.StartHourLocal ?? BotTourney.StartHourLocal;
    }
}
