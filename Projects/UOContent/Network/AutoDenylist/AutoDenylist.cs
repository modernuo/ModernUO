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
using Server.Logging;
using Server.Network.Bans;

namespace Server.Network;

/// <summary>
/// A short-lived, in-memory denylist of addresses the shard itself just caught misbehaving.
/// </summary>
/// <remarks>
/// The local half of promotion. Contributing to CrowdSec only helps once an OS bouncer reacts; until then
/// every reconnect costs a socket, a buffer and a <c>NetState</c> slot — and the verdicts that matter most
/// are reachable only after reading bytes, like a zero seed. It is also the whole defense on a shard running
/// no bouncer, which is the default. Not persisted, by design: a holding pen that survives restarts is a ban
/// without a ban's review. Only <see cref="BanReasons.IsBehavioral"/> verdicts are held.
/// </remarks>
/// <remarks>
/// A hold is never refreshed, so every expiry is <c>insertion + duration</c> and the ring is sorted by
/// construction. Retiring lapsed entries is therefore the number expiring rather than the number held, which
/// is what lets the cap be sized for the flood instead of for a scan.
/// </remarks>
public static class AutoDenylist
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AutoDenylist));

    // Membership only (normalized v6 bits). Loop-only. The expiry lives beside the key in the ring, so
    // there is exactly one copy of it and the two cannot disagree.
    private static readonly HashSet<UInt128> _held = [];

    // The same keys in expiry order. Parallel arrays rather than an array of structs: UInt128 forces
    // 16-byte alignment, so a packed (key, expiry) struct costs 32 bytes where these cost 24 -- and the
    // drain reads only the long[], 8 sequential bytes per entry.
    private static UInt128[] _ringKeys = [];
    private static long[] _ringExpiry = [];
    private static int _ringHead;
    private static int _ringCount;

    private static bool _enabled;
    private static long _durationMs;
    private static int _maxEntries;
    private static bool _warnedFull;

    public static int Count => _held.Count;

    // Test seam: the ring and the set hold the same entries, and nothing else may assume it.
    internal static int RingCount => _ringCount;

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

        Drain(nowTicks);

        var key = address.ToUInt128();

        // Deliberately not refreshed: the first detection sets the expiry and later ones leave it alone.
        // That keeps insertion order equal to expiry order, which is why the drain can stop at the first
        // live record. A flooder whose hold lapses trips the rate limiter on its next attempt -- which
        // runs ahead of the connection filters -- and is held again.
        if (!_held.Add(key))
        {
            return true;
        }

        // Drain already reclaimed everything reclaimable, so being over now means genuinely full.
        if (_held.Count > _maxEntries)
        {
            _held.Remove(key);

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

        Push(key, nowTicks + _durationMs);
        return true;
    }

    public static bool IsDenied(IPAddress address) => IsDenied(address, Core.TickCount);

    /// <summary>
    /// The accept-path decision, split out so the policy can be tested without a clock. Drains first: the
    /// expiry lives in the ring, not beside the membership, so a lapsed hold has to be retired here rather
    /// than expired on read. One array read when nothing has lapsed.
    /// </summary>
    internal static bool IsDenied(IPAddress address, long nowTicks)
    {
        if (!_enabled || address == null)
        {
            return false;
        }

        Drain(nowTicks);
        return _held.Contains(address.ToUInt128());
    }

    /// <summary>Releases an address early, e.g. when an operator retracts a ban.</summary>
    public static void Release(IPAddress address)
    {
        if (!_enabled || address == null)
        {
            return;
        }

        var key = address.ToUInt128();
        if (_held.Remove(key))
        {
            // The ring record has to go too. Nothing records that this key was released, so if it were
            // detected again before the old record lapsed, that record would retire the new hold early.
            // O(n), but this is an operator retraction, not the accept path.
            PurgeRing(key);
        }
    }

    /// <summary>
    /// Retires everything that has lapsed. Expiries only ever increase along the ring, so the first live
    /// record ends the scan and the cost is the number actually expiring, not the number held.
    /// </summary>
    internal static void Drain(long nowTicks)
    {
        var before = _ringCount;

        // Subtraction, never a direct compare: tick counts wrap. See dev-docs/tick-counts.md.
        while (_ringCount > 0 && _ringExpiry[_ringHead] - nowTicks <= 0)
        {
            _held.Remove(_ringKeys[_ringHead]);
            _ringHead = _ringHead + 1 == _ringKeys.Length ? 0 : _ringHead + 1;
            _ringCount--;
        }

        if (_ringCount != before)
        {
            _warnedFull = false;
        }
    }

    private static void Push(UInt128 key, long expiry)
    {
        if (_ringCount == _ringKeys.Length)
        {
            Grow();
        }

        var tail = _ringHead + _ringCount;
        if (tail >= _ringKeys.Length)
        {
            tail -= _ringKeys.Length;
        }

        _ringKeys[tail] = key;
        _ringExpiry[tail] = expiry;
        _ringCount++;
    }

    private static void Grow()
    {
        // Capped at the entry cap: Push only runs below it, so the ring never needs more, and doubling
        // past it would reserve roughly twice the slots it can ever use.
        var size = Math.Min(Math.Max(64, _ringKeys.Length * 2), _maxEntries);
        var keys = new UInt128[size];
        var expiry = new long[size];

        for (var i = 0; i < _ringCount; i++)
        {
            var from = _ringHead + i;
            if (from >= _ringKeys.Length)
            {
                from -= _ringKeys.Length;
            }

            keys[i] = _ringKeys[from];
            expiry[i] = _ringExpiry[from];
        }

        _ringKeys = keys;
        _ringExpiry = expiry;
        _ringHead = 0;
    }

    private static void PurgeRing(UInt128 key)
    {
        var capacity = _ringKeys.Length;

        for (var i = 0; i < _ringCount; i++)
        {
            var at = _ringHead + i;
            if (at >= capacity)
            {
                at -= capacity;
            }

            if (_ringKeys[at] != key)
            {
                continue;
            }

            // Close the gap so the ring stays contiguous and expiry-ordered.
            for (var j = i; j < _ringCount - 1; j++)
            {
                var to = _ringHead + j;
                if (to >= capacity)
                {
                    to -= capacity;
                }

                var from = to + 1 == capacity ? 0 : to + 1;
                _ringKeys[to] = _ringKeys[from];
                _ringExpiry[to] = _ringExpiry[from];
            }

            _ringCount--;
            return;
        }
    }

    internal static void LoadForTesting(bool enabled, long durationMs, int maxEntries)
    {
        _held.Clear();
        _ringKeys = [];
        _ringExpiry = [];
        _ringHead = 0;
        _ringCount = 0;
        _enabled = enabled;
        _durationMs = durationMs;
        _maxEntries = maxEntries;
        _warnedFull = false;
    }
}

/// <summary>Accept-path gate for <see cref="AutoDenylist"/>.</summary>
public sealed class AutoDenylistFilter : IConnectionFilter
{
    private Timer _sweepTimer;

    public string Name => "auto-denylist";

    public void Register()
    {
    }

    public void Start(CancellationToken token)
    {
        // Only reclaims memory: Hold and IsDenied both drain, so this matters on a shard that has gone
        // quiet after a flood and would otherwise hold the ring until someone next connects.
        _sweepTimer = Timer.DelayCall(
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1),
            () => AutoDenylist.Drain(Core.TickCount)
        );
    }

    public void Stop()
    {
        // Recurring, so an uncancelled sweep survives Stop and the next Start adds a second one.
        _sweepTimer?.Stop();
        _sweepTimer = null;
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
