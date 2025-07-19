using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Server.Json;

namespace Server.Mobiles;

public enum SpeedLevel
{
    None,
    VerySlow,
    Slow,
    Medium,
    Fast,
    VeryFast
}

public static class NPCSpeeds
{
    private const string _tablePath = "Data/npc-speeds.json";
    private static readonly Dictionary<Type, SpeedClassEntry> _speedsByType = new();
    private static readonly Dictionary<SpeedLevel, SpeedClassEntry> _speedsByLevel = new();

    // Time period to lock NPCs into idling
    public static int MinIdleSeconds { get; private set; }
    public static int MaxIdleSeconds { get; private set; }

    public static void GetSpeeds(BaseCreature bc, out double activeSpeed, out double passiveSpeed)
    {
        if ((bc.SpeedClass == SpeedLevel.None || !_speedsByLevel.TryGetValue(bc.SpeedClass, out var sp)) &&
            !_speedsByType.TryGetValue(bc.GetType(), out sp))
        {
            sp = _speedsByLevel[SpeedLevel.Medium];
        }

        activeSpeed = sp.ActiveSpeed;
        passiveSpeed = sp.PassiveSpeed;
    }

    public static void RegisterSpeed(SpeedClassEntry entry)
    {
        _speedsByLevel[entry.Level] = entry;

        foreach (var type in entry.Types)
        {
            _speedsByType[type] = entry;
        }
    }

    public static void Configure()
    {
        MinIdleSeconds = ServerConfiguration.GetSetting("movement.delay.npcMinIdle", 15);
        MaxIdleSeconds = ServerConfiguration.GetSetting("movement.delay.npcMaxIdle", 25);

        var path = Path.Combine(Core.BaseDirectory, _tablePath);
        if (!File.Exists(path))
        {
            return;
        }

        var speeds = JsonConfig.Deserialize<SpeedClassEntry[]>(path);

        for (var i = 0; i < speeds.Length; i++)
        {
            RegisterSpeed(speeds[i]);
        }
    }

    public record SpeedClassEntry
    {
        [JsonPropertyName("level")]
        public SpeedLevel Level { get; init; }

        [JsonPropertyName("active")]
        public double ActiveSpeed { get; init; }

        [JsonPropertyName("passive")]
        public double PassiveSpeed { get; init; }

        [JsonPropertyName("types")]
        public HashSet<Type> Types { get; init; }
    }
}
