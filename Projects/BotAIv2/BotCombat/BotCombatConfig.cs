using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-combat.json</c> is allowed to say about the cry, the stand and the stopwatch. Everything optional.
///
/// <para>
/// <b>Written on 12.10.2026 (w5), because every number in <c>BotCombat/</c> was code only.</b> The cry's reach, how long it
/// holds, who is called and the rung's default had to be rebuilt to be moved; the rules added that day (the steady cry's near
/// call, the stand for want of a flight, the stopwatch's sample of unanswered cries) are the ones most likely to want turning
/// on a running shard, so they are bound here with the dials that govern them. Keys are PascalCase, as everywhere: a key in
/// the wrong case is silently ignored, and the only proof of a live value is the line the hunt module writes at boot.
/// </para>
/// </summary>
public sealed class BotCombatSettings
{
    /// <summary>Whether a steady cry (the crier whole and able) sends its nearest free neighbour at once. See <c>BotCry.NearCalls</c>.</summary>
    public bool? NearCalls { get; set; }

    /// <summary>How far a steady cry calls its nearest free neighbour from.</summary>
    public int? NearReach { get; set; }

    /// <summary>The least the thing on a steady crier must come to, as a share of the crier's own strength, to call a neighbour.</summary>
    public double? NearOdds { get; set; }

    /// <summary>The most neighbours a steady cry sends.</summary>
    public int? MostNear { get; set; }

    /// <summary>Whether a bot refused the fight for the odds and offered no flight takes the stand. See <c>BotDefender.Stands</c>.</summary>
    public bool? Stands { get; set; }

    /// <summary>How near what is on a failing bot must stand for a stand on that rung: on top of it.</summary>
    public int? StandReach { get; set; }

    /// <summary>Whether a dire cry sends the best placed of ours near it at once. See <c>BotCry.Summons</c>.</summary>
    public bool? Summons { get; set; }

    /// <summary>The share of its health below which a crier is in real trouble.</summary>
    public double? DireAt { get; set; }

    /// <summary>The most of ours sent to one dire cry at one calling.</summary>
    public int? MostCalled { get; set; }

    /// <summary>How long a bot called to one cry is left alone by the next, in milliseconds.</summary>
    public int? HelperRestMs { get; set; }

    /// <summary>How far a cry carries, in tiles.</summary>
    public int? Carries { get; set; }

    /// <summary>How long a cry stands unrenewed before it lapses, in milliseconds. See <c>BotCry.HoldsMs</c>.</summary>
    public int? CryHoldsMs { get; set; }

    /// <summary>Whether the reflex stopwatch runs. See <c>BotReflex.On</c>.</summary>
    public bool? ReflexOn { get; set; }

    /// <summary>One unanswered cry with somebody in earshot in this many is written to the log with who was near. Nought: none.</summary>
    public int? CrySampleEvery { get; set; }
}

/// <summary>Reads the combat file and moves the numbers it names.</summary>
public static class BotCombatConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCombatConfig));

    private const string ConfigPath = "Configuration/bot-combat.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotCombatSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotCombatSettings());

            logger.Information(
                "Wrote a starter combat file to {Path}; every number stays as the code has it",
                ConfigPath
            );

            return;
        }

        BotCry.NearCalls = settings.NearCalls ?? BotCry.NearCalls;
        BotCry.NearReach = settings.NearReach ?? BotCry.NearReach;
        BotCry.NearOdds = settings.NearOdds ?? BotCry.NearOdds;
        BotCry.MostNear = settings.MostNear ?? BotCry.MostNear;

        BotDefender.Stands = settings.Stands ?? BotDefender.Stands;
        BotDefender.StandReach = settings.StandReach ?? BotDefender.StandReach;

        BotCry.Summons = settings.Summons ?? BotCry.Summons;
        BotCry.DireAt = settings.DireAt ?? BotCry.DireAt;
        BotCry.MostCalled = settings.MostCalled ?? BotCry.MostCalled;
        BotCry.HelperRestMs = settings.HelperRestMs ?? BotCry.HelperRestMs;
        BotCry.Carries = settings.Carries ?? BotCry.Carries;
        BotCry.HoldsMs = settings.CryHoldsMs ?? BotCry.HoldsMs;

        BotReflex.On = settings.ReflexOn ?? BotReflex.On;
        BotReflex.SampleEvery = settings.CrySampleEvery ?? BotReflex.SampleEvery;
    }
}
