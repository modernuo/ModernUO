using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Server.Json;

namespace Server.Mobiles;

public enum SpeedLevel
{
    None,
    Slow,
    Medium,
    Fast,
    VeryFast
}

public static class NPCSpeeds
{
    private const string _tablePath = "Data/npc-speeds.json";
    private static Dictionary<Type, SpeedClassEntry> _speedsByType = new();
    private static Dictionary<SpeedLevel, SpeedClassEntry> _speedsByLevel = new();

    // Enabled for pets on HS+
    public static bool ScaleSpeedByDex { get; private set; }
    public static double MinDelay { get; private set; }
    public static double MaxDelay { get; private set; }
    public static int MinDex { get; private set; }
    public static int MaxDex { get; private set; }

    // Time period to lock NPCs into idling
    public static int MinIdleSeconds { get; private set; }
    public static int MaxIdleSeconds { get; private set; }

    public static void GetSpeeds(BaseCreature bc, out double activeSpeed, out double passiveSpeed)
    {
        // Used for scaling pet's speed by dex in HS+
        if (bc.ScaleSpeedByDex)
        {
            var maxDex = MaxDex;
            double min = MinDelay;
            double max = MaxDelay;
            var dex = Math.Clamp(bc.Dex, MinDex, maxDex);

            activeSpeed = Math.Max(max - (max - min) * ((double)dex / maxDex), min);
            passiveSpeed = activeSpeed * 2;
            return;
        }

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
        ScaleSpeedByDex = ServerConfiguration.GetSetting("movement.delay.scaleSpeedByDex", Core.HS);
        MinDelay = ServerConfiguration.GetSetting("movement.delay.npcMinDelay", 0.1);
        MaxDelay = ServerConfiguration.GetSetting("movement.delay.npcMaxDelay", 0.4);
        MaxDex = ServerConfiguration.GetSetting("movement.delay.npcMinDex", 50);
        MaxDex = ServerConfiguration.GetSetting("movement.delay.npcMaxDex", 200);
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
