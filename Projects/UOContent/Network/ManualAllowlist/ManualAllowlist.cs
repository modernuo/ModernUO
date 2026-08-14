/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ManualAllowlist.cs                                                *
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
using System.Threading;
using System.Threading.Tasks;
using Server.Logging;

namespace Server.Network.Bans;

/// <summary>
/// The operator's own "leave this address alone" list, read from the same files
/// <c>tools/Export-IpBlocklist.ps1</c> subtracts at generation time.
/// </summary>
/// <remarks>
/// The generator already subtracts these from the blocklist, but that only covers being BLOCKED.
/// Behavioural detections never consult the blocklist, so without reading the files here a carve-out is
/// quietly routed around: one scanner behind a shared CGNAT address is enough to get the whole address
/// contributed and firewalled. Reading them also means an entry applies on the next reload rather than the
/// next regeneration. Opt-in via <c>ip-allowlist.json</c>'s <c>enabled</c>, since the poll runs for the
/// whole uptime; no shield against a manual ban either — see <see cref="BanExemptions"/>.
/// </remarks>
public static class ManualAllowlist
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ManualAllowlist));

    // Written by the reload poll (off-loop), read by the accept path (game loop). One volatile reference
    // swap is the whole synchronization story: readers see the old or the new snapshot, whole.
    private static volatile BlocklistSnapshot _snapshot = BlocklistSnapshot.Empty;

    private static string[] _patterns = [];
    private static TimeSpan _interval = TimeSpan.FromSeconds(60);
    private static long _lastStamp;
    private static CancellationTokenSource _cts;

    public static int Count => _snapshot.Count;

    /// <summary>True when an operator listed this address. Safe before <see cref="Initialize"/>.</summary>
    public static bool Contains(IPAddress address) => address != null && _snapshot.Contains(address);

    /// <summary>True when the shard is reading allowlist files. Safe before <see cref="Initialize"/>.</summary>
    public static bool Enabled { get; private set; }

    public static void Initialize()
    {
        ManualAllowlistConfiguration.Load();
        var settings = ManualAllowlistConfiguration.Settings;
        if (settings == null)
        {
            return;
        }

        _patterns = ResolvePaths(settings.Files);
        _interval = settings.ReloadInterval <= TimeSpan.Zero ? TimeSpan.FromSeconds(60) : settings.ReloadInterval;

        if (!settings.Enabled)
        {
            // The generator still subtracts these files, so a carve-out an operator already wrote looks
            // like it works right up until a behavioural detection contributes the address anyway.
            var present = ExpandPaths().Length;
            _patterns = [];

            if (present > 0)
            {
                logger.Warning(
                    "Manual allowlist is off (\"enabled\" false in ip-allowlist.json) but {Count} allowlist file(s) " +
                    "are present; those carve-outs will not suppress ban contributions",
                    present
                );
            }
            else
            {
                logger.Information("Manual allowlist disabled (\"enabled\" false in ip-allowlist.json)");
            }

            return;
        }

        if (_patterns.Length == 0)
        {
            logger.Information("Manual allowlist disabled (\"files\" empty in ip-allowlist.json)");
            return;
        }

        Enabled = true;
        Reload();

        _cts = CancellationTokenSource.CreateLinkedTokenSource(Core.ClosingTokenSource.Token);
        _ = Task.Run(() => PollLoop(_cts.Token), _cts.Token);
    }

    public static void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private static string[] ResolvePaths(string[] configured)
    {
        if (configured == null)
        {
            return [];
        }

        var resolved = new string[configured.Length];
        var count = 0;

        for (var i = 0; i < configured.Length; i++)
        {
            var path = configured[i];
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            // Relative resolves against BaseDirectory, never the working directory, which differs when the
            // shard is launched from elsewhere. Absolute is as-is, so shards can share a list.
            resolved[count++] = Path.IsPathRooted(path) ? path : Path.Join(Core.BaseDirectory, path);
        }

        Array.Resize(ref resolved, count);
        return resolved;
    }

    /// <summary>
    /// Expands the configured patterns to actual files. Done per poll rather than once, so a carve-out an
    /// admin adds is picked up without a restart.
    /// </summary>
    private static string[] ExpandPaths()
    {
        var files = new List<string>();

        for (var i = 0; i < _patterns.Length; i++)
        {
            var pattern = _patterns[i];
            var name = Path.GetFileName(pattern);

            if (name.IndexOf('*') < 0 && name.IndexOf('?') < 0)
            {
                if (File.Exists(pattern))
                {
                    files.Add(pattern);
                }

                continue;
            }

            try
            {
                var dir = Path.GetDirectoryName(pattern);
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                {
                    continue;
                }

                var matches = Directory.GetFiles(dir, name);
                Array.Sort(matches, StringComparer.Ordinal);

                for (var j = 0; j < matches.Length; j++)
                {
                    // Windows wildcard matching still honours legacy short names, so ".txt" can pull in the
                    // generator's ".txt.tmp" mid-swap. Check the real extension.
                    if (matches[j].EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        files.Add(matches[j]);
                    }
                }
            }
            catch
            {
                // Unreadable directory; the next poll retries.
            }
        }

        return files.ToArray();
    }

    private static async ValueTask PollLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                if (Stamp() != _lastStamp)
                {
                    // A save owns the disk and nothing here is urgent.
                    // See the threading policy in CLAUDE.md (rules #3 and #10).
                    while (World.Saving || World.WorldState == WorldState.PendingSave)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), token);
                    }

                    Reload();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                logger.Warning(e, "Manual allowlist reload check failed; keeping last snapshot ({Count})", Count);
            }
        }
    }

    /// <summary>
    /// Change fingerprint across every configured file. A missing file contributes nothing, so creating or
    /// deleting one also registers as a change.
    /// </summary>
    private static long Stamp()
    {
        var stamp = 0L;
        var paths = ExpandPaths();

        for (var i = 0; i < paths.Length; i++)
        {
            try
            {
                var info = new FileInfo(paths[i]);
                if (info.Exists)
                {
                    stamp = stamp * 31 + info.LastWriteTimeUtc.Ticks + info.Length;
                }
            }
            catch
            {
                // Mid-swap by the generator; the next poll picks it up.
            }
        }

        return stamp;
    }

    private static void Reload()
    {
        // Fingerprint BEFORE parsing, so it describes the version being read. Capturing after could skip a
        // version; a stale fingerprint only costs an extra reload.
        var stamp = Stamp();
        var combined = ReadAll(out var files);

        // Reuses the blocklist parser and interval index: an address set is direction-agnostic.
        var next = combined.Length == 0
            ? BlocklistSnapshot.Empty
            : BlocklistSnapshot.Build(combined, out _, out _);

        _snapshot = next; // single volatile swap; readers see old or new whole
        _lastStamp = stamp;

        logger.Information(
            "Manual allowlist loaded {Count} range(s) from {Files} file(s)",
            next.Count,
            files
        );
    }

    /// <summary>
    /// Concatenates every configured file into one buffer. The parser is line-based, so a newline join is
    /// enough, and membership stays a single lookup.
    /// </summary>
    private static byte[] ReadAll(out int files)
    {
        files = 0;

        var paths = ExpandPaths();
        var chunks = new byte[paths.Length][];
        var total = 0;

        for (var i = 0; i < paths.Length; i++)
        {
            try
            {
                if (!File.Exists(paths[i]))
                {
                    continue;
                }

                var bytes = File.ReadAllBytes(paths[i]);
                chunks[i] = bytes;
                total += bytes.Length + 1; // + newline separator
                files++;
            }
            catch (Exception e)
            {
                // Fail open per file: losing one entry beats refusing to load the rest.
                logger.Warning(e, "Could not read allowlist \"{Path}\"", paths[i]);
            }
        }

        if (total == 0)
        {
            return [];
        }

        var combined = new byte[total];
        var offset = 0;

        for (var i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i];
            if (chunk == null)
            {
                continue;
            }

            Buffer.BlockCopy(chunk, 0, combined, offset, chunk.Length);
            offset += chunk.Length;
            combined[offset++] = (byte)'\n';
        }

        return combined;
    }

    internal static void LoadForTesting(BlocklistSnapshot snapshot) => _snapshot = snapshot ?? BlocklistSnapshot.Empty;
}
