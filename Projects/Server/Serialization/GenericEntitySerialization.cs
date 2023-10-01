using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Server.Logging;

namespace Server;

public class GenericEntitySerialization<T> where T : class, ISerializable
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GenericEntitySerialization<T>));

    private static string _systemName;
    private static Serial _lastEntitySerial;
    private static readonly Dictionary<Serial, T> _pendingAdd = new();
    private static readonly Dictionary<Serial, T> _pendingDelete = new();
    private static Dictionary<Serial, T> _entitiesBySerial = new();

    public static void Configure(string systemName)
    {
        _systemName = systemName;
        typeof(T).RegisterFindEntity(FindEntity);
        Persistence.Register(_systemName, Serialize, WriteSnapshot, Deserialize);
    }

    internal static void Serialize()
    {
        EntityPersistence.SaveEntities(
            _entitiesBySerial.Values,
            entity => entity.Serialize(World.SerializedTypes)
        );
    }

    internal static void WriteSnapshot(string basePath)
    {
        IIndexInfo<Serial> indexInfo = new EntityTypeIndex(_systemName);
        EntityPersistence.WriteEntities(indexInfo, _entitiesBySerial, basePath,World.SerializedTypes, out _);
    }

    internal static void Deserialize(string path, Dictionary<ulong, string> typesDb)
    {
        IIndexInfo<Serial> indexInfo = new EntityTypeIndex(_systemName);

        _entitiesBySerial = EntityPersistence.LoadIndex(path, indexInfo, typesDb, out List<EntitySpan<T>> entities);

        if (_entitiesBySerial.Count > 0)
        {
            _lastEntitySerial = _entitiesBySerial.Keys.Max();
        }

        EntityPersistence.LoadData(path, indexInfo, typesDb, entities);
    }

    public static Serial NewEntity
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

            for (uint i = 0; i < uint.MaxValue; i++)
            {
                last++;

                if (FindEntity<T>(last) == null)
                {
                    return _lastEntitySerial = last;
                }
            }

            OutOfMemory("No serials left to allocate for BOBEntries");
            return Serial.MinusOne;
        }
    }

    public static void AddEntity(T entity)
    {
        var worldState = World.WorldState;
        switch (worldState)
        {
            default: // Not Running
                {
                    throw new Exception($"Added {entity.GetType().Name} before world load.");
                }
            case WorldState.Saving:
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
            case WorldState.Running:
                {
                    ref var entityEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(_entitiesBySerial, entity.Serial, out bool exists);
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

    public static void RemoveEntity(T entity)
    {
        var worldState = World.WorldState;
        switch (worldState)
        {
            default: // Not Running
                {
                    throw new Exception($"Removed {entity.GetType().Name} before world load.");
                }
            case WorldState.Saving:
            case WorldState.Loading:
            case WorldState.WritingSave:
                {
                    _pendingAdd.Remove(entity.Serial);
                    _pendingDelete[entity.Serial] = entity;
                    break;
                }
            case WorldState.Running:
                {
                    _entitiesBySerial.Remove(entity.Serial);
                    break;
                }
        }
    }

    public static R FindEntity<R>(Serial serial) where R : class, T => FindEntity<R>(serial, false);

    public static R FindEntity<R>(Serial serial, bool returnDeleted) where R : class, T
    {
        switch (World.WorldState)
        {
            default: return null;
            case WorldState.Loading:
            case WorldState.Saving:
            case WorldState.WritingSave:
                {
                    if (returnDeleted && _pendingDelete.TryGetValue(serial, out var entity))
                    {
                        return entity as R;
                    }

                    if (!_pendingAdd.TryGetValue(serial, out entity) && !_entitiesBySerial.TryGetValue(serial, out entity))
                    {
                        return null;
                    }

                    return !entity.Deleted || returnDeleted ? entity as R : null;
                }
            case WorldState.Running:
                {
                    return _entitiesBySerial.TryGetValue(serial, out var entity)
                           && (!entity.Deleted || returnDeleted) ? entity as R : null;
                }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void OutOfMemory(string message) => throw new OutOfMemoryException(message);
}
