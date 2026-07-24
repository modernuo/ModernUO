/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: FileBlocklist.cs                                                *
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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Server.Logging;

namespace Server.Network.Bans.Blocklist;

/// <summary>
/// In-app gate for a large, file-sourced IP blocklist. Holds an immutable snapshot swapped atomically
/// by an off-loop reload poll; accept-path reads are lock-free. Inert when no file is configured.
/// </summary>
public static class FileBlocklist
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(FileBlocklist));

    private static volatile BlocklistSnapshot _snapshot = BlocklistSnapshot.Empty;
    private static string _path;
    private static TimeSpan _interval;
    private static string _lastGenerated;
    private static DateTime _lastWriteUtc;
    private static CancellationTokenSource _cts;

    public static int Count => _snapshot.Count;

    public static void Configure()
    {
        BanConfiguration.Configure();
        var s = BanConfiguration.Settings;
        _path = s.BlocklistFile;
        _interval = s.BlocklistReloadInterval <= TimeSpan.Zero ? TimeSpan.FromSeconds(60) : s.BlocklistReloadInterval;
    }

    public static void Start(CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(_path))
        {
            logger.Information("FileBlocklist disabled (blocklistFile empty in bans.json)");
            return;
        }
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        Reload(); // synchronous prime; empty on failure (fail-open)
        _ = Task.Run(() => PollLoop(_cts.Token), _cts.Token);
    }

    public static void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public static bool IsBanned(IPAddress ip) => _snapshot.IsBanned(ip);

    // Test hook: inject a snapshot without file I/O.
    public static void LoadForTesting(BlocklistSnapshot snapshot) => _snapshot = snapshot;

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
                if (ChangedSinceLastLoad())
                {
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
                logger.Warning(e, "FileBlocklist reload check failed; keeping last snapshot ({Count})", Count);
            }
        }
    }

    private static bool ChangedSinceLastLoad()
    {
        try
        {
            var info = new FileInfo(_path);
            if (!info.Exists)
            {
                return false;
            }
            if (info.LastWriteTimeUtc == _lastWriteUtc)
            {
                return false; // cheapest guard
            }
        }
        catch
        {
            return false;
        }
        return !BlocklistFile.TryReadHeader(_path, out var h) || h.Generated != _lastGenerated;
    }

    private static void Reload()
    {
        // Capture the mtime/header BEFORE Load() so the markers describe the version we're about to
        // parse, not whatever the producer may have atomically swapped in mid-parse. If a swap happens
        // mid-parse, the markers describe the old-or-equal version, so the next poll detects the change
        // and reloads again -- this errs toward reloading and never skips a version.
        var writeUtc = default(DateTime);
        try
        {
            writeUtc = new FileInfo(_path).LastWriteTimeUtc;
        }
        catch
        {
            /* keep default */
        }

        BlocklistFile.TryReadHeader(_path, out var h);

        var next = BlocklistFile.Load(_path, out var parsed, out var skipped);
        _snapshot = next; // single volatile swap; readers see old or new whole
        _lastGenerated = h.Generated;
        _lastWriteUtc = writeUtc;
        logger.Information("FileBlocklist loaded {Parsed} entr(ies) ({Count} ranges, {Skipped} skipped) gen={Gen}",
            parsed, next.Count, skipped, h.Generated);
    }
}
