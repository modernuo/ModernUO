using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.Systems.Gateway;

/// <summary>
/// Stores sessions pushed from the gateway via SignalR.
/// Accessed only on the game thread -- no concurrency needed.
/// When a client sends 0x91, the login handler checks here first (O(1) lookup)
/// before falling back to network validation.
/// </summary>
public static class GatewaySessionStore
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GatewaySessionStore));
    private static readonly Dictionary<int, PushedSession> _sessions = new();
    private static TimerExecutionToken _cleanupTimer;

    public static void Configure()
    {
        if (!GatewayConfig.Enabled)
        {
            return;
        }

        // Cleanup expired sessions every 60 seconds
        Timer.StartTimer(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60), Cleanup, out _cleanupTimer);
    }

    /// <summary>
    /// Adds a pushed session. Called on the game thread via Core.LoopContext.Post.
    /// </summary>
    public static void Add(PushedSession session)
    {
        _sessions[session.AuthId] = session;
        logger.Debug("Cached pushed session AuthId=0x{AuthId:X8} for '{Username}'", session.AuthId, session.Username);
    }

    /// <summary>
    /// Tries to consume a session (one-time use). Returns true if found and not expired.
    /// Called on the game thread from the login handler.
    /// </summary>
    public static bool TryConsume(int authId, out PushedSession session)
    {
        if (_sessions.Remove(authId, out session!))
        {
            if (session.ExpiresAt > DateTime.UtcNow)
            {
                return true;
            }

            // Expired
            session = default!;
        }

        return false;
    }

    private static void Cleanup()
    {
        var now = DateTime.UtcNow;
        var expired = new List<int>();

        foreach (var (authId, session) in _sessions)
        {
            if (session.ExpiresAt < now)
            {
                expired.Add(authId);
            }
        }

        foreach (var authId in expired)
        {
            _sessions.Remove(authId);
        }

        if (expired.Count > 0)
        {
            logger.Debug("Cleaned up {Count} expired pushed sessions", expired.Count);
        }
    }
}

public record PushedSession(
    int AuthId,
    Guid GameAccountId,
    string Username,
    string AccessLevel,
    string ClientIP,
    string? ClientVersion,
    DateTime ExpiresAt
);
