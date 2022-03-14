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

    public static bool GetSpeeds(Type type, out double activeSpeed, out double passiveSpeed)
    {
        if (!(Enabled && m_Table.TryGetValue(type, out var sp)))
        {
            activeSpeed = 0;
            passiveSpeed = 0;
            return false;
        }

        activeSpeed = sp.ActiveSpeed;
        passiveSpeed = sp.PassiveSpeed;

        return true;
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
        [JsonPropertyName("active")]
        public double ActiveSpeed { get; init; }

        [JsonPropertyName("passive")]
        public double PassiveSpeed { get; init; }

        [JsonPropertyName("types")]
        public HashSet<Type> Types { get; init; }
    }
}
