/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GenericEntityPersistence.cs                                     *
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
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Server.Logging;

namespace Server;

public interface IGenericEntityPersistence
{
    void DeserializeIndexes(string savePath, Dictionary<ulong, string> typesDb);
}

public class GenericEntityPersistence<T> : GenericPersistence, IGenericEntityPersistence, ISlotRangeSource
    where T : class, ISerializable
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GenericEntityPersistence<T>));

    // Layout-validated direct access to EntitiesBySerial's entries array, letting workers
    // iterate the dictionary in parallel during saves. Null when unsupported on this runtime.
    private static readonly FieldInfo _entriesField =
        ShadowDictionaryEntries.Supported ? ShadowDictionaryEntries.GetEntriesField<T>() : null;

    // The entries array captured at freeze time. The dictionary cannot mutate while saving
    // (adds/removes divert to the pending queues), so the array is stable until released.
    private object _entriesSnapshot;

    // Support legacy split file serialization
    private static Dictionary<int, List<EntitySpan<T>>> _entities;

    private readonly Serial _minSerial;
    private readonly Serial _maxSerial;
    private Serial _lastEntitySerial;
    private readonly Dictionary<Serial, T> _pendingAdd = new();
    private readonly Dictionary<Serial, T> _pendingDelete = new();

    // Insertion-ordered table of every entity type added since boot. idx v4 records
    // reference types by table index, so entries are never removed — a type whose
    // entities were all deleted keeps its slot until restart. Only mutated on the game
    // thread (AddEntity, deserialize); only read on the background writer thread during
    // WritingSave, when AddEntity diverts to the pending queues.
    private readonly Dictionary<Type, ushort> _typeIndexes = new();
    private readonly List<Type> _typeTable = [];

    internal IReadOnlyList<Type> TypeTable => _typeTable;

    internal bool TryGetTypeIndex(Type type, out ushort index) => _typeIndexes.TryGetValue(type, out index);

    internal void RegisterType(Type type)
    {
        ref var index = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeIndexes, type, out var exists);
        if (!exists)
        {
            if (_typeTable.Count > ushort.MaxValue)
            {
                throw new InvalidOperationException(
                    $"{Name} exceeded {ushort.MaxValue + 1} distinct entity types."
                );
            }

            index = (ushort)_typeTable.Count;
            _typeTable.Add(type);
        }
    }

    public Dictionary<Serial, T> EntitiesBySerial { get; } = new();

    public GenericEntityPersistence(string name, int priority, uint minSerial, uint maxSerial) : this(
        name,
        priority,
        (Serial)minSerial,
        (Serial)maxSerial
    )
    {
    }

    public GenericEntityPersistence(string name, int priority, Serial minSerial, Serial maxSerial) : base(name, priority)
    {
        _minSerial = minSerial;
        _maxSerial = maxSerial;
        _lastEntitySerial = minSerial - 1;
        typeof(T).RegisterFindEntity(Find);
    }

    public override void WriteSnapshot(string savePath)
    {
        var dir = Path.Combine(savePath, Name);
        PathUtility.EnsureDirectory(dir);

        var threads = World._threadWorkers;

        // 1MB buffer: segments are written as large spans, but idx entries and skip splits
        // still benefit on the snapshot thread.
        using var binFs = new FileStream(
            Path.Combine(dir, $"{Name}.bin"), FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024
        );
        // v4 records are fixed-width 26 bytes; the header carries the type table
        // (name lengths vary — 64 bytes per entry is a staging hint, not a contract).
        var expectedIdxSize = 12 + 26L * EntitiesBySerial.Count + 64L * _typeTable.Count;
        using var idx = new FileBufferWriter(Path.Combine(dir, $"{Name}.idx"), expectedIdxSize);

        var binPosition = 0L;

        // Support for non-entity generic serialization.
        if (_selfLength > 0)
        {
            try
            {
                binFs.Write(threads[_selfThread].GetHeap(_selfPosition, _selfLength));
            }
            catch (Exception error)
            {
                logger.Error(
                    error,
                    "Error writing self-payload: (Thread: {Thread} - {Start} {Length})",
                    _selfThread,
                    _selfPosition,
                    _selfLength
                );
            }

            binPosition += _selfLength;
        }

        idx.Write(4); // Version

        // The type table is fully known at freeze (AddEntity diverts to the pending
        // queues while saving) and is written before the records so the loader can
        // resolve constructors before reading them.
        idx.Write(_typeTable.Count);
        for (var i = 0; i < _typeTable.Count; i++)
        {
            idx.WriteRaw(_typeTable[i].FullName);
        }

        var countPosition = idx.Position;
        idx.Write(0);

        // The bin is written in worker-heap order, not dictionary order: each worker logged
        // (segment, record lengths) as it serialized, so records pair with their bytes by
        // re-walking the same slots in the same order. The idx stores absolute positions,
        // so the loader never cares about record order.
        var entityCount = 0;

        for (var t = 0; t < threads.Length; t++)
        {
            var worker = threads[t];
            var segments = worker.Segments;

            for (var s = 0; s < segments.Count; s++)
            {
                var segment = segments[s];
                if (!ReferenceEquals(segment.Owner, this))
                {
                    continue;
                }

                try
                {
                    binPosition = WriteSegmentRecords(worker, in segment, idx, binFs, binPosition, ref entityCount);
                }
                catch (Exception error)
                {
                    logger.Error(
                        error,
                        "Error writing segment: (Thread: {Thread} - {Start}, {Records} records)",
                        t,
                        segment.HeapStart,
                        segment.RecordCount
                    );
                }
            }
        }

        var currentPosition = idx.Position;
        idx.Seek(countPosition, SeekOrigin.Begin);
        idx.Write(entityCount);
        idx.Seek(currentPosition, SeekOrigin.Begin);
    }

    private long WriteSegmentRecords(
        SerializationThreadWorker worker, in SerializedSegment segment, FileBufferWriter idx, FileStream binFs,
        long binPosition, ref int entityCount
    )
    {
        var lengths = worker.Lengths;
        var lengthIndex = segment.LengthsStart;

        var heapPos = (int)segment.HeapStart;
        var spanStart = heapPos;

        if (segment.SlotOffset >= 0)
        {
            // Re-walk the same slots the worker serialized; occupancy cannot have changed
            // because dictionary mutations divert to the pending queues until PostWorldSave.
            var entries = Unsafe.As<ShadowEntry<T>[]>(_entriesSnapshot);
            ref var entry = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(entries), segment.SlotOffset);

            for (var i = 0; i < segment.SlotCount; i++, entry = ref Unsafe.Add(ref entry, 1))
            {
                var entity = entry.Value;
                if (entity == null)
                {
                    continue;
                }

                var length = lengths[lengthIndex++];

                if (entity is Item { SkipSerialization: true } or Mobile { SkipSerialization: true })
                {
                    // The bytes exist in the heap but are not part of the save: split the span.
                    if (heapPos > spanStart)
                    {
                        binFs.Write(worker.GetHeap(spanStart, heapPos - spanStart));
                    }

                    heapPos += length;
                    spanStart = heapPos;
                    continue;
                }

                idx.Write(GetTypeIndex(entity));
                idx.Write(entity.Serial);
                idx.Write(entity.Created.Ticks);
                idx.Write(binPosition);
                idx.Write(length);

                binPosition += length;
                heapPos += length;
                entityCount++;
            }
        }
        else
        {
            var bufferEntities = worker.BufferEntities;

            for (var i = 0; i < segment.RecordCount; i++)
            {
                var entity = (T)bufferEntities[segment.EntitiesStart + i];
                var length = lengths[lengthIndex++];

                if (entity is Item { SkipSerialization: true } or Mobile { SkipSerialization: true })
                {
                    if (heapPos > spanStart)
                    {
                        binFs.Write(worker.GetHeap(spanStart, heapPos - spanStart));
                    }

                    heapPos += length;
                    spanStart = heapPos;
                    continue;
                }

                idx.Write(GetTypeIndex(entity));
                idx.Write(entity.Serial);
                idx.Write(entity.Created.Ticks);
                idx.Write(binPosition);
                idx.Write(length);

                binPosition += length;
                heapPos += length;
                entityCount++;
            }
        }

        if (heapPos > spanStart)
        {
            binFs.Write(worker.GetHeap(spanStart, heapPos - spanStart));
        }

        return binPosition;
    }

    private ushort GetTypeIndex(T entity)
    {
        if (!_typeIndexes.TryGetValue(entity.GetType(), out var typeIndex))
        {
            throw new InvalidOperationException(
                $"{entity.GetType()} was serialized but never registered; entities must enter {Name} through AddEntity."
            );
        }

        return typeIndex;
    }

    public override void Serialize()
    {
        // Self-payload first so a large one overlaps the entity stream instead of ending it.
        World.PushSingleToCache(this);

        // Fast path: publish slot ranges of the dictionary's entries array so the workers
        // iterate it directly in parallel — the main thread never touches the entities.
        if (TrySnapshotEntries(out var slotCount))
        {
            World.PushSlotRangesToCache(this, slotCount);
            return;
        }

        // Fallback: enumerate and hand off every entity from the main thread. Kept branch-free:
        // a bare loop is ~2.3x faster than one carrying per-entity logic, and multi-megabyte
        // entities are rare enough that riding inside a shared chunk is an acceptable tail.
        foreach (var entity in EntitiesBySerial.Values)
        {
            World.PushToCache(entity);
        }
    }

    internal bool TrySnapshotEntries(out int slotCount)
    {
        if (_entriesField != null && EntitiesBySerial.Count > 0 &&
            _entriesField.GetValue(EntitiesBySerial) is Array entries)
        {
            _entriesSnapshot = entries;
            slotCount = entries.Length;
            return true;
        }

        slotCount = 0;
        return false;
    }

    int ISlotRangeSource.SerializeRange(BufferWriter writer, List<int> lengths, int offset, int count)
    {
        // Layout proven at startup by ShadowDictionaryEntries.Supported; ranges are produced
        // from the same array's length, so every read is in-bounds.
        var entries = Unsafe.As<ShadowEntry<T>[]>(_entriesSnapshot);
        var serialized = 0;

        ref var entry = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(entries), offset);

        for (var i = 0; i < count; i++, entry = ref Unsafe.Add(ref entry, 1))
        {
            // Occupied slots are exactly the non-null values: Dictionary clears reference
            // values on remove, and never-used capacity is zero-initialized.
            var entity = entry.Value;
            if (entity != null)
            {
                var start = writer.Position;
                entity.Serialize(writer);
                lengths.Add((int)(writer.Position - start));
                serialized++;
            }
        }

        return serialized;
    }

    private static ConstructorInfo GetConstructorFor(string typeName, Type t, Type[] constructorTypes)
    {
        if (t?.IsAbstract != false)
        {
            Console.WriteLine("failed");

            var issue = t?.IsAbstract == true ? "marked abstract" : "not found";

            Console.Write($"Error: Type '{typeName}' was {issue}. Delete all of those types? (y/n): ");

            if (ConsoleInputHandler.ReadLine().InsensitiveEquals("y"))
            {
                Console.WriteLine("Loading...");
                return null;
            }

            Console.WriteLine("Types will not be deleted. An exception will be thrown.");

            throw new Exception($"Bad type '{typeName}'");
        }

        var ctor = t.GetConstructor(constructorTypes);

        if (ctor == null)
        {
            throw new Exception($"Type '{t}' does not have a serialization constructor");
        }

        return ctor;
    }

    /**
     * Legacy ReadTypes for backward compatibility with old saves that still have a tdb file
     */
    private unsafe Dictionary<ulong, ConstructorInfo> ReadTypes(string savePath)
    {
        var typesPath = Path.Combine(savePath, Name, $"{Name}.tdb");
        if (!File.Exists(typesPath))
        {
            return [];
        }

        Type[] ctorArguments = [typeof(Serial)];

        using var mmf = MemoryMappedFile.CreateFromFile(typesPath, FileMode.Open);
        using var accessor = mmf.CreateViewStream();

        byte* ptr = null;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        var dataReader = new UnmanagedDataReader(ptr, accessor.Length);

        var count = dataReader.ReadInt();
        var types = new Dictionary<ulong, ConstructorInfo>(count);

        for (var i = 0; i < count; ++i)
        {
            // Legacy didn't have the null flag check
            var typeName = dataReader.ReadStringRaw();
            var type = AssemblyHandler.FindTypeByName(typeName);
            var ctor = GetConstructorFor(typeName, type, ctorArguments);

            if (ctor != null)
            {
                // Keep the type table complete so the next (v4) save can index it.
                RegisterType(type);
            }

            types.Add((ulong)i, ctor);
        }

        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        return types;
    }

    public virtual void DeserializeIndexes(string savePath, Dictionary<ulong, string> typesDb)
    {
        var indexPath = Path.Combine(savePath, Name, $"{Name}.idx");

        _entities ??= [];

        // Support for legacy MUO Serialization that used split files
        if (!File.Exists(indexPath))
        {
            TryDeserializeSplitFileIndexes(savePath, typesDb);
            return;
        }

        InternalDeserializeIndexes(indexPath, typesDb, _entities[0] = []);
    }

    private void TryDeserializeSplitFileIndexes(string savePath, Dictionary<ulong, string> typesDb)
    {
        var index = 0;
        while (true)
        {
            var path = Path.Combine(savePath, Name, $"{Name}_{index}.idx");
            var fi = new FileInfo(path);
            if (!fi.Exists)
            {
                break;
            }

            if (fi.Length == 0)
            {
                continue;
            }

            InternalDeserializeIndexes(path, typesDb, _entities[index] = []);
            index++;
        }
    }

    private unsafe void InternalDeserializeIndexes(
        string filePath, Dictionary<ulong, string> typesDb, List<EntitySpan<T>> entities
    )
    {
        using var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);
        using var accessor = mmf.CreateViewStream();

        byte* ptr = null;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        var dataReader = new UnmanagedDataReader(ptr, accessor.Length);

        var version = dataReader.ReadInt();

        if (version >= 4)
        {
            DeserializeIndexesV4(dataReader, entities);
        }
        else
        {
            var ctors = version < 2 ? ReadTypes(Path.GetDirectoryName(filePath)) : [];

            if (typesDb == null && ctors.Count == 0)
            {
                accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                return;
            }

            var now = DateTime.UtcNow;
            var ctorArgs = new object[1];
            Type[] ctorArguments = [typeof(Serial)];

            var count = dataReader.ReadInt();

            for (var i = 0; i < count; ++i)
            {
                ulong hash;
                // Version 2 & 3 with SerializedTypes.db
                if (version >= 2)
                {
                    var flag = dataReader.ReadByte();
                    if (flag != 2)
                    {
                        throw new Exception($"Invalid type flag, expected 2 but received {flag}.");
                    }

                    hash = dataReader.ReadULong();
                }
                else
                {
                    hash = (ulong)dataReader.ReadInt(); // Legacy RunUO tdb index
                }

                if (!ctors.TryGetValue(hash, out var ctor) && typesDb?.TryGetValue(hash, out var typeName) == true)
                {
                    var type = AssemblyHandler.FindTypeByHash(hash);
                    ctors[hash] = ctor = GetConstructorFor(typeName, type, ctorArguments);

                    if (ctor != null)
                    {
                        // Keep the type table complete so the next (v4) save can index it.
                        RegisterType(type);
                    }
                }

                var serial = (Serial)dataReader.ReadUInt();
                var created = version == 0 ? now : new DateTime(dataReader.ReadLong(), DateTimeKind.Utc);
                if (version is > 0 and < 3)
                {
                    dataReader.ReadLong(); // LastSerialized
                }

                var pos = dataReader.ReadLong();
                var length = dataReader.ReadInt();

                if (ctor == null)
                {
                    continue;
                }

                ctorArgs[0] = serial;

                if (ctor.Invoke(ctorArgs) is T entity)
                {
                    entity.Created = created;
                    entities.Add(new EntitySpan<T>(entity, pos, length));
                    EntitiesBySerial[serial] = entity;
                }
            }
        }

        accessor.SafeMemoryMappedViewHandle.ReleasePointer();

        if (EntitiesBySerial.Count > 0)
        {
            _lastEntitySerial = EntitiesBySerial.Keys.Max();
        }
    }

    private void DeserializeIndexesV4(UnmanagedDataReader dataReader, List<EntitySpan<T>> entities)
    {
        Type[] ctorArguments = [typeof(Serial)];

        var typeCount = dataReader.ReadInt();
        var ctors = new ConstructorInfo[typeCount];

        for (var i = 0; i < typeCount; i++)
        {
            var typeName = dataReader.ReadStringRaw();
            var type = AssemblyHandler.FindTypeByHash(HashUtility.ComputeHash64(typeName));
            var ctor = GetConstructorFor(typeName, type, ctorArguments);

            if (ctor != null)
            {
                // Keep the type table complete so the next save can index it.
                RegisterType(type);
            }

            ctors[i] = ctor;
        }

        var ctorArgs = new object[1];
        var count = dataReader.ReadInt();

        for (var i = 0; i < count; ++i)
        {
            var ctor = ctors[dataReader.ReadUShort()];
            var serial = (Serial)dataReader.ReadUInt();
            var created = new DateTime(dataReader.ReadLong(), DateTimeKind.Utc);
            var pos = dataReader.ReadLong();
            var length = dataReader.ReadInt();

            if (ctor == null)
            {
                continue;
            }

            ctorArgs[0] = serial;

            if (ctor.Invoke(ctorArgs) is T entity)
            {
                entity.Created = created;
                entities.Add(new EntitySpan<T>(entity, pos, length));
                EntitiesBySerial[serial] = entity;
            }
        }
    }

    public override void Deserialize(string savePath, Dictionary<ulong, string> typesDb)
    {
        var dataPath = Path.Combine(savePath, Name, $"{Name}.bin");
        var fi = new FileInfo(dataPath);

        if (!fi.Exists)
        {
            TryDeserializeMultithread(savePath, typesDb);
        }
        else if (fi.Length > 0)
        {
            InternalDeserialize(dataPath, 0, typesDb);
        }

        _entities.Clear();
        _entities.TrimExcess();
        _entities = null;

        if (_toDelete != null)
        {
            foreach (var t in _toDelete)
            {
                t.Delete();
            }

            _toDelete.Clear();
            _toDelete = null;
        }
    }

    private static List<T> _toDelete;

    private unsafe void InternalDeserialize(string filePath, int index, Dictionary<ulong, string> typesDb)
    {
        using var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);
        using var accessor = mmf.CreateViewStream();

        byte* ptr = null;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        var dataReader = new UnmanagedDataReader(ptr, accessor.Length, typesDb);

        Deserialize(dataReader);

        var deleteAllFailures = false;

        foreach (var entry in _entities[index])
        {
            var t = entry.Entity;

            if (entry.Length == 0)
            {
                t?.Delete();
                continue;
            }

            // Skip this entry
            if (t == null)
            {
                continue;
            }

            string error;

            try
            {
                dataReader.Seek(entry.Position, SeekOrigin.Begin);
                var pos = entry.Position;

                t.Deserialize(dataReader);
                var lengthDeserialized = dataReader.Position - pos;

                error = lengthDeserialized != entry.Length
                    ? $"Serialized object was {entry.Length} bytes, but {lengthDeserialized} bytes deserialized"
                    : null;
            }
            catch (Exception e)
            {
                error = e.ToString();
            }

            if (error != null)
            {
                Console.WriteLine($"***** Bad deserialize of {t.GetType()} ({t.Serial}) *****");
                Console.WriteLine(error);

                if (!deleteAllFailures)
                {
                    Console.Write("Delete the object and continue? (y/n/a): ");
                    var pressedKey = ConsoleInputHandler.ReadLine();

                    if (pressedKey.InsensitiveEquals("a"))
                    {
                        deleteAllFailures = true;
                    }
                    else if (!pressedKey.InsensitiveEquals("y"))
                    {
                        throw new Exception("Deserialization failed.");
                    }
                }

                _toDelete ??= [];
                _toDelete.Add(t);
            }
        }

        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
    }

    private void TryDeserializeMultithread(string savePath, Dictionary<ulong, string> typesDb)
    {
        if (_entities == null)
        {
            return;
        }

        var folderPath = Path.Combine(savePath, Name);

        foreach (var i in _entities.Keys)
        {
            var path = Path.Combine(folderPath, $"{Name}_{i}.bin");
            InternalDeserialize(path, i, typesDb);
        }
    }

    // Override for non-entity serialization
    public override void Serialize(IGenericWriter writer)
    {
    }

    // Override for non-entity deserialization
    public override void Deserialize(IGenericReader reader)
    {
    }

    public override void PostWorldSave()
    {
        // Release the snapshot so a between-saves resize doesn't pin the old array.
        _entriesSnapshot = null;
        ProcessSafetyQueues();
    }

    public override void PostDeserialize()
    {
        ProcessSafetyQueues();
    }

    public Serial NewEntity
    {
        get
        {
#if THREADGUARD
            if (Thread.CurrentThread != Core.Thread)
            {
                logger.Error(
                    "Attempted to get a new entity serial from the wrong thread!\n{StackTrace}",
                    new StackTrace()
                );
            }
#endif
            var last = (uint)_lastEntitySerial;
            var min = (uint)_minSerial;
            var max = (uint)_maxSerial;

            for (uint i = 0; i < max; i++)
            {
                last++;

                if (last > max)
                {
                    last = min;
                }

                if (FindEntity<T>((Serial)last) == null)
                {
                    return _lastEntitySerial = (Serial)last;
                }
            }

            OutOfMemory($"No serials left to allocate for {Name}");
            return Serial.MinusOne;
        }
    }

    public void AddEntity(T entity)
    {
        var worldState = World.WorldState;
        switch (worldState)
        {
            default: // Not Running
                {
                    throw new Exception($"Added {entity.GetType().Name} before world load.");
                }
            case WorldState.Saving:
                {
                    AppendSafetyLog("add", entity);
                    goto case WorldState.Loading;
                }
            case WorldState.Loading:
            case WorldState.WritingSave:
                {
                    if (_pendingDelete.Remove(entity.Serial))
                    {
                        logger.Warning(
                            "Deleted then added {Entity} during {WorldState} state.",
                            entity.GetType().Name,
                            worldState.ToString()
                        );
                    }

                    _pendingAdd[entity.Serial] = entity;
                    break;
                }
            case WorldState.PendingSave:
            case WorldState.Running:
                {
                    RegisterType(entity.GetType());
                    ref var entityEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(EntitiesBySerial, entity.Serial, out var exists);
                    if (exists)
                    {
                        if (entityEntry == entity)
                        {
                            logger.Error(
                                $"Attempted to add '{{Entity}}' ({{Serial}}) but it already exists in the collection.{Environment.NewLine}{{StackTrace}}",
                                entity.GetType().FullName,
                                entity.Serial,
                                new StackTrace()
                            );
                        }
                        else
                        {
                            logger.Error(
                                $"Attempted to add '{{Entity}}' ({{Serial}}) but found '{{ExistingEntity}}' ({{ExistingSerial}}).{Environment.NewLine}{{StackTrace}}",
                                entity.GetType().FullName,
                                entity.Serial,
                                entityEntry.GetType().FullName,
                                entityEntry.Serial,
                                new StackTrace()
                            );
                        }
                    }
                    else
                    {
                        entityEntry = entity;
                    }
                    break;
                }
        }
    }

    public void RemoveEntity(T entity)
    {
        var worldState = World.WorldState;
        switch (worldState)
        {
            default: // Not Running
                {
                    throw new Exception($"Removed {entity.GetType().Name} before world load.");
                }
            case WorldState.Saving:
                {
                    AppendSafetyLog("delete", entity);
                    goto case WorldState.Loading;
                }
            case WorldState.Loading:
            case WorldState.WritingSave:
                {
                    _pendingAdd.Remove(entity.Serial);
                    _pendingDelete[entity.Serial] = entity;
                    break;
                }
            case WorldState.PendingSave:
            case WorldState.Running:
                {
                    EntitiesBySerial.Remove(entity.Serial);
                    break;
                }
        }
    }

    private void ProcessSafetyQueues()
    {
        foreach (var entity in _pendingAdd.Values)
        {
            AddEntity(entity);
        }

        foreach (var entity in _pendingDelete.Values)
        {
            if (_pendingAdd.ContainsKey(entity.Serial))
            {
                logger.Warning("Entity {Entity} was both pending deletion and addition after save", entity);
            }

            RemoveEntity(entity);
        }

        _pendingAdd.Clear();
        _pendingDelete.Clear();
    }

    private static void AppendSafetyLog(string action, ISerializable entity)
    {
        var message =
            $"Warning: Attempted to {{Action}} {{Entity}} during world save.{Environment.NewLine}This action could cause inconsistent state.{Environment.NewLine}It is strongly advised that the offending scripts be corrected.";

        logger.Information(message, action, entity);

        try
        {
            using var op = new StreamWriter("world-save-errors.log", true);
            op.WriteLine($"{DateTime.UtcNow}\t{message}");
            op.WriteLine(new StackTrace(2).ToString());
            op.WriteLine();
        }
        catch
        {
            // ignored
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Find(Serial serial, bool returnDeleted = false) => FindEntity<T>(serial, returnDeleted);

    public R FindEntity<R>(Serial serial, bool returnDeleted = false) where R : class, T
    {
        switch (World.WorldState)
        {
            default:
                {
                    return null;
                }
            case WorldState.Loading:
            case WorldState.Saving:
            case WorldState.WritingSave:
                {
                    if (returnDeleted && _pendingDelete.TryGetValue(serial, out var entity))
                    {
                        return entity as R;
                    }

                    if (_pendingAdd.TryGetValue(serial, out entity) || EntitiesBySerial.TryGetValue(serial, out entity))
                    {
                        return entity as R;
                    }

                    return null;
                }
            case WorldState.PendingSave:
            case WorldState.Running:
                {
                    return EntitiesBySerial.TryGetValue(serial, out var entity) ? entity as R : null;
                }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void OutOfMemory(string message) => throw new OutOfMemoryException(message);
}
