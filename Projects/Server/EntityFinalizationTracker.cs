/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EntityFinalizationTracker.cs                                    *
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
using System.Runtime.CompilerServices;
using System.Threading;
using Server.Logging;

namespace Server;

public static class EntityFinalizationTracker
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(EntityFinalizationTracker));

    private sealed record TrackedEntity(WeakReference<IEntity> Reference, int ReferenceHash, DateTime RemovedAt);

    private static readonly Lock _sync = new();
    private static bool _enabled;
    private static DateTime _nextCheck = DateTime.MinValue;

#if TRACK_LEAKS
    private const bool CanBeEnabled = true;
#else
    private const bool CanBeEnabled = false;
#endif

    public static void Configure()
    {
        CommandSystem.Register("gc", AccessLevel.Administrator, _ => GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true));
        CommandSystem.Register("TrackLeaks", AccessLevel.Developer, TrackLeaks_OnCommand);
    }

    [Usage("TrackLeaks <on|off>")]
    [Description("Enables or disables entity leak tracking. May impact performance and should be used only when necessary!")]
    private static void TrackLeaks_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        if (!CanBeEnabled) {
            from.SendMessage("Entity leak tracking is not enabled in this build. Rebuild with TRACK_LEAKS defined.");
            return;
        }

        if (e.Arguments.Length == 0)
        {
            from.SendMessage("Usage: TrackLeaks <on|off>");
            return;
        }

        var enable = Utility.ToBoolean(e.Arguments[0]);
        var status = enable ? "enabled" : "disabled";
        if (enable == _enabled)
        {
            from.SendMessage($"Entity leak tracking already {status}.");
            return;
        }

        if (_enabled)
        {
            EnableLeakTracking(from);
        }
        else
        {
            DisableLeakTracking(from);
        }
    }

    private static void EnableLeakTracking(Mobile from)
    {
        if (_enabled)
        {
            return;
        }

        _enabled = true;
        Gen2GcCallback.Register(() =>
        {
            CheckLeaks();
            return _enabled;
        });

        from.SendMessage("Entity leak tracking enabled.");
        logger.Warning("Entity leak tracking enabled by {Name} ({Serial:X8})", from.Name, from.Serial);
    }

    private static void DisableLeakTracking(Mobile from)
    {
        if (!_enabled)
        {
            return;
        }

        _enabled = false;
        from.SendMessage("Entity leak tracking disabled.");
        logger.Warning("Entity leak tracking disabled by {Name} ({Serial:X8})", from.Name, from.Serial);
    }

    private static readonly List<TrackedEntity> _entities = [];
    private static readonly HashSet<int> _finalizedHashes = [];

    public static void TrackEntity<T>(T entity) where T : IEntity
    {
        lock (_sync)
        {
            var hash = RuntimeHelpers.GetHashCode(entity);
            _entities.Add(new TrackedEntity(new WeakReference<IEntity>(entity), hash, Core.Now));
        }
    }

    public static void NotifyFinalized(object entity)
    {
        lock (_sync)
        {
            _finalizedHashes.Add(RuntimeHelpers.GetHashCode(entity));
        }
    }

    private static void CheckLeaks()
    {
        if (_entities.Count == 0)
        {
            return;
        }

        var now = Core.Now;
        if (now <= _nextCheck)
        {
            return;
        }

        _nextCheck = now + TimeSpan.FromMinutes(2);

        _entities.RemoveAll(entry =>
        {
            if (_finalizedHashes.Remove(entry.ReferenceHash))
            {
                return true;
            }

            if (!entry.Reference.TryGetTarget(out var obj) || obj is not IEntity entity)
            {
                return true;
            }

            if (ExceptionToLeakCheck(entity))
            {
                return false;
            }

            var duration = now - entry.RemovedAt;

            if (duration <= TimeSpan.FromSeconds(120))
            {
                return false;
            }

            logger.Warning("[Leak Warning] {Name} ({Serial:X8}) collected but not finalized after {Duration}.", entity.GetType().Name, entity.Serial, duration);
            return false;
        });
    }

    private static bool ExceptionToLeakCheck(IEntity entity) =>
        // Mobiles that are deleted, but have a reference to a corpse that is not deleted should be exempt
        (entity as Mobile)?.Corpse?.Deleted == false;
}
