/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ISerializableExtensions.cs                                      *
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
using System.Runtime.CompilerServices;

namespace Server;

public static class ISerializableExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MarkDirty(this ISerializable entity)
    {
        if (entity != null)
        {
            entity.SavePosition = -1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Delete(this ISerializable entity, IEntity toDelete)
    {
        toDelete?.Delete();
        entity.MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<T>(this ISerializable entity, ICollection<T> list, T value)
    {
        list.Add(value);
        entity.MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<K, V>(this ISerializable entity, IDictionary<K, V> dict, K key, V value)
    {
        dict[key] = value;
        entity.MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Insert<T>(this ISerializable entity, IList<T> list, T value, int index)
    {
        list.Insert(index, value);
        entity.MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove<T>(this ISerializable entity, ICollection<T> list, T value)
    {
        if (list.Remove(value))
        {
            entity.MarkDirty();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove<K, V>(this ISerializable entity, IDictionary<K, V> dict, K key, out V value)
    {
        if (dict.Remove(key, out value))
        {
            entity.MarkDirty();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveAt<T>(this ISerializable entity, IList<T> list, int index)
    {
        list.RemoveAt(index);
        entity.MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear<T>(this ISerializable entity, ICollection<T> list)
    {
        list.Clear();
        entity.MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Stop(this ISerializable entity, Timer timer)
    {
        timer?.Stop();
        entity.MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Start(this ISerializable entity, Timer timer)
    {
        timer?.Start();
        entity.MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Restart(this ISerializable entity, Timer timer, TimeSpan delay, TimeSpan interval)
    {
        if (timer != null)
        {
            timer.Stop();
            timer.Delay = delay;
            timer.Interval = interval;
            timer.Start();
            entity.MarkDirty();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<T>(this ISerializable entity, ref List<T> list, T value)
    {
        Utility.Add(ref list, value);
        entity.MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<K, V>(this ISerializable entity, ref Dictionary<K, V> dict, K key, V value)
    {
        Utility.Add(ref dict, key, value);
        entity.MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove<T>(this ISerializable entity, ref List<T> list, T value)
    {
        if (Utility.Remove(ref list, value))
        {
            entity.MarkDirty();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear<T>(this ISerializable entity, ref List<T> list)
    {
        Utility.Clear(ref list);
        entity.MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear<T>(this ISerializable entity, ref HashSet<T> set)
    {
        Utility.Clear(ref set);
        entity.MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear<K, V>(this ISerializable entity, ref Dictionary<K, V> dict)
    {
        Utility.Clear(ref dict);
        entity.MarkDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Stop(this ISerializable entity, ref Timer timer)
    {
        timer?.Stop();
        timer = null;
        entity.MarkDirty();
    }
}
