using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Server.Json;

namespace Server.Mobiles;

public enum SpeedLevel
{
    None, // no bucket: the creature's own speeds are authoritative (custom)
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

    // Construction-time resolution of a type's bucket: an explicit DefaultSpeedClass,
    // else the table's type list, else Medium so unconfigured creatures never construct
    // at 0/0. None only when the table itself is unloaded (test fixtures).
    public static SpeedLevel ResolveDefaultLevel(BaseCreature bc)
    {
        if (bc.DefaultSpeedClass != SpeedLevel.None)
        {
            return bc.DefaultSpeedClass;
        }

        if (_speedsByType.TryGetValue(bc.GetType(), out var sp))
        {
            return sp.Level;
        }

        return _speedsByLevel.ContainsKey(SpeedLevel.Medium) ? SpeedLevel.Medium : SpeedLevel.None;
    }

    // Null for None (custom) or an unloaded table. Creatures cache the result — the
    // table is immutable after Configure.
    public static SpeedClassEntry FindEntry(SpeedLevel level) =>
        level == SpeedLevel.None ? null : _speedsByLevel.GetValueOrDefault(level);

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

        // Movement clock (seconds per step); absent/0 = inherit the matching think value.
        [JsonPropertyName("activeMove")]
        public double ActiveMoveSpeed { get; init; }

        [JsonPropertyName("passiveMove")]
        public double PassiveMoveSpeed { get; init; }

        [JsonPropertyName("types")]
        public HashSet<Type> Types { get; init; }
    }
}
