/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EntityPersistence.cs                                            *
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
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;

namespace Server;

public static class EntityPersistence
{
    public static void WriteEntities<I, T>(
        IIndexInfo<I> indexInfo,
        Dictionary<I, T> entities,
        string savePath,
        ConcurrentQueue<Type> types,
        out Dictionary<string, int> counts
    ) where T : class, ISerializable
    {
        counts = new Dictionary<string, int>();

        var typeName = indexInfo.TypeName;

        var path = Path.Combine(savePath, typeName);

        PathUtility.EnsureDirectory(path);

        string idxPath = Path.Combine(path, $"{typeName}.idx");
        string binPath = Path.Combine(path, $"{typeName}.bin");

        using var idx = new BinaryFileWriter(idxPath, false, types);
        using var bin = new BinaryFileWriter(binPath, true, types);

        idx.Write(3); // Version
        idx.Write(entities.Count);
        foreach (var e in entities.Values)
        {
            long start = bin.Position;

            var t = e.GetType();
            idx.Write(t);
            idx.Write(e.Serial);
            idx.Write(e.Created.Ticks);
            idx.Write(start);

            e.SerializeTo(bin);

            idx.Write((int)(bin.Position - start));

            var type = e.GetType().FullName;
            if (type != null)
            {
                counts[type] = (counts.TryGetValue(type, out var count) ? count : 0) + 1;
            }
        }
    }

    public static Dictionary<I, T> LoadIndex<I, T>(
        string path,
        IIndexInfo<I> indexInfo,
        Dictionary<ulong, string> serializedTypes,
        out List<EntitySpan<T>> entities
    ) where T : class, ISerializable
    {
        var map = new Dictionary<I, T>();
        object[] ctorArgs = new object[1];

        var indexType = indexInfo.TypeName;

        string indexPath = Path.Combine(path, indexType, $"{indexType}.idx");

        entities = new List<EntitySpan<T>>();

        if (!File.Exists(indexPath))
        {
            return map;
        }

        using FileStream idx = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        BinaryReader idxReader = new BinaryReader(idx);

        var version = idxReader.ReadInt32();
        int count = idxReader.ReadInt32();

        var ctorArguments = new[] { typeof(I) };
        List<ConstructorInfo> types;

        string typesPath = Path.Combine(path, indexType, $"{indexType}.tdb");
        if (File.Exists(typesPath))
        {
            using FileStream tdb = new FileStream(typesPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryReader tdbReader = new BinaryReader(tdb);
            types = ReadTypes(tdbReader, ctorArguments);
            tdbReader.Close();
        }
        else
        {
            types = null;
        }

        // We must have a typeDb from SerializedTypes.db, or a tdb file
        if (serializedTypes == null && types == null)
        {
            return map;
        }

        var now = DateTime.UtcNow;

        for (int i = 0; i < count; ++i)
        {
            ConstructorInfo ctor;
            if (version >= 2)
            {
                var flag = idxReader.ReadByte();
                if (flag != 2)
                {
                    throw new Exception($"Invalid type flag, expected 2 but received {flag}.");
                }

                var hash = idxReader.ReadUInt64();
                serializedTypes!.TryGetValue(hash, out var typeName);
                ctor = GetConstructorFor(typeName, AssemblyHandler.FindTypeByHash(hash), ctorArguments);
            }
            else
            {
                ctor = types?[idxReader.ReadInt32()];
            }

            var serial = idxReader.ReadUInt32();
            var created = version == 0 ? now : new DateTime(idxReader.ReadInt64(), DateTimeKind.Utc);
            if (version is > 0 and < 3)
            {
                idxReader.ReadInt64(); // LastSerialized
            }
            var pos = idxReader.ReadInt64();
            var length = idxReader.ReadInt32();

            if (ctor == null)
            {
                continue;
            }

            I indexer = indexInfo.CreateIndex(serial);

            ctorArgs[0] = indexer;

            if (ctor.Invoke(ctorArgs) is T entity)
            {
                entity.Created = created;
                entities.Add(new EntitySpan<T>(entity, pos, length));
                map[indexer] = entity;
            }
        }

        idxReader.Close();
        entities.TrimExcess();

        return map;
    }

    public static void LoadData<I, T>(
        string path,
        IIndexInfo<I> indexInfo,
        Dictionary<ulong, string> serializedTypes,
        List<EntitySpan<T>> entities
    ) where T : class, ISerializable
    {
        var indexType = indexInfo.TypeName;

        string dataPath = Path.Combine(path, indexType, $"{indexType}.bin");

        if (!File.Exists(dataPath) || new FileInfo(dataPath).Length == 0)
        {
            return;
        }

        using var mmf = MemoryMappedFile.CreateFromFile(dataPath, FileMode.Open);
        using var stream = mmf.CreateViewStream();
        BufferReader br = null;

        var deleteAllFailures = false;

        foreach (var entry in entities)
        {
            T t = entry.Entity;

            var position = entry.Position;
            stream.Seek(position, SeekOrigin.Begin);

            // Skip this entry
            if (t == null)
            {
                continue;
            }

            if (entry.Length == 0)
            {
                t.Delete();
                continue;
            }

            var buffer = GC.AllocateUninitializedArray<byte>(entry.Length);
            if (br == null)
            {
                br = new BufferReader(buffer, serializedTypes);
            }
            else
            {
                br.Reset(buffer, out _);
            }

            stream.Read(buffer.AsSpan());
            string error;

            try
            {
                t.Deserialize(br);

                error = br.Position != entry.Length
                    ? $"Serialized object was {entry.Length} bytes, but {br.Position} bytes deserialized"
                    : null;
            }
            catch (Exception e)
            {
                error = e.ToString();
            }

            if (error == null)
            {
                t.InitializeSaveBuffer(buffer, World.SerializedTypes);
            }
            else
            {
                Console.WriteLine($"***** Bad deserialize of {t.GetType()} *****");
                Console.WriteLine(error);

                ConsoleKey pressedKey;

                if (!deleteAllFailures)
                {
                    Console.WriteLine("Delete the object and continue? (y/n/a)");
                    pressedKey = Console.ReadKey(true).Key;

                    if (pressedKey == ConsoleKey.A)
                    {
                        deleteAllFailures = true;
                    }
                    else if (pressedKey != ConsoleKey.Y)
                    {
                        throw new Exception("Deserialization failed.");
                    }
                }

                t.Delete();
            }
        }
    }

    private static ConstructorInfo GetConstructorFor(string typeName, Type t, Type[] constructorTypes)
    {
        if (t?.IsAbstract != false)
        {
            Console.WriteLine("failed");

            var issue = t?.IsAbstract == true ? "marked abstract" : "not found";

            Console.WriteLine($"Error: Type '{typeName}' was {issue}. Delete all of those types? (y/n)");

            if (Console.ReadKey(true).Key == ConsoleKey.Y)
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
    private static List<ConstructorInfo> ReadTypes(BinaryReader tdbReader, Type[] ctorArguments)
    {
        var count = tdbReader.ReadInt32();

        var types = new List<ConstructorInfo>(count);

        for (var i = 0; i < count; ++i)
        {
            var typeName = tdbReader.ReadString();
            var t = AssemblyHandler.FindTypeByFullName(typeName, false);
            var ctor = GetConstructorFor(typeName, t, ctorArguments);
            types.Add(ctor);
        }

        return types;
    }

    private static void SerializeTo(this ISerializable entity, IGenericWriter writer)
    {
        var saveBuffer = entity.SaveBuffer;

        // If nothing was serialized we expect the object to be deleted on deserialization
        if (saveBuffer.Position == 0)
        {
            return;
        }

        // Resize to the exact size
        saveBuffer.Resize((int)saveBuffer.Position);

        // Write that amount
        writer.Write(saveBuffer.Buffer);
    }
}
