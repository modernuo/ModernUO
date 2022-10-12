/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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
using System.Linq;
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
    Saving,
    WritingSave
}

public static class World
{
    private static ILogger logger = LogFactory.GetLogger(typeof(World));

    private static ManualResetEvent m_DiskWriteHandle = new(true);
    private static Dictionary<Serial, IEntity> _pendingAdd = new();
    private static Dictionary<Serial, IEntity> _pendingDelete = new();
    private static ConcurrentQueue<Item> _decayQueue = new();

    private static string _tempSavePath; // Path to the temporary folder for the save
    private static bool _enableSaveStats;

    public const bool DirtyTrackingEnabled = false;
    public const uint ItemOffset = 0x40000000;
    public const uint MaxItemSerial = 0x7FFFFFFF;
    public const uint MaxMobileSerial = ItemOffset - 1;
    private const uint _maxItems = MaxItemSerial - ItemOffset + 1;

    private static Serial _lastMobile = Serial.Zero;
    private static Serial _lastItem = (Serial)ItemOffset;
    private static Serial _lastGuild = Serial.Zero;

    public static Serial NewMobile
    {
        get
        {
            var last = _lastMobile;
            var maxMobile = (Serial)MaxMobileSerial;

            for (int i = 0; i < MaxMobileSerial; i++)
            {
                last++;

                if (last > maxMobile)
                {
                    last = (Serial)1;
                }

                if (FindMobile(last) == null)
                {
                    return _lastMobile = last;
                }
            }

            OutOfMemory("No serials left to allocate for mobiles");
            return Serial.MinusOne;
        }
    }

    public static Serial NewItem
    {
        get
        {
            var last = _lastItem;

            for (int i = 0; i < _maxItems; i++)
            {
                last++;

                if (last > MaxItemSerial)
                {
                    last = (Serial)ItemOffset;
                }

                if (FindItem(last) == null)
                {
                    return _lastItem = last;
                }
            }

            OutOfMemory("No serials left to allocate for items");
            return Serial.MinusOne;
        }
    }

    public static Serial NewGuild
    {
        get
        {
            while (FindGuild(_lastGuild += 1) != null)
            {
            }

            return _lastGuild;
        }
    }

    private static void OutOfMemory(string message) => throw new OutOfMemoryException(message);

    public static string SavePath { get; private set; }

    public static WorldState WorldState { get; private set; }
    public static bool Saving => WorldState == WorldState.Saving;
    public static bool Running => WorldState is not WorldState.Loading and not WorldState.Initial;
    public static bool Loading => WorldState == WorldState.Loading;

    public static Dictionary<Serial, Mobile> Mobiles { get; private set; }
    public static Dictionary<Serial, Item> Items { get; private set; }
    public static Dictionary<Serial, BaseGuild> Guilds { get; private set; }

    public static void Configure()
    {
        var tempSavePath = ServerConfiguration.GetSetting("world.tempSavePath", "temp");
        _tempSavePath = PathUtility.GetFullPath(tempSavePath);

        var savePath = ServerConfiguration.GetOrUpdateSetting("world.savePath", "Saves");
        SavePath = PathUtility.GetFullPath(savePath);

        _enableSaveStats = ServerConfiguration.GetOrUpdateSetting("world.enableSaveStats", false);

        // Mobiles & Items
        Persistence.Register("Mobiles & Items", SaveEntities, WriteEntities, LoadEntities, 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WaitForWriteCompletion()
    {
        m_DiskWriteHandle.WaitOne();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnqueueForDecay(Item item)
    {
        if (WorldState != WorldState.Saving)
        {
            logger.Warning($"Attempting to queue {item} for decay but the world is not saving");
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Broadcast(int hue, bool ascii, string format, params object[] args) =>
        Broadcast(hue, ascii, string.Format(format, args));

    internal static void LoadEntities(string basePath, Dictionary<ulong, string> typesDb)
    {
        IIndexInfo<Serial> itemIndexInfo = new EntityTypeIndex("Items");
        IIndexInfo<Serial> mobileIndexInfo = new EntityTypeIndex("Mobiles");
        IIndexInfo<Serial> guildIndexInfo = new EntityTypeIndex("Guilds");

        Mobiles = EntityPersistence.LoadIndex(basePath, mobileIndexInfo, typesDb, out List<EntitySpan<Mobile>> mobiles);
        Items = EntityPersistence.LoadIndex(basePath, itemIndexInfo, typesDb, out List<EntitySpan<Item>> items);
        Guilds = EntityPersistence.LoadIndex(basePath, guildIndexInfo, typesDb, out List<EntitySpan<BaseGuild>> guilds);

        if (Mobiles.Count > 0)
        {
            _lastMobile = Mobiles.Keys.Max();
        }

        if (Items.Count > 0)
        {
            _lastItem = Items.Keys.Max();
        }

        if (Guilds.Count > 0)
        {
            _lastGuild = Guilds.Keys.Max();
        }

        EntityPersistence.LoadData(basePath, mobileIndexInfo, typesDb, mobiles);
        EntityPersistence.LoadData(basePath, itemIndexInfo, typesDb,  items);
        EntityPersistence.LoadData(basePath, guildIndexInfo, typesDb, guilds);
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

        ProcessSafetyQueues();

        foreach (var item in Items.Values)
        {
            if (item.Parent == null)
            {
                item.UpdateTotals();
            }

            item.ClearProperties();
        }

        foreach (var m in Mobiles.Values)
        {
            m.UpdateRegion(); // Is this really needed?
            m.UpdateTotals();

            m.ClearProperties();
        }

        watch.Stop();

        logger.Information("World loaded ({ItemCount} items, {MobileCount} mobiles) ({Duration:F2} seconds)",
            Items.Count,
            Mobiles.Count,
            watch.Elapsed.TotalSeconds
        );

        WorldState = WorldState.Running;
    }

    private static void ProcessSafetyQueues()
    {
        foreach (var entity in _pendingAdd.Values)
        {
            AddEntity(entity);
        }

        foreach (var entity in _pendingDelete.Values)
        {
            if (_pendingAdd.ContainsKey(entity.Serial))
            {
                logger.Warning("Entity {Entity} was both pending both deletion and addition after save", entity);
            }

            RemoveEntity(entity);
        }
    }

    private static void AppendSafetyLog(string action, ISerializable entity)
    {
        var message =
            $"Warning: Attempted to {action} {entity} during world save.{Environment.NewLine}This action could cause inconsistent state.{Environment.NewLine}It is strongly advised that the offending scripts be corrected.";

        logger.Information(message);

        try
        {
            using var op = new StreamWriter("world-save-errors.log", true);
            op.WriteLine("{0}\t{1}", DateTime.UtcNow, message);
            op.WriteLine(new StackTrace(2).ToString());
            op.WriteLine();
        }
        catch
        {
            // ignored
        }
    }

    private static void FinishWorldSave()
    {
        WorldState = WorldState.Running;

        ProcessDecay();
        ProcessSafetyQueues();
    }

    private static void TraceSave(params IEnumerable<KeyValuePair<string, int>>[] entityTypes)
    {
        try
        {
            int count = 0;

            var timestamp = Utility.GetTimeStamp();
            var saveStatsPath = Path.Combine(Core.BaseDirectory, $"Logs/Saves/Save-Stats-{timestamp}.log");
            PathUtility.EnsureDirectory(saveStatsPath);

            using var op = new StreamWriter(saveStatsPath, true);

            for (var i = 0; i < entityTypes.Length; i++)
            {
                foreach (var (t, c) in entityTypes[i])
                {
                    op.WriteLine("{0}: {1}", t, c);
                    count++;
                }
            }

            op.WriteLine("- Total: {0}", count);

            op.WriteLine();
            op.WriteLine();
        }
        catch
        {
            // ignored
        }
    }

    internal static void WriteEntities(string basePath)
    {
        IIndexInfo<Serial> itemIndexInfo = new EntityTypeIndex("Items");
        IIndexInfo<Serial> mobileIndexInfo = new EntityTypeIndex("Mobiles");
        IIndexInfo<Serial> guildIndexInfo = new EntityTypeIndex("Guilds");

        EntityPersistence.WriteEntities(mobileIndexInfo, Mobiles, basePath, SerializedTypes, out var mobileCounts);
        EntityPersistence.WriteEntities(itemIndexInfo, Items, basePath, SerializedTypes, out var itemCounts);
        EntityPersistence.WriteEntities(guildIndexInfo, Guilds, basePath, SerializedTypes, out var guildCounts);

        if (_enableSaveStats)
        {
            TraceSave(mobileCounts?.ToList(), itemCounts?.ToList(), guildCounts?.ToList());
        }
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

            logger.Information("Writing world save snapshot done ({Duration:F2} seconds)", watch.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        if (exception != null)
        {
            logger.Error(exception, "Writing world save snapshot failed.");
            Persistence.TraceException(exception);

            BroadcastStaff(0x35, true, "Writing world save snapshot failed.");
        }
        else
        {
            try
            {
                EventSink.InvokeWorldSavePostSnapshot(SavePath, tempPath);
                PathUtility.MoveDirectory(tempPath, SavePath);
            }
            catch (Exception ex)
            {
                Persistence.TraceException(ex);
            }
        }

        // Clear types
        SerializedTypes.Clear();

        m_DiskWriteHandle.Set();

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

    internal static void SaveEntities()
    {
        _serializationStart = DateTime.UtcNow;
        EntityPersistence.SaveEntities(Items.Values, SaveEntity);
        EntityPersistence.SaveEntities(Mobiles.Values, SaveEntity);
        EntityPersistence.SaveEntities(Guilds.Values, SaveEntity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SaveEntity<T>(T entity) where T : class, ISerializable
    {
        if (entity is Item item && item.CanDecay() && item.LastMoved + item.DecayTime <= _serializationStart)
        {
            EnqueueForDecay(item);
        }

        entity.Serialize(SerializedTypes);
    }

    public static void Save()
    {
        if (WorldState != WorldState.Running)
        {
            return;
        }

        WaitForWriteCompletion(); // Blocks Save until current disk flush is done.

        WorldState = WorldState.Saving;

        m_DiskWriteHandle.Reset();

        Broadcast(0x35, true, "The world is saving, please wait.");

        logger.Information("Saving world");

        var watch = Stopwatch.StartNew();

        Exception exception = null;

        try
        {
            Persistence.Serialize();
            EventSink.InvokeWorldSave();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        WorldState = WorldState.WritingSave;

        watch.Stop();

        if (exception == null)
        {
            var duration = watch.Elapsed.TotalSeconds;
            logger.Information("World save completed ({Duration:F2} seconds)", duration);

            // Only broadcast if it took at least 150ms
            if (duration >= 0.15)
            {
                Broadcast(0x35, true, $"World Save completed in {duration:F2} seconds.");
            }
        }
        else
        {
            logger.Error(exception, "World save failed");
            Persistence.TraceException(exception);

            BroadcastStaff(0x35, true, "World save failed.");
        }

        ThreadPool.QueueUserWorkItem(WriteFiles);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEntity FindEntity(Serial serial, bool returnDeleted = false) => FindEntity<IEntity>(serial, returnDeleted);

    public static T FindEntity<T>(Serial serial, bool returnDeleted = false) where T : class, IEntity
    {
        switch (WorldState)
        {
            default: return default;
            case WorldState.Loading:
            case WorldState.Saving:
            case WorldState.WritingSave:
                {
                    if (_pendingDelete.TryGetValue(serial, out var entity))
                    {
                        return !returnDeleted ? null : entity as T;
                    }

                    if (_pendingAdd.TryGetValue(serial, out entity))
                    {
                        return entity as T;
                    }

                    goto case WorldState.Running;
                }
            case WorldState.Running:
                {
                    if (serial.IsItem)
                    {
                        Items.TryGetValue(serial, out var item);
                        return item as T;
                    }

                    if (serial.IsMobile)
                    {
                        Mobiles.TryGetValue(serial, out var mob);
                        return mob as T;
                    }

                    return default;
                }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item FindItem(Serial serial, bool returnDeleted = false) => FindEntity<Item>(serial, returnDeleted);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mobile FindMobile(Serial serial, bool returnDeleted = false) =>
        FindEntity<Mobile>(serial, returnDeleted);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BaseGuild FindGuild(Serial serial) => Guilds.TryGetValue(serial, out var guild) ? guild : null;

    public static void AddEntity<T>(T entity) where T : class, IEntity
    {
        switch (WorldState)
        {
            default: // Not Running
                {
                    throw new Exception($"Added {entity.GetType().Name} before world load.\n");
                }
            case WorldState.Saving:
                {
                    AppendSafetyLog("add", entity);
                    goto case WorldState.WritingSave;
                }
            case WorldState.Loading:
            case WorldState.WritingSave:
                {
                    if (_pendingDelete.Remove(entity.Serial))
                    {
                        logger.Warning($"Deleted then added {entity.GetType().Name} during {WorldState.ToString()} state.");
                    }
                    _pendingAdd[entity.Serial] = entity;
                    break;
                }
            case WorldState.Running:
                {
                    if (entity.Serial.IsItem)
                    {
                        Items[entity.Serial] = entity as Item;
                    }

                    if (entity.Serial.IsMobile)
                    {
                        Mobiles[entity.Serial] = entity as Mobile;
                    }
                    break;
                }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddGuild(BaseGuild guild)
    {
        Guilds[guild.Serial] = guild;
    }

    public static void RemoveEntity<T>(T entity) where T : class, IEntity
    {
        switch (WorldState)
        {
            default: // Not Running
                {
                    throw new Exception($"Removed {entity.GetType().Name} before world load.\n");
                }
            case WorldState.Saving:
                {
                    AppendSafetyLog("delete", entity);
                    goto case WorldState.WritingSave;
                }
            case WorldState.Loading:
            case WorldState.WritingSave:
                {
                    _pendingAdd.Remove(entity.Serial);
                    _pendingDelete[entity.Serial] = entity;
                    break;
                }
            case WorldState.Running:
                {
                    if (entity.Serial.IsItem)
                    {
                        Items.Remove(entity.Serial);
                    }

                    if (entity.Serial.IsMobile)
                    {
                        Mobiles.Remove(entity.Serial);
                    }
                    break;
                }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveGuild(BaseGuild guild) => Guilds.Remove(guild.Serial);
}
