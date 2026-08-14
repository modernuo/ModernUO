/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LoginAllowlist.cs                                               *
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
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Server.Logging;
using Server.Network.Bans;

namespace Server.Network;

/// <summary>
/// An allowlist addresses earn by logging in successfully, so a reputation feed cannot get a known player
/// blocked and a flaky connection cannot get one globally banned.
/// </summary>
/// <remarks>
/// <para>
/// Consulted only after the blocklist has already matched, and again before a ban is contributed, so a
/// normal accept pays nothing for it. An entry is evidence rather than a licence: enough strikes inside the
/// window revokes it. It cannot bootstrap, so it hedges stable addresses and does not replace
/// <see cref="FileAllowlist"/>. See <c>dev-docs/ip-bans-and-allowlists.md</c>.
/// </para>
/// <para>
/// Both dictionaries are game-loop state. Only the file write runs off-loop, over a snapshot taken on the
/// loop.
/// </para>
/// </remarks>
public static class LoginAllowlist
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(LoginAllowlist));

    // Address (normalized v6 bits) -> unix seconds of its last successful login. Loop-only.
    private static readonly Dictionary<UInt128, long> _allowed = [];

    // Suppressed contributions in the current window. Only holds allowlisted addresses, so it is bounded by
    // _allowed and cannot be grown by an attacker.
    private static readonly Dictionary<UInt128, Strike> _strikes = [];

    // Reused across flushes: past ~5,300 entries a fresh UInt128[] is an LOH allocation, and this ran every
    // flush. Grown geometrically, never shrunk, so a steady shard stops allocating here entirely.
    private static UInt128[] _addressBuffer = [];
    private static long[] _stampBuffer = [];

    private static bool _enabled;
    private static string _path;
    private static long _ttlSeconds;
    private static int _escalateAfterStrikes;
    private static long _strikeWindowSeconds;
    private static bool _dirty;

    // Loop-only. The writer owns the buffers until it posts completion back, so a flush that lands while
    // one is in flight has to wait rather than overwrite them mid-write.
    private static bool _writing;

    public static int Count => _allowed.Count;

    private struct Strike
    {
        public int Count;
        public long WindowStart; // unix seconds; 0 means "no window open"
    }

    public static void Configure()
    {
        LoginAllowlistConfiguration.Load();
        var s = LoginAllowlistConfiguration.Settings;

        _enabled = s.Enabled && !string.IsNullOrWhiteSpace(s.File) && s.Ttl > TimeSpan.Zero;
        if (!_enabled)
        {
            return;
        }

        _path = Path.IsPathRooted(s.File) ? s.File : Path.Join(Core.BaseDirectory, s.File);
        _ttlSeconds = (long)s.Ttl.TotalSeconds;
        _escalateAfterStrikes = s.EscalateAfterStrikes;
        _strikeWindowSeconds = (long)s.StrikeWindow.TotalSeconds;
    }

    public static void Initialize()
    {
        if (!_enabled)
        {
            logger.Information("Login allowlist disabled");
            return;
        }

        Load();

        var interval = LoginAllowlistConfiguration.Settings.FlushInterval;
        if (interval <= TimeSpan.Zero)
        {
            interval = TimeSpan.FromHours(1);
        }

        Timer.DelayCall(interval, interval, Flush);

        // A clean stop writes whatever it was holding, so the interval only bounds what a crash loses.
        // HandleClosed skips InvokeShutdown when the server crashed, so the crash path needs its own.
        EventSink.Shutdown += OnShutdown;
        EventSink.ServerCrashed += OnCrashed;
    }

    /// <summary>
    /// Records a successful authentication. Private addresses are skipped: a LAN or loopback login says
    /// nothing about the public internet.
    /// </summary>
    public static void RecordLogin(IPAddress address) => RecordLogin(address, ToUnixSeconds(Core.Now));

    /// <summary>The pure write, split out so policy can be tested without a clock.</summary>
    internal static void RecordLogin(IPAddress address, long nowUnix)
    {
        if (!_enabled || address == null || address.IsPrivateNetwork())
        {
            return;
        }

        var key = address.ToUInt128();
        _allowed[key] = nowUnix;

        // A fresh login clears the tally: someone just proved they hold an account.
        _strikes.Remove(key);
        _dirty = true;
    }

    /// <summary>
    /// True when this address logged in within the TTL. Expiry is decided on read, so a stale entry left for
    /// the next flush can never allow anything.
    /// </summary>
    public static bool IsAllowed(IPAddress address) => IsAllowed(address, ToUnixSeconds(Core.Now));

    /// <summary>The pure decision, split out so the TTL policy can be tested without a clock.</summary>
    internal static bool IsAllowed(IPAddress address, long nowUnix)
    {
        if (!_enabled || address == null)
        {
            return false;
        }

        return _allowed.TryGetValue(address.ToUInt128(), out var stamp) && nowUnix - stamp <= _ttlSeconds;
    }

    public static bool IsExemptFromEscalation(IPAddress address, string reason) =>
        IsExemptFromEscalation(address, reason, ToUnixSeconds(Core.Now));

    /// <summary>
    /// Whether this contribution should be dropped instead of escalated, counting a strike if so. Not a pure
    /// read — calling it is what spends the address's allowance.
    /// </summary>
    internal static bool IsExemptFromEscalation(IPAddress address, string reason, long nowUnix)
    {
        // An operator's explicit ban, or a reason nobody opted in, escalates untouched.
        if (!BanReasons.IsBehavioral(reason) || !IsAllowed(address, nowUnix))
        {
            return false;
        }

        if (_escalateAfterStrikes <= 0)
        {
            return true; // revocation disabled: an entry is unconditional
        }

        var key = address.ToUInt128();
        _strikes.TryGetValue(key, out var strike);

        if (strike.WindowStart == 0 || nowUnix - strike.WindowStart > _strikeWindowSeconds)
        {
            strike = new Strike { WindowStart = nowUnix };
        }

        strike.Count++;

        if (strike.Count < _escalateAfterStrikes)
        {
            _strikes[key] = strike;
            return true;
        }

        // Allowance spent: drop the entry so this and all after it escalate. Earned back by logging in.
        _allowed.Remove(key);
        _strikes.Remove(key);
        _dirty = true;

        logger.Information(
            "{Address} revoked from the login allowlist after {Count} suppressed contribution(s); last was '{Reason}'",
            address,
            strike.Count,
            reason
        );

        return false;
    }

    internal static void LoadForTesting(bool enabled, long ttlSeconds, int escalateAfterStrikes = 0, long strikeWindowSeconds = 3600)
    {
        _allowed.Clear();
        _strikes.Clear();
        _writing = false;
        _dirty = false;
        _enabled = enabled;
        _ttlSeconds = ttlSeconds;
        _escalateAfterStrikes = escalateAfterStrikes;
        _strikeWindowSeconds = strikeWindowSeconds;
        _path = null;
    }

    private static long ToUnixSeconds(DateTime utc) => (long)(utc - DateTime.UnixEpoch).TotalSeconds;

    private static void Flush()
    {
        // _dirty stays set whenever this bails, so skipping never loses an entry. A save owns the disk and
        // nothing here is urgent. See the threading policy in CLAUDE.md (rules #3 and #10).
        if (!_enabled || !_dirty || _writing || World.Saving || World.WorldState == WorldState.PendingSave)
        {
            return;
        }

        var count = Snapshot(out var dropped);
        var addresses = _addressBuffer;
        var stamps = _stampBuffer;
        var path = _path;

        _dirty = false;
        _writing = true;

        _ = Task.Run(
            () =>
            {
                var written = Write(path, addresses, stamps, count, dropped);

                // Back to the loop to release the buffers: _writing and _dirty are loop state, so the
                // writer must not touch them itself. See CLAUDE.md rule #10.
                Core.LoopContext.Post(
                    () =>
                    {
                        _writing = false;
                        if (!written)
                        {
                            _dirty = true; // nothing reached disk; the next flush retries
                        }
                    }
                );
            }
        );
    }

    /// <summary>
    /// A crash is the case the flush interval cannot cover, so write on the way down.
    /// </summary>
    /// <remarks>
    /// The handler runs on whichever thread faulted. That is the loop thread for anything thrown out of
    /// <c>RunEventLoop</c>, but a background thread can get here too, and the dictionaries are loop state
    /// that the loop may still be mutating. Off-loop, the last hour is not worth the race.
    /// </remarks>
    private static void OnCrashed(ServerCrashedEventArgs e)
    {
        if (System.Threading.Thread.CurrentThread == Core.Thread)
        {
            OnShutdown();
        }
    }

    /// <summary>
    /// Called on a clean shutdown, where there is no loop left to hand a write off to. Synchronous on
    /// purpose: nothing schedules after this, so a handed-off write would reach no disk.
    /// </summary>
    private static void OnShutdown()
    {
        // A write already in flight holds the buffers and has all but the last moments of the list.
        if (!_enabled || !_dirty || _writing)
        {
            return;
        }

        var count = Snapshot(out var dropped);
        _dirty = false;

        Write(_path, _addressBuffer, _stampBuffer, count, dropped);
    }

    /// <summary>
    /// Prunes expired entries and copies what survives into the shared buffers, on the loop. Returns the
    /// live count; the buffers are longer than that and everything past it is stale.
    /// </summary>
    private static int Snapshot(out int dropped)
    {
        var nowUnix = ToUnixSeconds(Core.Now);
        var cutoff = nowUnix - _ttlSeconds;

        if (_addressBuffer.Length < _allowed.Count)
        {
            // Geometric so a shard adding addresses one at a time does not reallocate every flush.
            var size = Math.Max(_allowed.Count, Math.Max(64, _addressBuffer.Length * 2));
            _addressBuffer = new UInt128[size];
            _stampBuffer = new long[size];
        }

        var count = 0;
        dropped = 0;

        foreach (var (address, stamp) in _allowed)
        {
            if (stamp < cutoff)
            {
                _allowed.Remove(address);
                _strikes.Remove(address);
                dropped++;
                continue;
            }

            _addressBuffer[count] = address;
            _stampBuffer[count] = stamp;
            count++;
        }

        PruneStaleStrikes(nowUnix);
        return count;
    }

    /// <summary>Drops tallies whose window has closed.</summary>
    private static void PruneStaleStrikes(long nowUnix)
    {
        if (_strikes.Count == 0)
        {
            return;
        }

        foreach (var (address, strike) in _strikes)
        {
            if (nowUnix - strike.WindowStart > _strikeWindowSeconds)
            {
                _strikes.Remove(address);
            }
        }
    }

    /// <summary>Writes the list out. Returns false when nothing reached disk, so the caller can retry.</summary>
    private static bool Write(string path, UInt128[] addresses, long[] stamps, int count, int dropped)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Sibling + swap, so a reader never sees a half-written list.
            var tmp = path + ".tmp";

            using (var writer = new StreamWriter(tmp, false, new UTF8Encoding(false), 1 << 16))
            {
                writer.Write("# modernuo-login-allowlist generated=");
                writer.Write(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
                writer.Write(" count=");
                writer.Write(count);
                writer.Write('\n');

                for (var i = 0; i < count; i++)
                {
                    writer.Write(addresses[i].ToIpAddress().ToString());
                    writer.Write(' ');
                    writer.Write(stamps[i]);
                    writer.Write('\n');
                }
            }

            File.Move(tmp, path, true);

            if (dropped > 0)
            {
                logger.Information("Login allowlist wrote {Count} entr(ies), dropped {Dropped} past TTL", count, dropped);
            }

            return true;
        }
        catch (Exception e)
        {
            // Recoverable: entries are still in memory and the next flush retries.
            logger.Warning(e, "Could not write the login allowlist to \"{Path}\"", path);
            return false;
        }
    }

    private static void Load()
    {
        if (!File.Exists(_path))
        {
            logger.Information("Login allowlist empty: no file at \"{Path}\"", _path);
            return;
        }

        var cutoff = ToUnixSeconds(Core.Now) - _ttlSeconds;
        var loaded = 0;
        var skipped = 0;

        try
        {
            foreach (var line in File.ReadLines(_path))
            {
                var span = line.AsSpan().Trim();
                if (span.Length == 0 || span[0] == '#' || span[0] == ';')
                {
                    continue;
                }

                var sep = span.IndexOf(' ');
                if (sep <= 0 ||
                    !IPAddress.TryParse(span[..sep], out var address) ||
                    !long.TryParse(span[(sep + 1)..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stamp))
                {
                    skipped++;
                    continue;
                }

                // Expired on disk: do not carry a stranger into memory.
                if (stamp < cutoff)
                {
                    skipped++;
                    _dirty = true; // the file is now out of date; the next flush rewrites it
                    continue;
                }

                _allowed[address.ToUInt128()] = stamp;
                loaded++;
            }
        }
        catch (Exception e)
        {
            // Fail open: an unreadable list allows nobody, which beats refusing to boot.
            logger.Warning(e, "Could not read the login allowlist at \"{Path}\"; continuing with {Count}", _path, _allowed.Count);
            return;
        }

        logger.Information("Login allowlist loaded {Loaded} entr(ies) ({Skipped} expired or malformed)", loaded, skipped);
    }
}
