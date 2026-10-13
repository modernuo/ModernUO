using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-mend.json</c> is allowed to say. Everything optional.
///
/// <para>
/// What is not here: how much a heal heals, what it costs in mana, how long a bandage takes. All of that is the
/// engine's, and a file able to disagree with it would be a file able to promise a bot a rescue that does not
/// arrive.
/// </para>
/// </summary>
public sealed class BotMendSettings
{
    /// <summary>How hurt something has to be before it is worth mending.</summary>
    public double? Hurt { get; set; }

    /// <summary>How much of its health a bot is mended to before the job is done.</summary>
    public double? Mended { get; set; }

    /// <summary>How near a heal is cast from. The bandage's reach is the engine's and not ours to move.</summary>
    public int? Cast { get; set; }

    /// <summary>How long after a blow a bot still counts as being under fire.</summary>
    public int? UnderFireMs { get; set; }

    /// <summary>The share of health below which a bot swallows a bottle instead of mending properly.</summary>
    public double? Gulp { get; set; }

    /// <summary>How much more urgent mending is at death's door than at the threshold.</summary>
    public double? Urgency { get; set; }

    /// <summary>How far a caster looks for somebody else worth healing.</summary>
    public int? Watch { get; set; }

    /// <summary>Whether the nearest of ours able to mend a bot under its failing line is pressed to it rather than told to look up (12.10.2026, w5).</summary>
    public bool? SurgeonPresses { get; set; }

    /// <summary>How often another attempt is made.</summary>
    public int? TryMs { get; set; }

    /// <summary>What mending is reckoned at per minute before experience corrects it.</summary>
    public double? Expects { get; set; }

    /// <summary>How long a patch-up is expected to take.</summary>
    public double? WorkMinutes { get; set; }

    /// <summary>How far a fleeing bot looks for what it is running from, and how far counts as away.</summary>
    public int? FleeWatch { get; set; }

    /// <summary>How far a fleeing bot heads in one go.</summary>
    public int? FleeBound { get; set; }

    /// <summary>How long a flight may go on before it is given up as not working, in milliseconds.</summary>
    public int? FleeGiveUpMs { get; set; }

    /// <summary>How much of the strength a hurt bot has left the opposition may come to before it runs.</summary>
    public double? FleeBearable { get; set; }

    /// <summary>What getting away is reckoned at per minute. See <see cref="BotBolt.Prior"/> before moving it.</summary>
    public double? FleeExpects { get; set; }

    /// <summary>Whether a beaten bot runs for a guarded town when that is a realistic escape (02.10.2026). See <see cref="BotHaven"/>.</summary>
    public bool? FleeToTown { get; set; }

    /// <summary>How far, straight, a guarded town may be for a run to be weighed.</summary>
    public int? FleeTownReach { get; set; }

    /// <summary>The most seconds of running a town may be off at the runner's own pace.</summary>
    public int? FleeTownSeconds { get; set; }

    /// <summary>A town this near is run for whatever the pursuer's pace.</summary>
    public int? FleeTownNear { get; set; }

    /// <summary>A running bot's step afoot and mounted, in milliseconds, for pricing the run to a town (12.10.2026, w4).</summary>
    public int? FleeFootStepMs { get; set; }

    public int? FleeMountStepMs { get; set; }

    /// <summary>How long a run for a town may go on with the thing still on the bot, in milliseconds.</summary>
    public int? FleeTownMs { get; set; }

    /// <summary>Whether a bot on a run for a town, or outmatched and not yet having struck its attacker, holds its swing. See <see cref="BotBolt.HoldsSwing"/>.</summary>
    public bool? FleeHoldsSwing { get; set; }
}

/// <summary>Reads the mending file and moves the numbers it names.</summary>
public static class BotMendConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMendConfig));

    private const string ConfigPath = "Configuration/bot-mend.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotMendSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotMendSettings());

            logger.Information(
                "Wrote a starter mending file to {Path}; every number stays as the code has it",
                ConfigPath
            );

            return;
        }

        BotMend.Hurt = settings.Hurt ?? BotMend.Hurt;
        BotMend.Mended = settings.Mended ?? BotMend.Mended;
        BotMend.Cast = settings.Cast ?? BotMend.Cast;
        BotMend.UnderFireMs = settings.UnderFireMs ?? BotMend.UnderFireMs;
        BotMend.Gulp = settings.Gulp ?? BotMend.Gulp;

        BotSalve.Urgency = settings.Urgency ?? BotSalve.Urgency;

        BotSalve.TryMs = settings.TryMs ?? BotSalve.TryMs;
        BotSalve.Prior = settings.Expects ?? BotSalve.Prior;
        BotSalve.WorkMinutes = settings.WorkMinutes ?? BotSalve.WorkMinutes;

        BotSurgeon.Reach = settings.Watch ?? BotSurgeon.Reach;
        BotSurgeon.Presses = settings.SurgeonPresses ?? BotSurgeon.Presses;

        // The other answer this rung gives. Kept in the mending file because it is the same question —
        // a bot that is losing, and what it should do about it.
        BotBolt.Watch = settings.FleeWatch ?? BotBolt.Watch;
        BotBolt.Bound = settings.FleeBound ?? BotBolt.Bound;
        BotBolt.GiveUpMs = settings.FleeGiveUpMs ?? BotBolt.GiveUpMs;
        BotBolt.Prior = settings.FleeExpects ?? BotBolt.Prior;

        BotFugitive.Bearable = settings.FleeBearable ?? BotFugitive.Bearable;

        // The run for a town (02.10.2026). See BotHaven.
        BotHaven.Running = settings.FleeToTown ?? BotHaven.Running;
        BotHaven.Reach = settings.FleeTownReach ?? BotHaven.Reach;
        BotHaven.MostSeconds = settings.FleeTownSeconds ?? BotHaven.MostSeconds;
        BotHaven.NearTiles = settings.FleeTownNear ?? BotHaven.NearTiles;
        BotHaven.FootStepMs = settings.FleeFootStepMs ?? BotHaven.FootStepMs;
        BotHaven.MountStepMs = settings.FleeMountStepMs ?? BotHaven.MountStepMs;
        BotBolt.HavenMs = settings.FleeTownMs ?? BotBolt.HavenMs;
        BotBolt.HoldsSwing = settings.FleeHoldsSwing ?? BotBolt.HoldsSwing;
    }
}
