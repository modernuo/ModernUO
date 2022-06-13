/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Server
{
    public class Persistence
    {
        public const int DefaultPriority = 100;

        private static readonly SortedSet<RegistryEntry> _registry = new(new RegistryEntryComparer());

        public static void Register(
            string name,
            Action serializer,
            Action<string> snapshotWriter,
            Action<string> deserializer,
            int priority = DefaultPriority
        )
        {
            _registry.Add(
                new RegistryEntry
                {
                    Name = name,
                    Priority = priority,
                    Serialize = serializer,
                    WriteSnapshot = snapshotWriter,
                    Deserialize = deserializer
                }
            );
        }

        public static void Unregister(string name) => _registry.RemoveWhere(entry => entry.Name == name);

        public static void Load(string path)
        {
            // This should probably not be parallel since Mobiles must be loaded before Items
            foreach (var entry in _registry)
            {
                entry.Deserialize(path);
            }
        }

        public static void Serialize()
        {
            Parallel.ForEach(_registry, entry => entry.Serialize());
        }

        public static void WriteSnapshot(string path)
        {
            foreach (var entry in _registry)
            {
                entry.WriteSnapshot(path);
            }
        }

        public record RegistryEntry
        {
            public string Name { get; init; }
            public int Priority { get; init; }
            public Action Serialize { get; init; } // Serializing to memory buffers
            public Action<string> WriteSnapshot { get; init; }
            public Action<string> Deserialize { get; init; }
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
}
