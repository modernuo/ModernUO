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
using System.Linq;
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

    private static List<EntitySpan<T>> _entities;

    private string _name;
    private Serial _lastEntitySerial;
    private readonly Dictionary<Serial, T> _pendingAdd = new();
    private readonly Dictionary<Serial, T> _pendingDelete = new();
    private uint _minSerial;
    private uint _maxSerial;

    public Dictionary<Serial, T> EntitiesBySerial { get; private set; } = new();

    public GenericEntityPersistence(string name, int priority, uint minSerial, uint maxSerial) : base(priority)
    {
        _name = name;
        _minSerial = minSerial;
        _maxSerial = maxSerial;
        _lastEntitySerial = (Serial)(minSerial - 1);
        typeof(T).RegisterFindEntity(Find);
    }

    public override void Serialize()
    {
        foreach (var entity in EntitiesBySerial.Values)
        {
            World.PushToCache(entity);
        }
    }

    public override void WriteSnapshot(string basePath)
    {
        IIndexInfo<Serial> indexInfo = new EntityTypeIndex(_name);
        EntityPersistence.WriteEntities(indexInfo, EntitiesBySerial, basePath,World.SerializedTypes, out _);
    }

    public virtual void DeserializeIndexes(string savePath, Dictionary<ulong, string> typesDb)
    {
        IIndexInfo<Serial> indexInfo = new EntityTypeIndex(_name);

        EntitiesBySerial = EntityPersistence.LoadIndex(savePath, indexInfo, typesDb, out _entities);

        if (EntitiesBySerial.Count > 0)
        {
            _lastEntitySerial = EntitiesBySerial.Keys.Max();
        }
    }

    public override void Deserialize(string savePath, Dictionary<ulong, string> typesDb)
    {
        IIndexInfo<Serial> indexInfo = new EntityTypeIndex(_name);
        EntityPersistence.LoadData(savePath, indexInfo, typesDb, _entities);
        _entities = null;
    }

    public override void PostSerialize()
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
            var last = _lastEntitySerial;
            var max = (Serial)_maxSerial;

            for (uint i = 0; i < _maxSerial; i++)
            {
                last++;

                if (last > max)
                {
                    last = (Serial)_minSerial;
                }

                if (FindEntity<T>(last) == null)
                {
                    return _lastEntitySerial = last;
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
                    goto case WorldState.WritingSave;
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
                                $"Attempted to add '{{Entity}}' ({{Serial}}) to World.Items but it already exists in the collection.{Environment.NewLine}{{StackTrace}}",
                                entity.GetType().FullName,
                                entity.Serial,
                                new StackTrace()
                            );
                        }
                        else
                        {
                            logger.Error(
                                $"Attempted to add '{{Entity}}' ({{Serial}}) to World.Items but found '{{ExistingEntity}}' ({{ExistingSerial}}).{Environment.NewLine}{{StackTrace}}",
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
                    goto case WorldState.WritingSave;
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
            default: return null;
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
