using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Server.Logging;

namespace Server.Systems.Gateway;

public static class GatewayClient
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GatewayClient));

    // REST client (always available as fallback)
    private static HttpClient _httpClient;

    // SignalR client (optional, preferred when connected)
    private static HubConnection _hubConnection;

    public static bool IsReady => _httpClient != null;
    public static bool IsSignalRConnected => _hubConnection?.State == HubConnectionState.Connected;

    public static void Configure()
    {
        if (!GatewayConfig.Enabled)
        {
            return;
        }

        // REST client (always configured)
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(GatewayConfig.GatewayUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(10)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"ApiKey {GatewayConfig.ApiKey}");

        // SignalR client (optional)
        if (GatewayConfig.SignalREnabled)
        {
            var hubUrl = $"{GatewayConfig.GatewayUrl.TrimEnd('/')}/hubs/gameserver?apiKey={GatewayConfig.ApiKey}";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30)
                })
                .Build();

            // Register handlers -- all marshal to game thread
            _hubConnection.On<PushedSession>("PushSession", session =>
            {
                Core.LoopContext.Post(() => GatewaySessionStore.Add(session), EventLoopContext.Priority.High);
            });

            _hubConnection.On<AdminCommandData>("AdminCommand", command =>
            {
                Core.LoopContext.Post(() => GatewayAdminHandler.HandleCommand(command));
            });

            _hubConnection.On("Ping", () =>
            {
                // No-op, connection keep-alive handled by SignalR internally
            });

            _hubConnection.Reconnected += connectionId =>
            {
                logger.Information("SignalR reconnected to gateway (ConnectionId: {Id})", connectionId);
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                logger.Warning("SignalR connection to gateway closed: {Message}", error?.Message ?? "clean disconnect");
                return Task.CompletedTask;
            };

            // Connect after server starts (via EventSink.ServerStarted)
        }
    }

    /// <summary>
    /// Connects the SignalR hub. Called after server is fully loaded.
    /// </summary>
    public static async Task ConnectSignalRAsync()
    {
        if (_hubConnection == null)
        {
            return;
        }

        try
        {
            await _hubConnection.StartAsync();
            logger.Information("SignalR connected to gateway at {Url}", GatewayConfig.GatewayUrl);
        }
        catch (Exception ex)
        {
            logger.Warning("SignalR connection to gateway failed: {Message}. Will retry automatically.", ex.Message);
        }
    }

    // --- SignalR methods ---

    public static Task SendHeartbeatAsync(HeartbeatRequest data) =>
        _hubConnection?.InvokeAsync("Heartbeat", new { data.PlayerCount, data.MaxPlayers, data.IsOnline })
        ?? Task.CompletedTask;

    public static async Task<GameLoginResponse?> ValidateSessionAsync(int authId)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            return null;
        }

        try
        {
            var result = await _hubConnection.InvokeAsync<SessionValidationResult>("ValidateSession", authId);
            if (result == null)
            {
                return null;
            }

            return new GameLoginResponse(result.Valid, result.Reason, result.GameAccountId, result.AccessLevel);
        }
        catch (Exception ex)
        {
            logger.Warning("SignalR ValidateSession failed: {Message}", ex.Message);
            return null;
        }
    }

    // --- REST methods (fallback) ---

    public static Task<HttpResponseMessage> PostAsJsonAsync<T>(string path, T content) =>
        _httpClient.PostAsJsonAsync(path, content);

    // --- Types ---

    public record HeartbeatRequest(
        [property: JsonPropertyName("playerCount")] int PlayerCount,
        [property: JsonPropertyName("maxPlayers")] int MaxPlayers,
        [property: JsonPropertyName("isOnline")] bool IsOnline
    );

    public record GameLoginRequest(
        [property: JsonPropertyName("authId")] int AuthId,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("password")] string Password
    );

    public record GameLoginResponse(
        [property: JsonPropertyName("accepted")] bool Accepted,
        [property: JsonPropertyName("reason")] string? Reason,
        [property: JsonPropertyName("accountId")] Guid? AccountId,
        [property: JsonPropertyName("accessLevel")] string? AccessLevel
    );

    public record AdminCommandData(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("payload")] string Payload
    );

    public record SessionValidationResult(
        [property: JsonPropertyName("valid")] bool Valid,
        [property: JsonPropertyName("gameAccountId")] Guid? GameAccountId,
        [property: JsonPropertyName("accessLevel")] string? AccessLevel,
        [property: JsonPropertyName("reason")] string? Reason
    );
}
