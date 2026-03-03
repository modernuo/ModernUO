/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: World.cs                                                        *
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Server.Guilds;
using Server.Logging;
using Server.Network;

namespace Server;

public enum WorldState
{
    Initial,
    Loading,
    Running,
    PendingSave,
    Saving,
    WritingSave
}

public static class World
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(World));

    private static readonly ItemPersistence _itemPersistence = new();
    private static readonly MobilePersistence _mobilePersistence = new();
    private static readonly GenericEntityPersistence<BaseGuild> _guildPersistence = new("Guilds", 3, 1, 0x7FFFFFFF);

    private static int _threadId;
    internal static SerializationThreadWorker[] _threadWorkers;
    private static readonly ManualResetEvent _diskWriteHandle = new(true);

    private static string _tempSavePath; // Path to the temporary folder for the save

    public const bool DirtyTrackingEnabled = false;
    public const uint ItemOffset = 0x40000000;
    public const uint MaxItemSerial = 0x7EEEEEEE;
    public const uint MaxMobileSerial = ItemOffset - 1;

    public const uint ResetVirtualSerial = MaxItemSerial;
    public const uint MaxVirtualSerial = 0x7FFFFFFF;
    private static uint _nextVirtualSerial = ResetVirtualSerial;

    public static Serial NewMobile => _mobilePersistence.NewEntity;
    public static Serial NewItem => _itemPersistence.NewEntity;
    public static Serial NewGuild => _guildPersistence.NewEntity;

    // Virtual things don't persist across saves
    public static Serial NewVirtual
    {
        get
        {
#if THREADGUARD
            if (Thread.CurrentThread != Core.Thread)
            {
                logger.Error(
                    "Attempted to get a new virtual serial from the wrong thread!\n{StackTrace}",
                    new StackTrace()
                );
            }
#endif
            var value = _nextVirtualSerial > MaxVirtualSerial ? ResetVirtualSerial : _nextVirtualSerial;
            _nextVirtualSerial = value + 1;
            return (Serial)value;
        }
    }

    public static Dictionary<Serial, Item> Items => _itemPersistence.EntitiesBySerial;
    public static Dictionary<Serial, Mobile> Mobiles => _mobilePersistence.EntitiesBySerial;
    public static Dictionary<Serial, BaseGuild> Guilds => _guildPersistence.EntitiesBySerial;

    public static bool UseMultiThreadedSaves { get; private set; }
    public static string SavePath { get; private set; }
    public static WorldState WorldState { get; private set; }
    public static bool Saving => WorldState == WorldState.Saving;
    public static bool Running => WorldState is not WorldState.Loading and not WorldState.Initial;
    public static bool Loading => WorldState == WorldState.Loading;

    public static void Configure()
    {
        var tempSavePath = ServerConfiguration.GetSetting("world.tempSavePath", "temp");
        _tempSavePath = PathUtility.GetFullPath(tempSavePath);

        var savePath = ServerConfiguration.GetOrUpdateSetting("world.savePath", "Saves");
        SavePath = PathUtility.GetFullPath(savePath);

        UseMultiThreadedSaves = ServerConfiguration.GetOrUpdateSetting("world.useMultithreadedSaves", true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WaitForWriteCompletion() => _diskWriteHandle.WaitOne();

    public static void Broadcast(int hue, bool ascii, string text)
    {
        var length = OutgoingMessagePackets.GetMaxMessageLength(text);

        var buffer = stackalloc byte[length].InitializePacket();

        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile == null)
            {
                continue;
            }

            length = OutgoingMessagePackets.CreateMessage(
                buffer, Serial.MinusOne, -1, MessageType.Regular, hue, 3, ascii, "ENU", "System", text
            );

            if (length != buffer.Length)
            {
                buffer = buffer[..length]; // Adjust to the actual size
            }

            ns.Send(buffer);
        }

        NetState.FlushAll();
    }

    public static void BroadcastStaff(string text) => BroadcastStaff(0x35, false, text);

    public static void BroadcastStaff(int hue, bool ascii, string text)
    {
        var length = OutgoingMessagePackets.GetMaxMessageLength(text);

        var buffer = stackalloc byte[length].InitializePacket();

        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile == null || ns.Mobile.AccessLevel < AccessLevel.GameMaster)
            {
                continue;
            }

            length = OutgoingMessagePackets.CreateMessage(
                buffer, Serial.MinusOne, -1, MessageType.Regular, hue, 3, ascii, "ENU", "System", text
            );

            if (length != buffer.Length)
            {
                buffer = buffer[..length]; // Adjust to the actual size
            }

            ns.Send(buffer);
        }

        NetState.FlushAll();
    }

    public static void Load()
    {
        if (WorldState != WorldState.Initial)
        {
            return;
        }

        WorldState = WorldState.Loading;

        logger.Information("Loading world");
        var watch = Stopwatch.StartNew();

        Persistence.Load(SavePath);
        EventSink.InvokeWorldLoad();

        // Set the world to running before we process our queues
        WorldState = WorldState.Running;

        Persistence.PostDeserializeAll(); // Process safety queues

        watch.Stop();

        logger.Information("Loading world {Status} ({ItemCount} items, {MobileCount} mobiles) ({Duration:F2} seconds)",
            "done",
            Items.Count,
            Mobiles.Count,
            watch.Elapsed.TotalSeconds
        );

        // Create the serialization threads.
        var threadCount = UseMultiThreadedSaves ? Math.Max(Environment.ProcessorCount - 1, 1) : 1;
        _threadWorkers = new SerializationThreadWorker[threadCount];

        for (var i = 0; i < _threadWorkers.Length; i++)
        {
            _threadWorkers[i] = new SerializationThreadWorker(i);
        }
    }

    /**
     * Duplicates can be weeded out asynchronously while flushing
     * If performance becomes a problem, we need to build a dual mode concurrent array.
     *
     ****************************************************** Proposal ******************************************************
     * The structure is initialized with a large capacity to avoid unnecessary resizing.
     * Write Mode:
     * - Multiple threads can add a single, or a range of elements concurrently.
     * - Elements can be Peeked, but there are no guarantees.
     * - To resize the internal array, replaced it with the next size up from an array pool.
     * - The structure cannot be cleared in this mode.
     *
     * Read Mode:
     * - The array can be read from multiple threads using a ref struct enumerator.
     * - Elements cannot be added or reassigned.
     * - Cleared by replacing the internal array with another one from the pool.
     * - Note: Upon clearing, the existing array is not sent back to the pool until there are zero enumerators.
     *
     * Enumeration:
     * - Multiple threads can enumerate while in read mode. The enumerator will Interlocked.Increment a read counter.
     * - Upon dispose of the enumerator, the read counter will be lowered with an Interlocked.Decrement
     * - When the read counter reaches 0, if there is a cleared array, the array is sent back to the pool zeroed.
     *
     * Notes:
     * - Elements can never be removed.
     *
     * How is this different from ConcurrentQueue?
     * The functionality is very similar, except the constraints allow the implementation to be done without locks.
     * Since this implementation uses pooled arrays, allocations will approach zero over time.
     **********************************************************************************************************************
     */
    public static ConcurrentQueue<Type> SerializedTypes { get; } = new();

    public static void Save()
    {
        if (WorldState != WorldState.Running)
        {
            return;
        }

        WaitForWriteCompletion(); // Blocks Save until current disk flush is done.
        _diskWriteHandle.Reset();

        WorldState = WorldState.PendingSave;
        ThreadPool.QueueUserWorkItem(Preserialize);
    }

    private static void Preserialize(object state)
    {
        try
        {
            // Allocate the heaps for the GC
            foreach (var worker in _threadWorkers)
            {
                worker.AllocateHeap();
            }

            WakeSerializationThreads();

            // Execute this synchronously so we don't have a race condition
            Core.LoopContext.Post(() => Core.RequestSnapshot(PathUtility.EnsureRandomPath(_tempSavePath)));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Preparing to save world {Status}", "failed");
            Persistence.TraceException(ex);

            BroadcastStaff(0x35, true, "Preparing for a world save failed! Check the logs!");
        }
    }

    internal static void Snapshot(string snapshotPath)
    {
        if (WorldState != WorldState.PendingSave)
        {
            return;
        }

        NetState.FlushAll();

        WorldState = WorldState.Saving;

        Broadcast(0x35, true, "The world is saving, please wait.");

        logger.Information("Saving world");

        var watch = Stopwatch.StartNew();

        Exception exception = null;

        try
        {
            if (string.IsNullOrEmpty(snapshotPath))
            {
                throw new ArgumentException("Snapshot path cannot be null or empty", nameof(snapshotPath));
            }

            Persistence.SerializeAll();
            PauseSerializationThreads();
            EventSink.InvokeWorldSave();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        WorldState = WorldState.WritingSave;
        ThreadPool.QueueUserWorkItem(WriteFiles, snapshotPath);
        watch.Stop();

        if (exception == null)
        {
            var duration = watch.Elapsed.TotalSeconds;
            logger.Information("Saving world {Status} ({Duration:F2} seconds)", "done", duration);

            Broadcast(0x35, true, $"World save completed in {duration:F2} seconds.");
        }
        else
        {
            logger.Error(exception, "Saving world {Status}", "failed");
            Persistence.TraceException(exception);

            BroadcastStaff(0x35, true, "World save failed! Check the logs!");
        }
    }

    private static readonly HashSet<Type> _typesSet = [];

    private static void WriteFiles(object state)
    {
        var snapshotPath = (string)state;
        try
        {
            var watch = Stopwatch.StartNew();
            logger.Information("Writing world save snapshot");

            // Dedupe the types
            while (SerializedTypes.TryDequeue(out var type))
            {
                _typesSet.Add(type);
            }

            Persistence.WriteSnapshotAll(snapshotPath, _typesSet);

            _typesSet.Clear();

            try
            {
                EventSink.InvokeWorldSavePostSnapshot(SavePath, snapshotPath);
                PathUtility.MoveDirectoryContents(snapshotPath, SavePath);
                Directory.SetLastWriteTimeUtc(SavePath, Core.Now);
            }
            catch (Exception ex)
            {
                Persistence.TraceException(ex);
            }

            watch.Stop();
            logger.Information("Writing world save snapshot {Status} ({Duration:F2} seconds)", "done", watch.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Writing world save snapshot failed");
            Persistence.TraceException(ex);

            BroadcastStaff(0x35, true, "Writing world save snapshot failed! Check the logs!");
        }

        // Clear types
        SerializedTypes.Clear();

        _diskWriteHandle.Set();
        Core.LoopContext.Post(FinishWorldSave);
    }

    private static void FinishWorldSave()
    {
        WorldState = WorldState.Running;
        Persistence.PostWorldSaveAll(); // Process decay and safety queues
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WakeSerializationThreads()
    {
        for (var i = 0; i < _threadWorkers.Length; i++)
        {
            _threadWorkers[i].Wake();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void PauseSerializationThreads()
    {
        for (var i = 0; i < _threadWorkers.Length; i++)
        {
            _threadWorkers[i].Sleep();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetThreadWorkerCount() => Math.Max(Environment.ProcessorCount - 1, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ResetRoundRobin() => _threadId = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void PushToCache(IGenericSerializable e)
    {
        _threadWorkers[_threadId++].Push(e);
        if (_threadId == _threadWorkers.Length)
        {
            _threadId = 0;
        }
    }

    public static void ExitSerializationThreads()
    {
        for (var i = 0; i < _threadWorkers.Length; i++)
        {
            _threadWorkers[i]?.Exit();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item FindItem(Serial serial, bool returnDeleted = false) => _itemPersistence.Find(serial, returnDeleted);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mobile FindMobile(Serial serial, bool returnDeleted = false) =>
        _mobilePersistence.Find(serial, returnDeleted);

    // Legacy: Only used for retrieving Items and Mobiles.
    public static void AddEntity(IEntity entity)
    {
        if (entity is Item item)
        {
            _itemPersistence.AddEntity(item);
        }
        else if (entity is Mobile mobile)
        {
            _mobilePersistence.AddEntity(mobile);
        }
        else
        {
            logger.Warning("Attempted to call World.AddEntity with '{Entity}'. Must be a mobile or item.", entity.GetType());
        }
    }

    public static void RemoveEntity(IEntity entity)
    {
        if (entity is Item item)
        {
            _itemPersistence.RemoveEntity(item);
        }
        else if (entity is Mobile mobile)
        {
            _mobilePersistence.RemoveEntity(mobile);
        }
        else
        {
            logger.Warning($"Attempted to call World.RemoveEntity with '{entity.GetType()}'. Must be a mobile or item.");
            return;
        }

#if TRACK_LEAKS
        EntityFinalizationTracker.TrackEntity(entity);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BaseGuild FindGuild(Serial serial) => _guildPersistence.Find(serial);

    public static void AddGuild(BaseGuild guild) => _guildPersistence.AddEntity(guild);

    public static void RemoveGuild(BaseGuild guild) => _guildPersistence.RemoveEntity(guild);

    // Legacy: Only used for retrieving Items and Mobiles.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEntity FindEntity(Serial serial, bool returnDeleted = false) =>
        FindEntity<IEntity>(serial, returnDeleted);

    // Legacy: Only used for retrieving Items and Mobiles.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FindEntity<T>(Serial serial, bool returnDeleted = false)
        where T : class, IEntity
    {
        if (serial.IsItem)
        {
            return _itemPersistence.Find(serial, returnDeleted) as T;
        }

        return _mobilePersistence.Find(serial, returnDeleted) as T;
    }

    private class ItemPersistence : GenericEntityPersistence<Item>
    {
        public ItemPersistence() : base("Items", 2, ItemOffset, MaxItemSerial)
        {
        }

        public override void PostDeserialize()
        {
            base.PostDeserialize();

            foreach (var item in EntitiesBySerial.Values)
            {
                if (item.Parent == null)
                {
                    item.UpdateTotals();
                }

                item.ClearProperties();
                item.UpdateDecayRegistration();
            }
        }
    }

    private class MobilePersistence : GenericEntityPersistence<Mobile>
    {
        public MobilePersistence() : base("Mobiles", 1, 1, MaxMobileSerial)
        {
        }

        public override void PostDeserialize()
        {
            base.PostDeserialize();

            foreach (var m in EntitiesBySerial.Values)
            {
                m.UpdateRegion(); // Is this really needed?
                m.UpdateTotals();

                m.ClearProperties();
            }
        }
    }
}
