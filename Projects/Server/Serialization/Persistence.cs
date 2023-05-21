/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Persistence.cs                                                  *
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
using System.Threading.Tasks;

namespace Server;

public static class Persistence
{
    public const int DefaultPriority = 100;

    private static readonly SortedSet<RegistryEntry> _registry = new(new RegistryEntryComparer());

    public static void Register(
        string name,
        Action serializer,
        Action<string> snapshotWriter,
        Action<string, Dictionary<ulong, string>> deserializer,
        Action afterSerialize = null,
        int priority = DefaultPriority
    )
    {
        _registry.Add(
            new RegistryEntry
            {
                Name = name,
                Priority = priority,
                Serialize = serializer,
                AfterSerialize = afterSerialize,
                WriteSnapshot = snapshotWriter,
                Deserialize = deserializer
            }
        );
    }

    public static void Unregister(string name) => _registry.RemoveWhere(entry => entry.Name == name);

    public static void Load(string path)
    {
        var typesDb = LoadTypes(path);

        // This should probably not be parallel since Mobiles must be loaded before Items
        foreach (var entry in _registry)
        {
            entry.Deserialize(path, typesDb);
        }
    }

    private static Dictionary<ulong, string> LoadTypes(string path)
    {
        var db = new Dictionary<ulong, string>();

        string tdbPath = Path.Combine(path, "SerializedTypes.db");
        if (!File.Exists(tdbPath))
        {
            return db;
        }

        using FileStream tdb = new FileStream(tdbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        BinaryReader tdbReader = new BinaryReader(tdb);

        var version = tdbReader.ReadInt32();
        var count = tdbReader.ReadInt32();

        for (var i = 0; i < count; ++i)
        {
            var hash = tdbReader.ReadUInt64();
            var typeName = tdbReader.ReadString();
            db[hash] = typeName;
        }

        return db;
    }

    public static void Serialize()
    {
        Parallel.ForEach(_registry, entry => entry.Serialize());

        // Synchronously run the AfterSerialize on the main game thread
        foreach (var entry in _registry)
        {
            entry.AfterSerialize?.Invoke();
        }
    }

    public static void WriteSnapshot(string path, ConcurrentQueue<Type> types)
    {
        foreach (var entry in _registry)
        {
            entry.WriteSnapshot(path);
        }

        // Dedupe the queue.
        foreach (var type in types)
        {
            _typesSet.Add(type);
        }

        WriteSerializedTypesSnapshot(path, _typesSet);
        _typesSet.Clear();
    }

    private static HashSet<Type> _typesSet = new();

    public static void WriteSerializedTypesSnapshot(string path, HashSet<Type> types)
    {
        string tdbPath = Path.Combine(path, "SerializedTypes.db");
        using var tdb = new BinaryFileWriter(tdbPath, false);

        tdb.Write(0); // version
        tdb.Write(types.Count);

        foreach (var type in types)
        {
            var fullName = type.FullName;
            tdb.Write(HashUtility.ComputeHash64(fullName));
            tdb.Write(fullName);
        }
    }

    public record RegistryEntry
    {
        public string Name { get; init; }
        public int Priority { get; init; }

        // Serializes to memory buffers and run in parallel
        public Action Serialize { get; init; }
        public Action AfterSerialize { get; init; }
        public Action<string> WriteSnapshot { get; init; }
        public Action<string, Dictionary<ulong, string>> Deserialize { get; init; }
    }

    internal class RegistryEntryComparer : IComparer<RegistryEntry>
    {
        public int Compare(RegistryEntry x, RegistryEntry y)
        {
            if (x == y)
            {
                return 0;
            }

            if (x == null)
            {
                return 1;
            }

            if (y == null)
            {
                return -1;
            }

            // First sort by priority
            var cmp = x.Priority.CompareTo(y.Priority);

            // Then alphabetically. We won't allow the same entry (by name) twice in the SortedSet
            return cmp != 0 ? cmp : x.Name?.CompareOrdinal(y.Name) ?? -1;
        }
    }

    public static void TraceException(Exception ex)
    {
        try
        {
            using var op = new StreamWriter("save-errors.log", true);
            op.WriteLine("# {0}", Core.Now);

            op.WriteLine(ex);

            op.WriteLine();
            op.WriteLine();
        }
        catch
        {
            // ignored
        }

        Console.WriteLine(ex);
    }
}
