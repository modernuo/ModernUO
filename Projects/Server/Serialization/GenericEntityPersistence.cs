/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
    public void DeserializeIndexes(string savePath, Dictionary<ulong, string> typesDb);
}

public class GenericEntityPersistence<T> : Persistence, IGenericEntityPersistence where T : class, ISerializable
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GenericEntityPersistence<T>));

    // Support legacy split file serialization
    private static Dictionary<int, List<EntitySpan<T>>> _entities;

    private readonly string _name;
    private readonly Serial _minSerial;
    private readonly Serial _maxSerial;
    private Serial _lastEntitySerial;
    private readonly Dictionary<Serial, T> _pendingAdd = new();
    private readonly Dictionary<Serial, T> _pendingDelete = new();

    public Dictionary<Serial, T> EntitiesBySerial { get; } = new();

    public GenericEntityPersistence(string name, int priority, uint minSerial, uint maxSerial) : this(
        name,
        priority,
        (Serial)minSerial,
        (Serial)maxSerial
    )
    {
    }

    public GenericEntityPersistence(string name, int priority, Serial minSerial, Serial maxSerial) : base(priority)
    {
        _name = name;
        _minSerial = minSerial;
        _maxSerial = maxSerial;
        _lastEntitySerial = minSerial - 1;
        typeof(T).RegisterFindEntity(Find);
    }

    public override void WriteSnapshot(string savePath, HashSet<Type> typeSet)
    {
        var dir = Path.Combine(savePath, _name);
        PathUtility.EnsureDirectory(dir);

        var threads = World._threadWorkers;

        using var binFs = new FileStream(Path.Combine(dir, $"{_name}.bin"), FileMode.Create, FileAccess.Write, FileShare.None);
        using var idxFs = new FileStream(Path.Combine(dir, $"{_name}.idx"), FileMode.Create);
        using var idx = new MemoryMapFileWriter(idxFs, 1024 * 1024, typeSet); // 1MB

        idx.Write(3); // Version
        idx.Write(EntitiesBySerial.Values.Count);
        var binPosition = 0L;

        foreach (var e in EntitiesBySerial.Values)
        {
            var thread = e.SerializedThread;
            var heapStart = e.SerializedPosition;
            var heapLength = e.SerializedLength;

            idx.Write(e.GetType());
            idx.Write(e.Serial);
            idx.Write(e.Created.Ticks);
            idx.Write(binPosition);
            idx.Write(heapLength);

            try
            {
                binFs.Write(threads[thread].GetHeap(heapStart, heapLength));
            }
            catch (Exception error)
            {
                Console.WriteLine("Error writing entity: {0} (Thread: {1} - {2} {3})\n{4}", e, thread, heapStart, heapLength, error);
            }

            binPosition += heapLength;
        }
    }

    public override void Serialize()
    {
        foreach (var entity in EntitiesBySerial.Values)
        {
            World.PushToCache(entity);
        }
    }

    private static ConstructorInfo GetConstructorFor(string typeName, Type t, Type[] constructorTypes)
    {
        if (t?.IsAbstract != false)
        {
            Console.WriteLine("failed");

            var issue = t?.IsAbstract == true ? "marked abstract" : "not found";

            Console.Write($"Error: Type '{typeName}' was {issue}. Delete all of those types? (y/n): ");

            if (Console.ReadLine().InsensitiveEquals("y"))
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
    private unsafe Dictionary<int, ConstructorInfo> ReadTypes(string savePath)
    {
        string typesPath = Path.Combine(savePath, _name, $"{_name}.tdb");
        if (!File.Exists(typesPath))
        {
            return null;
        }

        Type[] ctorArguments = [typeof(Serial)];

        using var mmf = MemoryMappedFile.CreateFromFile(typesPath, FileMode.Open);
        using var accessor = mmf.CreateViewStream();

        byte* ptr = null;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        var dataReader = new UnmanagedDataReader(ptr, accessor.Length);

        var count = dataReader.ReadInt();
        var types = new Dictionary<int, ConstructorInfo>(count);

        for (var i = 0; i < count; ++i)
        {
            // Legacy didn't have the null flag check
            var typeName = dataReader.ReadStringRaw();
            types.Add(i, GetConstructorFor(typeName, AssemblyHandler.FindTypeByName(typeName), ctorArguments));
        }

        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        return types;
    }

    public virtual void DeserializeIndexes(string savePath, Dictionary<ulong, string> typesDb)
    {
        string indexPath = Path.Combine(savePath, _name, $"{_name}.idx");

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
            var path = Path.Combine(savePath, _name, $"{_name}_{index}.idx");
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

        Dictionary<int, ConstructorInfo> ctors = [];

        if (version < 2)
        {
            ctors = ReadTypes(Path.GetDirectoryName(filePath));
        }

        if (typesDb == null && ctors == null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var ctorArgs = new object[1];
        Type[] ctorArguments = [typeof(Serial)];

        var count = dataReader.ReadInt();

        for (var i = 0; i < count; ++i)
        {
            ConstructorInfo ctor;
            // Version 2 & 3 with SerializedTypes.db
            if (version >= 2)
            {
                var flag = dataReader.ReadByte();
                if (flag != 2)
                {
                    throw new Exception($"Invalid type flag, expected 2 but received {flag}.");
                }

                var hash = dataReader.ReadULong();
                typesDb!.TryGetValue(hash, out var typeName);
                ctor = GetConstructorFor(typeName, AssemblyHandler.FindTypeByHash(hash), ctorArguments);
            }
            else
            {
                ctor = ctors?[dataReader.ReadInt()];
            }

            Serial serial = (Serial)dataReader.ReadUInt();
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

        accessor.SafeMemoryMappedViewHandle.ReleasePointer();

        if (EntitiesBySerial.Count > 0)
        {
            _lastEntitySerial = EntitiesBySerial.Keys.Max();
        }
    }

    public override void Deserialize(string savePath, Dictionary<ulong, string> typesDb)
    {
        string dataPath = Path.Combine(savePath, _name, $"{_name}.bin");
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
    }

    private static unsafe void InternalDeserialize(string filePath, int index, Dictionary<ulong, string> typesDb)
    {
        using var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);
        using var accessor = mmf.CreateViewStream();

        byte* ptr = null;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        UnmanagedDataReader dataReader = new UnmanagedDataReader(ptr, accessor.Length, typesDb);
        var deleteAllFailures = false;

        foreach (var entry in _entities[index])
        {
            T t = entry.Entity;

            if (entry.Length == 0)
            {
                t?.Delete();
                continue;
            }

            // Skip this entry
            if (t == null)
            {
                dataReader.Seek(entry.Length, SeekOrigin.Current);
                continue;
            }

            string error;

            try
            {
                var pos = dataReader.Position;
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
                    var pressedKey = Console.ReadLine();

                    if (pressedKey.InsensitiveEquals("a"))
                    {
                        deleteAllFailures = true;
                    }
                    else if (!pressedKey.InsensitiveEquals("y"))
                    {
                        throw new Exception("Deserialization failed.");
                    }
                }

                t.Delete();
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

        var folderPath = Path.Combine(savePath, _name);

        foreach (var i in _entities.Keys)
        {
            var path = Path.Combine(folderPath, $"{_name}_{i}.bin");
            InternalDeserialize(path, i, typesDb);
        }
    }

    public override void PostWorldSave()
    {
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

            OutOfMemory($"No serials left to allocate for {_name}");
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
                        logger.Warning("Deleted then added {Entity} during {WorldState} state.", entity.GetType().Name, worldState.ToString());
                    }

                    _pendingAdd[entity.Serial] = entity;
                    break;
                }
            case WorldState.PendingSave:
            case WorldState.Running:
                {
                    ref var entityEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(EntitiesBySerial, entity.Serial, out bool exists);
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

        _pendingAdd.Clear();

        foreach (var entity in _pendingDelete.Values)
        {
            if (_pendingAdd.ContainsKey(entity.Serial))
            {
                logger.Warning("Entity {Entity} was both pending deletion and addition after save", entity);
            }

            RemoveEntity(entity);
        }

        _pendingDelete.Clear();
    }

    private void AppendSafetyLog(string action, ISerializable entity)
    {
        var message =
            $"Warning: Attempted to {{Action}} {{Entity}} during world save.{Environment.NewLine}This action could cause inconsistent state.{Environment.NewLine}It is strongly advised that the offending scripts be corrected.";

        logger.Information(message, action, entity);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Find(Serial serial) => FindEntity<T>(serial, false, false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Find(Serial serial, bool returnDeleted) => FindEntity<T>(serial, returnDeleted, false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Find(Serial serial, bool returnDeleted, bool returnPending) => FindEntity<T>(serial, returnDeleted, returnPending);

    public R FindEntity<R>(Serial serial) where R : class, T => FindEntity<R>(serial, false, false);

    public R FindEntity<R>(Serial serial, bool returnDeleted, bool returnPending) where R : class, T
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
                    if (returnDeleted && returnPending && _pendingDelete.TryGetValue(serial, out var entity))
                    {
                        return entity as R;
                    }

                    if (returnPending && _pendingAdd.TryGetValue(serial, out entity) ||
                        EntitiesBySerial.TryGetValue(serial, out entity))
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
