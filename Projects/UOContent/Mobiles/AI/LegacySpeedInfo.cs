using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Server.Json;
using Server.Logging;

namespace Server.Mobiles.AI;

public class LegacySpeedInfo
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(LegacySpeedInfo));

    public static bool Enabled { get; private set; }

    private static SpeedInfo[] _legacySpeeds;
    private static Dictionary<Type, SpeedInfo> _legacySpeedsByType;

    private record SpeedInfo(string Name, double ActiveSpeed, double PassiveSpeed);

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("movement.delay.useLegacySpeeds", false);

        if (Enabled)
        {
            var legacySpeeds = Path.Combine(Core.BaseDirectory, "Data/legacy-speed-info.json");
            if (File.Exists(legacySpeeds))
            {
                var speeds = JsonConfig.Deserialize<SpeedInfoSetting[]>(legacySpeeds);
                _legacySpeeds = new SpeedInfo[speeds.Length];
                _legacySpeedsByType ??= new Dictionary<Type, SpeedInfo>();

                for (var i = 0; i < speeds.Length; i++)
                {
                    var speedSetting = speeds[i];
                    var speed = _legacySpeeds[i] = new SpeedInfo(speedSetting.Name, speedSetting.ActiveSpeed, speedSetting.PassiveSpeed);
                    foreach (var type in speedSetting.Types)
                    {
                        RegisterTypeToSpeedLevel(type, speed);
                    }
                }
            }
        }
    }

    public static void RegisterLegacySpeed<T>(string speedLevel)
    {
        var type = typeof(T);
        if (!Enabled)
        {
            logger.Warning($"Cannot register {type} for legacy speed because it is disabled.");
            return;
        }

        if (_legacySpeeds?.Length > 0)
        {
            foreach (var speed in _legacySpeeds)
            {
                if (speed.Name.InsensitiveEquals(speedLevel))
                {
                    RegisterTypeToSpeedLevel(type, speed);
                    return;
                }
            }
        }

        logger.Warning($"Cannot find speed level {speedLevel} to register {type}.");
    }

    public static bool GetSpeedByType(Type type, out double activeSpeed, out double passiveSpeed)
    {
        if (Enabled && _legacySpeedsByType.TryGetValue(type, out var speedInfo))
        {
            activeSpeed = speedInfo.ActiveSpeed;
            passiveSpeed = speedInfo.PassiveSpeed;
            return true;
        }

        // Propagate the negative values used to indicate that we never set the speeds
        activeSpeed = -1;
        passiveSpeed = -1;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RegisterTypeToSpeedLevel(Type type, SpeedInfo speed)
    {
        _legacySpeedsByType ??= new Dictionary<Type, SpeedInfo>();
        _legacySpeedsByType[type] = speed;
    }

    internal class SpeedInfoSetting
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("activeSpeed")]
        public double ActiveSpeed { get; set; }

        [JsonPropertyName("passiveSpeed")]
        public double PassiveSpeed { get; set; }

        [JsonPropertyName("types")]
        public Type[] Types { get; set; }
    }
}
