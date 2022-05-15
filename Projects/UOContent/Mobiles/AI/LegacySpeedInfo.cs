using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Server.Json;
using Server.Logging;

namespace Server;

public class LegacySpeedInfo
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(LegacySpeedInfo));

    private const string _tablePath = "Data/npc-speeds.json";
    private static Dictionary<Type, LegacySpeedEntry> m_Table;

    public static bool Enabled { get; private set; }

    public static void GetSpeeds(Type type, out double activeSpeed, out double passiveSpeed)
    {
        if (!m_Table.TryGetValue(type, out var sp))
        {
            // "Fast"
            activeSpeed = 0.2;
            passiveSpeed = 0.4;
            return;
        }

        activeSpeed = sp.ActiveSpeed;
        passiveSpeed = sp.PassiveSpeed;
    }

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetSetting("movement.delay.useLegacySpeeds", !Core.HS);

        if (!Enabled)
        {
            return;
        }

        var path = Path.Combine(Core.BaseDirectory, _tablePath);
        if (!File.Exists(path))
        {
            logger.Warning($"Cannot find {path}. Disabling legacy speed system.");
            Enabled = false;
            return;
        }

        var speeds = JsonConfig.Deserialize<LegacySpeedEntry[]>(path);

        m_Table = new Dictionary<Type, LegacySpeedEntry>();

        for (var i = 0; i < speeds.Length; ++i)
        {
            var info = speeds[i];

            foreach (var type in info.Types)
            {
                m_Table[type] = info;
            }
        }
    }

    public record LegacySpeedEntry
    {
        [JsonPropertyName("activeSpeed")]
        public double ActiveSpeed { get; init; }

        [JsonPropertyName("passiveSpeed")]
        public double PassiveSpeed { get; init; }

        [JsonPropertyName("types")]
        public HashSet<Type> Types { get; init; }
    }
}
