using Server.Logging;

namespace Server.Systems.Gateway;

/// <summary>
/// Configuration for the ModernUO Gateway integration.
/// When enabled, game server login (0x91) is validated against the Gateway API
/// and heartbeats are sent periodically.
///
/// Settings are stored in modernuo.json under the "gateway" prefix.
/// </summary>
public static class GatewayConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GatewayConfig));

    public static bool Enabled { get; private set; }
    public static string GatewayUrl { get; private set; } = "";
    public static string ApiKey { get; private set; } = "";
    public static int HeartbeatIntervalSeconds { get; private set; }

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("gateway.enabled", false);

        if (!Enabled)
        {
            return;
        }

        GatewayUrl = ServerConfiguration.GetOrUpdateSetting("gateway.url", "http://localhost:5000");
        ApiKey = ServerConfiguration.GetOrUpdateSetting("gateway.apiKey", "");
        HeartbeatIntervalSeconds = ServerConfiguration.GetOrUpdateSetting("gateway.heartbeatIntervalSeconds", 30);

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            logger.Warning("Gateway is enabled but gateway.apiKey is empty. Heartbeats and login validation will fail.");
        }

        logger.Information("Gateway integration enabled: {Url}", GatewayUrl);
    }
}
