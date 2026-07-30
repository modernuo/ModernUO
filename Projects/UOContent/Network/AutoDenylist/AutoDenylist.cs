/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AutoDenylist.cs                                                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Server.Collections;
using Server.Logging;
using Server.Network.Bans;

namespace Server.Network;

/// <summary>
/// A short-lived, in-memory denylist of addresses the shard itself just caught misbehaving.
/// </summary>
/// <remarks>
/// The local half of promotion. Contributing to CrowdSec only helps once an OS bouncer reacts; until then
/// every reconnect costs a socket, a buffer and a <c>NetState</c> slot — and the verdicts that matter most
/// are reachable only after reading bytes, like a zero seed. It is also the whole defence on a shard running
/// no bouncer, which is the default. Not persisted, by design: a holding pen that survives restarts is a ban
/// without a ban's review. Only <see cref="BanReasons.IsBehavioral"/> verdicts are held.
/// </remarks>
public static class AutoDenylist
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AutoDenylist));

    // Address (normalized v6 bits) -> Core.TickCount at which the hold lapses. Loop-only.
    private static readonly Dictionary<UInt128, long> _held = [];

    private static bool _enabled;
    private static long _durationMs;
    private static int _maxEntries;
    private static bool _warnedFull;

    public static int Count => _held.Count;

    public static void Configure()
    {
        AutoDenylistConfiguration.Load();
        var s = AutoDenylistConfiguration.Settings;

        _enabled = s.Enabled && s.Duration > TimeSpan.Zero && s.MaxEntries > 0;
        if (!_enabled)
        {
            return;
        }

        _durationMs = (long)s.Duration.TotalMilliseconds;
        _maxEntries = s.MaxEntries;

        ConnectionFilters.Register(new AutoDenylistFilter());
        BanChannel.Register(new AutoDenylistReporter());
    }

    /// <summary>
    /// Holds an address for the configured duration. Ignores non-behavioural verdicts and refuses to grow
    /// past the cap: the flood this exists for must not become a memory leak.
    /// </summary>
    public static void Hold(IPAddress address, string reason) => Hold(address, reason, Core.TickCount);

    internal static bool Hold(IPAddress address, string reason, long nowTicks)
    {
        if (!_enabled || address == null || !BanReasons.IsBehavioral(reason))
        {
            return false;
        }

        var key = address.ToUInt128();

        // An address already held is just extended, so no cap check is needed.
        if (!_held.ContainsKey(key) && _held.Count >= _maxEntries)
        {
            Sweep(nowTicks);

            if (_held.Count >= _maxEntries)
            {
                if (!_warnedFull)
                {
                    _warnedFull = true;
                    logger.Warning(
                        "Auto-denylist is full at {Max} addresses; further detections are disconnected but not held",
                        _maxEntries
                    );
                }

                return false;
            }
        }

        _held[key] = nowTicks + _durationMs;
        return true;
    }

    public static bool IsDenied(IPAddress address) => IsDenied(address, Core.TickCount);

    /// <summary>The pure decision, split out so the accept-path policy can be tested without a clock.</summary>
    internal static bool IsDenied(IPAddress address, long nowTicks)
    {
        if (!_enabled || address == null)
        {
            return false;
        }

        // Decided on read, so a lapsed hold cannot deny even before the sweep. Subtraction: TickCount wraps.
        return _held.TryGetValue(address.ToUInt128(), out var expires) && expires - nowTicks > 0;
    }

    /// <summary>Releases an address early, e.g. when an operator retracts a ban.</summary>
    public static void Release(IPAddress address)
    {
        if (_enabled && address != null)
        {
            _held.Remove(address.ToUInt128());
        }
    }

    internal static void Sweep(long nowTicks)
    {
        if (_held.Count == 0)
        {
            return;
        }

        using var lapsed = new PooledRefList<UInt128>(16);

        foreach (var (address, expires) in _held)
        {
            if (expires - nowTicks <= 0)
            {
                lapsed.Add(address);
            }
        }

        for (var i = 0; i < lapsed.Count; i++)
        {
            _held.Remove(lapsed[i]);
        }

        if (lapsed.Count > 0)
        {
            _warnedFull = false;
        }
    }

    internal static void LoadForTesting(bool enabled, long durationMs, int maxEntries)
    {
        _held.Clear();
        _enabled = enabled;
        _durationMs = durationMs;
        _maxEntries = maxEntries;
        _warnedFull = false;
    }
}

/// <summary>Accept-path gate for <see cref="AutoDenylist"/>.</summary>
public sealed class AutoDenylistFilter : IConnectionFilter
{
    public string Name => "auto-denylist";

    public void Register()
    {
    }

    public void Start(CancellationToken token)
    {
        // Only an optimisation: IsDenied expires on read.
        Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), () => AutoDenylist.Sweep(Core.TickCount));
    }

    public void Stop()
    {
    }

    public bool ShouldDeny(IPAddress address) => AutoDenylist.IsDenied(address);
}

/// <summary>
/// Feeds <see cref="AutoDenylist"/> from the ban channel. A reporter rather than a direct call, because the
/// detection sites live in the engine and must not reach into content.
/// </summary>
public sealed class AutoDenylistReporter : IBanReporter
{
    public string Name => "auto-denylist";

    public bool CanRetract => true;

    public void Register()
    {
    }

    public void Start(CancellationToken token)
    {
    }

    public void Stop()
    {
    }

    /// <summary>
    /// The contributed <paramref name="ttl"/> is ignored: how long a bouncer should ban an address is a
    /// different question from how long this shard holds it at accept.
    /// </summary>
    public void Report(IPAddress address, TimeSpan ttl, string reason) => AutoDenylist.Hold(address, reason);

    public void Retract(IPAddress address) => AutoDenylist.Release(address);
}
