/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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
    public static T ReadEntity<T>(this IGenericReader reader) where T : class, ISerializable
    {
        Serial serial = reader.ReadSerial();
        var typeT = typeof(T);

        T entity;

        // Add to this list when creating new serializable types
        if (typeof(BaseGuild).IsAssignableFrom(typeT))
        {
            entity = World.FindGuild(serial) as T;
            // If we check for `entity.Deleted` here during deserialization then all guilds are deleted because
            // Deleted -> Disbanded -> No leader, which is the case before deserialization.
            // TODO: Use a deleted flag instead, and actively check for dibanded guilds properly.
        }
        else
        {
            entity = World.FindEntity<IEntity>(serial) as T;
            if (entity?.Deleted == false)
            {
                return entity;
            }
        }

        return entity?.Created <= reader.LastSerialized ? entity : null;
    }

    public static List<T> ReadEntityList<T>(this IGenericReader reader) where T : class, ISerializable
    {
        var count = reader.ReadInt();

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

    public static HashSet<T> ReadEntitySet<T>(this IGenericReader reader) where T : class, ISerializable
    {
        var count = reader.ReadInt();

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
