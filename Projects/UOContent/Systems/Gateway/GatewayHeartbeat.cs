using System;
using Server.Logging;
using Server.Network;

namespace Server.Systems.Gateway;

/// <summary>
/// Sends periodic heartbeats to the ModernUO Gateway so it knows this server is online.
/// Reports player count and online status.
/// Prefers SignalR when connected, falls back to REST.
/// </summary>
public static class GatewayHeartbeat
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GatewayHeartbeat));

    private static TimerExecutionToken _heartbeatTimer;

    public static void Configure()
    {
        if (!GatewayConfig.Enabled)
        {
            return;
        }

        EventSink.ServerStarted += OnServerStarted;
    }

    private static void OnServerStarted()
    {
        logger.Information(
            "Gateway heartbeat started, sending every {Interval}s",
            GatewayConfig.HeartbeatIntervalSeconds);

        var interval = TimeSpan.FromSeconds(GatewayConfig.HeartbeatIntervalSeconds);
        Timer.StartTimer(interval, interval, SendHeartbeat, out _heartbeatTimer);

        // Send first heartbeat immediately
        SendHeartbeat();

        // Connect SignalR if enabled
        if (GatewayConfig.SignalREnabled)
        {
            _ = GatewayClient.ConnectSignalRAsync();
        }
    }

    private static async void SendHeartbeat()
    {
        if (!GatewayClient.IsReady)
        {
            return;
        }

        try
        {
            var playerCount = NetState.Instances.Count;
            var request = new GatewayClient.HeartbeatRequest(playerCount, GatewayConfig.MaxPlayers, true);

            if (GatewayClient.IsSignalRConnected)
            {
                await GatewayClient.SendHeartbeatAsync(request);
            }
            else
            {
                // REST fallback
                var response = await GatewayClient.PostAsJsonAsync("/api/heartbeat", request);

                if (!response.IsSuccessStatusCode)
                {
                    logger.Warning("Gateway heartbeat failed: {StatusCode}", (int)response.StatusCode);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Warning("Gateway heartbeat error: {Message}", ex.Message);
        }
    }
}
