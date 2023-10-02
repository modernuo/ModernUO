/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializationExtensions.cs                                      *
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
using Server.Guilds;

namespace Server;

public static class SerializationExtensions
{
    private static readonly Dictionary<Type, Func<Serial, bool, bool, ISerializable>> _directFinderTable = new();
    private static readonly Dictionary<Type, Func<Serial, bool, bool, ISerializable>> _searchTable = new();

    public static void RegisterFindEntity(this Type type, Func<Serial, bool, bool, ISerializable> func)
    {
        _searchTable[type] = func;
    }

    public static T ReadEntity<T>(this IGenericReader reader) where T : class, ISerializable
    {
        Serial serial = reader.ReadSerial();
        var typeT = typeof(T);

        T entity;

        // Add to this list when creating new serializable types
        if (typeof(BaseGuild).IsAssignableFrom(typeT))
        {
            // If we check for `entity.Deleted` here during deserialization then all guilds are deleted because
            // Deleted -> Disbanded -> No leader, which is the case before deserialization.
            // TODO: Use a deleted flag instead, and actively check for disbanded guilds properly.
            return World.FindGuild(serial) as T;
        }

        if (typeof(IEntity).IsAssignableFrom(typeT))
        {
            return World.FindEntity<IEntity>(serial, returnPending: false) as T;
        }

        if (_directFinderTable.TryGetValue(typeT, out var finder))
        {
            return finder(serial, false, false) as T;
        }

        Type type = null;
        foreach (var baseType in _searchTable.Keys)
        {
            if (baseType.IsAssignableFrom(typeT))
            {
                type = baseType;
                break;
            }
        }

        if (type == null)
        {
            type = typeT;
            while (true)
            {
                var baseType = type?.BaseType;

                // Find the parent class with ISerializable registered. To do this we break on it's parent class (or object)
                // that doesn't have ISerializable implemented.
                if (baseType?.GetInterface("ISerializable") == null && type?.GetInterface("ISerializable") != null)
                {
                    break;
                }

                type = baseType;
            }

            throw new Exception($"No FindEntity registered for '{type.FullName}'.");
        }

        finder = _searchTable[type];
        _directFinderTable[type] = finder;
        return finder(serial, false, false) as T;
    }

    public static List<T> ReadEntityList<T>(
        this IGenericReader reader,
        bool nullIfEmpty = false
    ) where T : class, ISerializable
    {
        var count = reader.ReadInt();

        if (count == 0 && nullIfEmpty)
        {
            return null;
        }

        var list = new List<T>(count);

        for (var i = 0; i < count; ++i)
        {
            var entity = reader.ReadEntity<T>();
            if (entity != null)
            {
                list.Add(entity);
            }
        }

        return list;
    }

    public static HashSet<T> ReadEntitySet<T>(
        this IGenericReader reader,
        bool nullIfEmpty = false
    ) where T : class, ISerializable
    {
        var count = reader.ReadInt();

        if (count == 0 && nullIfEmpty)
        {
            return null;
        }

        var set = new HashSet<T>(count);

        for (var i = 0; i < count; ++i)
        {
            var entity = reader.ReadEntity<T>();
            if (entity != null)
            {
                set.Add(entity);
            }
        }

        return set;
    }

    public static void Write(this IGenericWriter writer, ISerializable value)
    {
        writer.Write(value?.Deleted != false ? Serial.MinusOne : value.Serial);
    }

    public static void Write<T>(this IGenericWriter writer, ICollection<T> coll) where T : class, ISerializable
    {
        writer.Write(coll.Count);
        foreach (var entry in coll)
        {
            writer.Write(entry);
        }
    }

    public static void Write<T>(
        this IGenericWriter writer, ICollection<T> coll, Action<IGenericWriter, T> action
    ) where T : class, ISerializable
    {
        if (coll == null)
        {
            writer.Write(0);
            return;
        }

        writer.Write(coll.Count);
        foreach (var entry in coll)
        {
            action(writer, entry);
        }
    }

    public static void Write(this IGenericWriter writer, Poison p)
    {
        if (p == null)
        {
            writer.Write(false);
        }
        else
        {
            writer.Write(true);
            writer.Write((byte)p.Level);
        }
    }

    public static Poison ReadPoison(this IGenericReader reader) =>
        reader.ReadBool() ? Poison.GetPoison(reader.ReadByte()) : null;
}
