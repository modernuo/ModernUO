/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
    private static ILogger logger = LogFactory.GetLogger(typeof(World));

    private static readonly ItemPersistence _itemPersistence = new();
    private static readonly MobilePersistence _mobilePersistence = new();
    private static readonly GenericEntityPersistence<BaseGuild> _guildPersistence = new("Guilds", 3, 1, 0x7FFFFFFF);

    private static int _threadId;
    private static readonly SerializationThreadWorker[] _threadWorkers = new SerializationThreadWorker[Environment.ProcessorCount - 1];
    private static readonly ManualResetEvent _diskWriteHandle = new(true);
    private static readonly ConcurrentQueue<Item> _decayQueue = new();

    private static string _tempSavePath; // Path to the temporary folder for the save

    public const bool DirtyTrackingEnabled = false;
    public const uint ItemOffset = 0x40000000;
    public const uint MaxItemSerial = 0x7FFFFFFF;
    public const uint MaxMobileSerial = ItemOffset - 1;

    public static Serial NewMobile => _mobilePersistence.NewEntity;
    public static Serial NewItem => _itemPersistence.NewEntity;
    public static Serial NewGuild => _guildPersistence.NewEntity;

    public static Dictionary<Serial, Item> Items => _itemPersistence.EntitiesBySerial;
    public static Dictionary<Serial, Mobile> Mobiles => _mobilePersistence.EntitiesBySerial;
    public static Dictionary<Serial, BaseGuild> Guilds => _guildPersistence.EntitiesBySerial;

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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WaitForWriteCompletion()
    {
        _diskWriteHandle.WaitOne();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnqueueForDecay(Item item)
    {
        if (WorldState != WorldState.Saving)
        {
            logger.Warning("Attempting to queue {Item} for decay but the world is not saving", item);
            return;
        }

        _decayQueue.Enqueue(item);
    }

    public static void Broadcast(int hue, bool ascii, string text)
    {
        var length = OutgoingMessagePackets.GetMaxMessageLength(text);

        Span<byte> buffer = stackalloc byte[length].InitializePacket();

        foreach (var ns in TcpServer.Instances)
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

    public static void BroadcastStaff(int hue, bool ascii, string text)
    {
        var length = OutgoingMessagePackets.GetMaxMessageLength(text);

        Span<byte> buffer = stackalloc byte[length].InitializePacket();

        foreach (var ns in TcpServer.Instances)
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
        for (var i = 0; i < _threadWorkers.Length; i++)
        {
            _threadWorkers[i] = new SerializationThreadWorker();
        }
    }

    private static void FinishWorldSave()
    {
        WorldState = WorldState.Running;

        Persistence.PostSerializeAll(); // Process safety queues
    }

    public static void WriteFiles(object state)
    {
        Exception exception = null;

        var tempPath = PathUtility.EnsureRandomPath(_tempSavePath);

        try
        {
            var watch = Stopwatch.StartNew();
            logger.Information("Writing world save snapshot");

            Persistence.WriteSnapshot(tempPath, SerializedTypes);

            watch.Stop();

            logger.Information("Writing world save snapshot {Status} ({Duration:F2} seconds)", "done", watch.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        if (exception != null)
        {
            logger.Error(exception, "Writing world save snapshot {Status}.", "failed");
            Persistence.TraceException(exception);

            BroadcastStaff(0x35, true, "Writing world save snapshot failed.");
        }
        else
        {
            try
            {
                EventSink.InvokeWorldSavePostSnapshot(SavePath, tempPath);
                PathUtility.MoveDirectory(tempPath, SavePath);
                Directory.SetLastWriteTimeUtc(SavePath, Core.Now);
            }
            catch (Exception ex)
            {
                Persistence.TraceException(ex);
            }
        }

        // Clear types
        SerializedTypes.Clear();

        _diskWriteHandle.Set();

        Core.LoopContext.Post(FinishWorldSave);
    }

    private static void ProcessDecay()
    {
        while (_decayQueue.TryDequeue(out var item))
        {
            if (item.OnDecay())
            {
                // TODO: Add Logging
                item.Delete();
            }
        }
    }

    private static DateTime _serializationStart;

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

        // Start our serialization threads
        for (var i = 0; i < _threadWorkers.Length; i++)
        {
            _threadWorkers[i].Wake();
        }

        WorldState = WorldState.PendingSave;

        Core.RequestSnapshot();
    }

    internal static TimeSpan Snapshot()
    {
        if (WorldState != WorldState.PendingSave)
        {
            return TimeSpan.Zero;
        }

        WorldState = WorldState.Saving;

        Broadcast(0x35, true, "The world is saving, please wait.");

        logger.Information("Saving world");

        var watch = Stopwatch.StartNew();

        Exception exception = null;

        try
        {
            _serializationStart = Core.Now;
            Persistence.SerializeAll();

            // Pause the workers
            foreach (var worker in _threadWorkers)
            {
                worker.Sleep();
            }

            EventSink.InvokeWorldSave();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        WorldState = WorldState.WritingSave;

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

        ThreadPool.QueueUserWorkItem(WriteFiles);

        watch.Stop();

        return watch.Elapsed;
    }

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
            _threadWorkers[i].Exit();
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
            logger.Warning($"Attempted to call World.AddEntity with '{entity.GetType()}'. Must be a mobile or item.");
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
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BaseGuild FindGuild(Serial serial) => _guildPersistence.Find(serial);

    public static void AddGuild(BaseGuild guild) => _guildPersistence.AddEntity(guild);

    public static void RemoveGuild(BaseGuild guild) => _guildPersistence.RemoveEntity(guild);

    // Legacy: Only used for retrieving Items and Mobiles.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEntity FindEntity(Serial serial, bool returnDeleted = false, bool returnPending = false) =>
        FindEntity<IEntity>(serial, returnDeleted, returnPending);

    // Legacy: Only used for retrieving Items and Mobiles.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FindEntity<T>(Serial serial, bool returnDeleted = false, bool returnPending = false)
        where T : class, IEntity
    {
        if (serial.IsItem)
        {
            return _itemPersistence.Find(serial, returnDeleted, returnPending) as T;
        }

        return _mobilePersistence.Find(serial, returnDeleted, returnPending) as T;
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
            }
        }

        public override void Serialize()
        {
            foreach (var item in EntitiesBySerial.Values)
            {
                if (item.CanDecay() && item.LastMoved + item.DecayTime <= _serializationStart)
                {
                    EnqueueForDecay(item);
                }

                PushToCache(item);
            }
        }

        public override void PostSerialize()
        {
            ProcessDecay(); // Run this before the safety queue

            base.PostSerialize();
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

    private class SerializationThreadWorker
    {
        private readonly Thread _thread;
        private readonly AutoResetEvent _startEvent; // Main thread tells the thread to start working
        private readonly AutoResetEvent _stopEvent; // Main thread waits for the worker finish draining
        private bool _pause;
        private bool _exit;
        private readonly ConcurrentQueue<IGenericSerializable> _entities;

        public SerializationThreadWorker()
        {
            _startEvent = new AutoResetEvent(false);
            _stopEvent = new AutoResetEvent(false);
            _entities = new ConcurrentQueue<IGenericSerializable>();
            _thread = new Thread(Execute);
            _thread.Start(this);
        }

        public void Wake()
        {
            _startEvent.Set();
        }

        public void Sleep()
        {
            Volatile.Write(ref _pause, true);
            _stopEvent.WaitOne();
        }

        public void Exit()
        {
            _exit = true;
            Wake();
            Sleep();
        }

        public void Push(IGenericSerializable entity)
        {
            _entities.Enqueue(entity);
        }

        private static void Execute(object obj)
        {
            var serializedTypes = SerializedTypes;
            SerializationThreadWorker worker = (SerializationThreadWorker)obj;

            var reader = worker._entities;

            while (worker._startEvent.WaitOne())
            {
                while (true)
                {
                    bool pauseRequested = Volatile.Read(ref worker._pause);
                    if (reader.TryDequeue(out var entity))
                    {
                        entity.Serialize(serializedTypes);
                    }
                    else if (pauseRequested) // Break when finished
                    {
                        break;
                    }
                }

                worker._stopEvent.Set(); // Allow the main thread to continue now that we are finished
                worker._pause = false;

                if (Core.Closing || worker._exit)
                {
                    return;
                }
            }
        }
    }
}
