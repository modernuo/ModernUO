/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Firewall.cs                                                     *
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
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using Server.Collections;
using Server.Json;
using Server.Logging;

namespace Server.Network;

public static class Firewall
{
    // Single-threaded: the accept path, admin gump/command, TTL expiry timer, and boot load all run on
    // the main game loop. No locks, caches, or version counters are needed. See the ban-channel design doc.
    // _entries is the authoritative store (gump/persistence/TTL/command all work against it); _index is a
    // derived, rebuild-on-demand SortedRangeIndex used only for the accept-path IsBlocked lookup, shared
    // with the same sorted-range binary-search primitive the blocklist uses (see BlocklistSnapshot).
    private static readonly List<IFirewallEntry> _entries = [];

    // Entries with a TTL: entry -> absolute expiry tick (Core.TickCount). Permanent entries are absent.
    private static readonly Dictionary<IFirewallEntry, long> _expiring = new();

    private static SortedRangeIndex<UInt128> _index = SortedRangeIndex<UInt128>.Empty;
    private static bool _indexDirty;

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(Firewall));
    private const string _path = "Configuration/firewall.json";
    private const string _legacyPath = "firewall.cfg";
    private static bool _dirty;
    private static bool _configured;

    public static int FirewallSetCount => _entries.Count;

    public static void ReadFirewallSet(Action<IReadOnlyCollection<IFirewallEntry>> callback) => callback(_entries);

    public static bool IsBlocked(IPAddress address)
    {
        if (_entries.Count == 0)
        {
            return false;
        }

        EnsureIndex();
        return _index.Contains(address.ToUInt128());
    }

    // Rebuilds the derived lookup index from the authoritative _entries list, but only when entries have
    // changed since the last build. Runs on the main game loop, so the pooled build buffer is single-threaded
    // (mt: false); only the two final SortedRangeIndex arrays are heap-allocated.
    private static void EnsureIndex()
    {
        if (!_indexDirty)
        {
            return;
        }

        using var ranges = PooledRefList<SortedRangeIndex<UInt128>.Range>.Create(_entries.Count, mt: false);
        foreach (var entry in _entries)
        {
            ranges.Add(new SortedRangeIndex<UInt128>.Range(entry.MinIpAddress, entry.MaxIpAddress));
        }

        ranges.Sort(SortedRangeIndex<UInt128>.ByMin);
        _index = SortedRangeIndex<UInt128>.Build(ranges.AsSpan());
        _indexDirty = false;
    }

    public static bool Add(IFirewallEntry firewallEntry) => Add(firewallEntry, TimeSpan.Zero);

    /// <summary>
    /// Adds an entry. <paramref name="ttl"/> &lt;= <see cref="TimeSpan.Zero"/> means permanent. Returns false
    /// if the entry was already present.
    /// </summary>
    // Indexed scan (no closure allocation); firewall lists are small, so O(n) is negligible and this
    // stays off the hot path (Add/Remove are admin/boot actions, not the accept path).
    private static int IndexOfEntry(IFirewallEntry entry)
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].CompareTo(entry) == 0)
            {
                return i;
            }
        }

        return -1;
    }

    public static bool Add(IFirewallEntry firewallEntry, TimeSpan ttl, bool persist = true)
    {
        if (firewallEntry == null || IndexOfEntry(firewallEntry) >= 0)
        {
            return false;
        }

        _entries.Add(firewallEntry);

        if (ttl > TimeSpan.Zero)
        {
            _expiring[firewallEntry] = Core.TickCount + (long)ttl.TotalMilliseconds;
        }

        _indexDirty = true;

        if (persist)
        {
            MarkDirty();
        }

        return true;
    }

    public static bool Remove(IFirewallEntry entry)
    {
        if (entry == null)
        {
            return false;
        }

        var index = IndexOfEntry(entry);
        if (index < 0)
        {
            return false;
        }

        // Remove the stored instance from _expiring (not the passed reference), so a value-equal
        // entry created elsewhere still clears the TTL bookkeeping.
        var stored = _entries[index];
        _entries.RemoveAt(index);
        _expiring.Remove(stored);
        _indexDirty = true;
        MarkDirty();
        return true;
    }

    /// <summary>
    /// Removes every entry whose TTL has elapsed. Called from the main-thread maintenance timer (Task 2).
    /// </summary>
    internal static void ExpireEntries(long nowTicks)
    {
        if (_expiring.Count == 0)
        {
            return;
        }

        List<IFirewallEntry> expired = null;
        foreach (var (entry, expiresAt) in _expiring)
        {
            if (expiresAt - nowTicks <= 0)
            {
                (expired ??= []).Add(entry);
            }
        }

        if (expired == null)
        {
            return;
        }

        foreach (var entry in expired)
        {
            _entries.Remove(entry);
            _expiring.Remove(entry);
        }

        _indexDirty = true;
        MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IFirewallEntry ToFirewallEntry(object entry) =>
        entry switch
        {
            IFirewallEntry firewallEntry => firewallEntry,
            IPAddress address            => new SingleIpFirewallEntry(address),
            string s                     => ToFirewallEntry(s),
            _                            => null
        };

    public static IFirewallEntry ToFirewallEntry(string entry)
    {
        if (entry == null)
        {
            return null;
        }

        try
        {
            var rangeSeparator = entry.IndexOf('-');
            if (rangeSeparator > -1)
            {
                return new CidrFirewallEntry(
                    IPAddress.Parse(entry.AsSpan(0, rangeSeparator)),
                    IPAddress.Parse(entry.AsSpan(rangeSeparator + 1))
                );
            }

            if (entry.IndexOf('/') > -1)
            {
                return new CidrFirewallEntry(entry);
            }

            return new SingleIpFirewallEntry(entry);
        }
        catch
        {
            return null;
        }
    }

    public static void Configure()
    {
        if (_configured)
        {
            return;
        }
        _configured = true;

        var path = Path.Join(Core.BaseDirectory, _path);

        if (File.Exists(path))
        {
            LoadFrom(JsonConfig.Deserialize<FirewallSettings>(path));
        }
        else
        {
            var legacyPath = ResolveLegacyCfgPath();
            if (legacyPath != null)
            {
                MigrateLegacyCfg(legacyPath);
                Save(); // materialize firewall.json; the .cfg is no longer read after this
                TryMarkLegacyCfgMigrated(legacyPath);
            }
        }

        // Main-thread maintenance: expire TTLs and flush pending writes. No background thread.
        Timer.DelayCall(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), Maintenance);
    }

    private static void Maintenance()
    {
        ExpireEntries(Core.TickCount);

        if (_dirty)
        {
            Save();
        }
    }

    private static void MarkDirty() => _dirty = true;

    internal static void LoadFrom(FirewallSettings settings)
    {
        if (settings?.Entries == null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var record in settings.Entries)
        {
            var entry = ToFirewallEntry(record.Value);
            if (entry == null)
            {
                logger.Warning("Ignoring unparseable firewall entry \"{Entry}\"", record.Value);
                continue;
            }

            var ttl = TimeSpan.Zero;
            if (record.Expires is { } expires)
            {
                ttl = expires - now;
                if (ttl <= TimeSpan.Zero)
                {
                    continue; // already expired
                }
            }

            Add(entry, ttl, persist: false);
        }
    }

    internal static FirewallSettings ToSettings()
    {
        var now = DateTime.UtcNow;
        var nowTicks = Core.TickCount;
        var list = new List<FirewallEntryRecord>(_entries.Count);

        foreach (var entry in _entries)
        {
            DateTime? expires = null;
            if (_expiring.TryGetValue(entry, out var expiresAtTick))
            {
                expires = now.AddMilliseconds(expiresAtTick - nowTicks);
            }

            list.Add(new FirewallEntryRecord { Value = entry.ToString(), Expires = expires });
        }

        return new FirewallSettings { Entries = list.ToArray() };
    }

    public static void Save()
    {
        _dirty = false;
        var path = Path.Join(Core.BaseDirectory, _path);
        var tmp = path + ".tmp";
        JsonConfig.Serialize(tmp, ToSettings());
        File.Move(tmp, path, overwrite: true); // atomic swap
    }

    /// <summary>
    /// Locates the legacy firewall.cfg to migrate. The modern convention is <see cref="Core.BaseDirectory"/>,
    /// checked first; the pre-collapse <c>AdminFirewall</c> used a bare relative path (resolved against the
    /// process's current working directory), which may differ from <see cref="Core.BaseDirectory"/> when the
    /// shard is launched from elsewhere, so that's checked as a fallback. Returns null if neither exists.
    /// </summary>
    private static string ResolveLegacyCfgPath()
    {
        var underBaseDirectory = Path.Join(Core.BaseDirectory, _legacyPath);
        if (File.Exists(underBaseDirectory))
        {
            return underBaseDirectory;
        }

        return File.Exists(_legacyPath) ? _legacyPath : null;
    }

    private static void MigrateLegacyCfg(string legacyPath)
    {
        var searchValues = System.Buffers.SearchValues.Create("*Xx?");

        using var reader = new StreamReader(legacyPath);
        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.AsSpan().ContainsAny(searchValues))
            {
                logger.Warning("Legacy firewall entry \"{Entry}\" ignored during migration", line);
                continue;
            }

            var entry = ToFirewallEntry(line);
            if (entry != null)
            {
                Add(entry, TimeSpan.Zero, persist: false);
            }
        }

        logger.Information("Migrated {Count} entr(ies) from legacy firewall.cfg to firewall.json", _entries.Count);
    }

    /// <summary>
    /// Renames the migrated <c>.cfg</c> to <c>firewall.cfg.migrated</c> so it isn't re-scanned on the next
    /// boot and operators can see it was already migrated. Best-effort: a locked/read-only file must not
    /// fail startup, since the migration itself (firewall.json) already succeeded.
    /// </summary>
    private static void TryMarkLegacyCfgMigrated(string legacyPath)
    {
        try
        {
            File.Move(legacyPath, legacyPath + ".migrated", overwrite: true);
        }
        catch (Exception e)
        {
            logger.Warning(e, "Could not rename migrated legacy firewall file \"{Path}\"", legacyPath);
        }
    }

    internal static void ResetForTesting()
    {
        _entries.Clear();
        _expiring.Clear();
        _index = SortedRangeIndex<UInt128>.Empty;
        _indexDirty = false;
        _configured = false;
    }
}
