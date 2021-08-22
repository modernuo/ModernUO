/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Server
{
    public static class EntityPersistence
    {
        public static void WriteEntities<I, T>(
            IIndexInfo<I> indexInfo,
            Dictionary<I, T> entities,
            List<Type> types,
            string savePath,
            out Dictionary<string, int> counts
        ) where T : class, ISerializable
        {
            counts = new Dictionary<string, int>();

            var typeName = indexInfo.TypeName;

            var path = Path.Combine(savePath, typeName);

            AssemblyHandler.EnsureDirectory(path);

            string idxPath = Path.Combine(path, $"{typeName}.idx");
            string tdbPath = Path.Combine(path, $"{typeName}.tdb");
            string binPath = Path.Combine(path, $"{typeName}.bin");

            using var idx = new BinaryFileWriter(idxPath, false);
            using var tdb = new BinaryFileWriter(tdbPath, false);
            using var bin = new BinaryFileWriter(binPath, true);

            idx.Write(entities.Count);
            foreach (var e in entities.Values)
            {
                long start = bin.Position;

                idx.Write(e.TypeRef);
                idx.Write(e.Serial);
                idx.Write(start);

                e.SerializeTo(bin);

                idx.Write((int)(bin.Position - start));

                var type = e.GetType().FullName;
                if (type != null)
                {
                    counts[type] = (counts.TryGetValue(type, out var count) ? count : 0) + 1;
                }
            }

            tdb.Write(types.Count);
            for (int i = 0; i < types.Count; ++i)
            {
                tdb.Write(types[i].FullName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SaveEntities<T>(
            IEnumerable<T> list,
            Action<T> serializer
        ) where T : class, ISerializable => Parallel.ForEach(list, serializer);

        public static Dictionary<I, T> LoadIndex<I, T>(
            string path,
            IIndexInfo<I> indexInfo,
            out List<EntityIndex<T>> entities
        ) where T : class, ISerializable
        {
            var map = new Dictionary<I, T>();
            object[] ctorArgs = new object[1];

            var indexType = indexInfo.TypeName;

            string indexPath = Path.Combine(path, indexType, $"{indexType}.idx");
            string typesPath = Path.Combine(path, indexType, $"{indexType}.tdb");

            entities = new List<EntityIndex<T>>();

            if (!File.Exists(indexPath) || !File.Exists(typesPath))
            {
                return map;
            }

            using FileStream idx = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryReader idxReader = new BinaryReader(idx);

            using FileStream tdb = new FileStream(typesPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryReader tdbReader = new BinaryReader(tdb);

            List<Tuple<ConstructorInfo, string>> types = ReadTypes<I>(tdbReader);

            var count = idxReader.ReadInt32();

            for (int i = 0; i < count; ++i)
            {
                var typeID = idxReader.ReadInt32();
                var number = idxReader.ReadUInt32();
                var pos = idxReader.ReadInt64();
                var length = idxReader.ReadInt32();

                Tuple<ConstructorInfo, string> objs = types[typeID];

                if (objs == null)
                {
                    continue;
                }

                ConstructorInfo ctor = objs.Item1;
                I indexer = indexInfo.CreateIndex(number);

                ctorArgs[0] = indexer;

                if (ctor.Invoke(ctorArgs) is T t)
                {
                    entities.Add(new EntityIndex<T>(t, typeID, pos, length));
                    map[indexer] = t;
                }
            }

            tdbReader.Close();
            idxReader.Close();

            return map;
        }

        public static void LoadData<I, T>(
            string path,
            IIndexInfo<I> indexInfo,
            List<EntityIndex<T>> entities
        ) where T : class, ISerializable
        {
            var indexType = indexInfo.TypeName;

            string dataPath = Path.Combine(path, indexType, $"{indexType}.bin");

            if (!File.Exists(dataPath))
            {
                return;
            }

            using FileStream bin = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            BufferReader br = null;

            var deleteAllFailures = false;

            foreach (var entry in entities)
            {
                T t = entry.Entity;

                var position = entry.Position;
                bin.Seek(position, SeekOrigin.Begin);

                // Skip this entry
                if (t == null)
                {
                    continue;
                }

                var buffer = GC.AllocateUninitializedArray<byte>(entry.Length);
                if (br == null)
                {
                    br = new BufferReader(buffer);
                }
                else
                {
                    br.Reset(buffer, out _);
                }

                bin.Read(buffer.AsSpan());
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
                    t.InitializeSaveBuffer(buffer);
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

        private static List<Tuple<ConstructorInfo, string>> ReadTypes<I>(BinaryReader tdbReader)
        {
            var constructorTypes = new[] { typeof(I) };

            var count = tdbReader.ReadInt32();

            var types = new List<Tuple<ConstructorInfo, string>>(count);

            for (var i = 0; i < count; ++i)
            {
                var typeName = tdbReader.ReadString();

                var t = AssemblyHandler.FindTypeByFullName(typeName, false);

                if (t?.IsAbstract != false)
                {
                    Console.WriteLine("failed");

                    var issue = t?.IsAbstract == true ? "marked abstract" : "not found";

                    Console.WriteLine($"Error: Type '{typeName}' was {issue}. Delete all of those types? (y/n)");

                    if (Console.ReadKey(true).Key == ConsoleKey.Y)
                    {
                        types.Add(null);
                        Console.WriteLine("Loading...");
                        continue;
                    }

                    Console.WriteLine("Types will not be deleted. An exception will be thrown.");

                    throw new Exception($"Bad type '{typeName}'");
                }

                var ctor = t.GetConstructor(constructorTypes);

                if (ctor != null)
                {
                    types.Add(new Tuple<ConstructorInfo, string>(ctor, typeName));
                }
                else
                {
                    throw new Exception($"Type '{t}' does not have a serialization constructor");
                }
            }

            return types;
        }

        private static void SerializeTo(this ISerializable entity, IGenericWriter writer)
        {
            var saveBuffer = entity.SaveBuffer;

            // Resize to the exact size
            saveBuffer.Resize((int)saveBuffer.Position);

            // Write that amount
            writer.Write(saveBuffer.Buffer);
        }
    }
}
