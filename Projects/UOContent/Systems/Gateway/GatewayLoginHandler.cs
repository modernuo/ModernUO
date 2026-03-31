using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ModernUO.CodeGeneratedEvents;
using Server.Accounting;
using Server.Engines.CharacterCreation;
using Server.Logging;
using Server.Network;
using Server.Tasks;

namespace Server.Systems.Gateway;

/// <summary>
/// Intercepts the game server login event (0x91 packet) and validates against the Gateway API
/// instead of using local account storage.
///
/// Uses a three-tier approach:
///   1. Fast path: check local pushed session store (O(1), no network) -- sessions pushed via SignalR
///   2. SignalR path: validate session over SignalR hub connection
///   3. REST path: fallback HTTP validation
///
/// When gateway.enabled is false, this handler returns immediately and the default
/// AccountHandler processes the login as normal.
/// </summary>
public static class GatewayLoginHandler
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GatewayLoginHandler));

    [OnEvent(nameof(GameServer.GameServerLoginEvent))]
    public static void OnGameServerLogin(GameServer.GameLoginEventArgs e)
    {
        if (!GatewayConfig.Enabled || !GatewayClient.IsReady)
        {
            return;
        }

        var state = e.State;
        var username = e.Username;
        var authId = state.AuthId;

        // Fast path: check local pushed session store (O(1), no network)
        if (GatewaySessionStore.TryConsume(authId, out var session))
        {
            logger.Information("Login: {NetState} Account '{Username}' accepted via pushed session", state, username);
            AcceptLogin(e, state, username, session.AccessLevel);
            return;
        }

        // Slow path: network validation (SignalR or REST)
        var password = e.Password;

        // Defer the login -- tells the packet handler to skip both accept and reject paths.
        e.Deferred = true;

        // HTTP call on background thread, result posted back to game thread.
        Task.Run(() => ValidateViaNetworkAsync(authId, username, password))
            .ContinueWithOnGameThread(t => OnValidationComplete(t, state, username));
    }

    private static async Task<GatewayClient.GameLoginResponse> ValidateViaNetworkAsync(
        int authId,
        string username,
        string password)
    {
        // Try SignalR first
        if (GatewayClient.IsSignalRConnected)
        {
            var signalRResult = await GatewayClient.ValidateSessionAsync(authId);
            if (signalRResult != null)
            {
                return signalRResult;
            }
        }

        // Fall back to REST
        var request = new GatewayClient.GameLoginRequest(authId, username, password);
        var httpResponse = await GatewayClient.PostAsJsonAsync("/api/validate-game-login", request);
        httpResponse.EnsureSuccessStatusCode();
        return await httpResponse.Content.ReadFromJsonAsync<GatewayClient.GameLoginResponse>();
    }

    /// <summary>
    /// Runs on the game thread after the background network call completes.
    /// </summary>
    private static void OnValidationComplete(Task<GatewayClient.GameLoginResponse> task, NetState state, string username)
    {
        if (!state.Running)
        {
            return; // Client disconnected while we were validating
        }

        if (task.IsFaulted)
        {
            logger.Error(task.Exception, "Gateway login validation failed for '{Username}'", username);
            state.Disconnect($"Gateway validation error.");
            return;
        }

        var response = task.Result;

        if (response is not { Accepted: true })
        {
            logger.Information(
                "Login: {NetState} Gateway rejected '{Username}': {Reason}",
                state, username, response?.Reason ?? "unknown");
            state.Disconnect($"Gateway rejected login: {response?.Reason ?? "unknown"}");
            return;
        }

        // Gateway accepted -- use common accept logic (deferred path)
        logger.Information("Login: {NetState} Account '{Username}' at character list (via Gateway)", state, username);
        AcceptLoginDeferred(state, username, response.AccessLevel);
    }

    /// <summary>
    /// Handles login acceptance for the fast path (non-deferred, from pushed session store).
    /// Sets event args so the packet handler sends the response.
    /// </summary>
    private static void AcceptLogin(GameServer.GameLoginEventArgs e, NetState state, string username, string accessLevel)
    {
        var acct = FindOrCreateAccount(state, username, accessLevel);

        if (acct.Banned)
        {
            logger.Information("Login: {NetState} Locally banned account '{Username}'", state, username);
            e.Accepted = false;
            return;
        }

        acct.LogAccess(state);

        state.Account = acct;
        state.CityInfo = CharacterCreation.GetStartingCities();
        e.Accepted = true;
        e.CityInfo = CharacterCreation.GetStartingCities();
    }

    /// <summary>
    /// Handles login acceptance for the deferred path (after async network validation).
    /// Sends packets directly since the original packet handler has already returned.
    /// </summary>
    private static void AcceptLoginDeferred(NetState state, string username, string accessLevel)
    {
        var acct = FindOrCreateAccount(state, username, accessLevel);

        if (acct.Banned)
        {
            logger.Information("Login: {NetState} Locally banned account '{Username}'", state, username);
            state.Disconnect("Account is locally banned.");
            return;
        }

        acct.LogAccess(state);

        state.Account = acct;
        state.CityInfo = CharacterCreation.GetStartingCities();

        // Send the same packets that IncomingAccountPackets.GameLogin sends on e.Accepted = true
        state.CompressionEnabled = true;
        state.SendSupportedFeature();
        state.SendCharacterList();
    }

    /// <summary>
    /// Finds an existing local account or creates a new shell account.
    /// Gateway owns credentials; local account is for character storage only.
    /// </summary>
    private static Account FindOrCreateAccount(NetState state, string username, string accessLevel)
    {
        var acct = Accounts.GetAccount(username) as Account;

        if (acct == null)
        {
            acct = new Account(username, Guid.NewGuid().ToString());

            if (Enum.TryParse<AccessLevel>(accessLevel, true, out var level))
            {
                acct.AccessLevel = level;
            }

            logger.Information(
                "Login: {NetState} Created local account for '{Username}' (AccessLevel: {Level})",
                state, username, acct.AccessLevel);
        }

        return acct;
    }
}
