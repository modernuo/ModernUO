/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PooledEnumeration.cs                                            *
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Server.Collections;
using Server.Items;
using Server.Network;

namespace Server;

public interface IPooledEnumerable<T> : IEnumerable<T>, IDisposable
{
}

public static class PooledEnumeration
{
    public delegate IEnumerable<T> Selector<out T>(Map.Sector sector, Rectangle2D bounds);

    static PooledEnumeration()
    {
        ClientSelector = SelectClients;
        EntitySelector = SelectEntities;
        MultiSelector = SelectMultis;
        MultiTileSelector = SelectMultiTiles;
    }

    public static Selector<NetState> ClientSelector { get; set; }
    public static Selector<IEntity> EntitySelector { get; set; }
    public static Selector<Mobile> MobileSelector { get; set; }
    public static Selector<BaseMulti> MultiSelector { get; set; }
    public static Selector<StaticTile[]> MultiTileSelector { get; set; }

    public static IEnumerable<NetState> SelectClients(Map.Sector s, Rectangle2D bounds)
    {
        var clients = new List<NetState>(s.Clients.Count);
        foreach (var client in s.Clients)
        {
            var m = client.Mobile;

            if (m?.Deleted == false && bounds.Contains(m.Location))
            {
                clients.Add(client);
            }
        }

        return clients;
    }

    public static IEnumerable<IEntity> SelectEntities(Map.Sector s, Rectangle2D bounds)
    {
        var entities = new List<IEntity>(s.Mobiles.Count + s.Items.Count);
        foreach (var mob in s.Mobiles)
        {
            entities.Add(mob);
        }

        foreach (var item in s.Items)
        {
            entities.Add(item);
        }

        return entities;
    }

    public static IEnumerable<BaseMulti> SelectMultis(Map.Sector s, Rectangle2D bounds)
    {
        var entities = new List<BaseMulti>(s.Multis.Count);
        for (int i = s.Multis.Count - 1; i >= 0; --i)
        {
            BaseMulti multi = s.Multis[i];
            if (multi is { Deleted: false } && bounds.Contains(multi.Location))
            {
                entities.Add(multi);
            }
        }
        return entities;
    }

    public static IEnumerable<StaticTile[]> SelectMultiTiles(Map.Sector s, Rectangle2D bounds)
    {
        for (int l = s.Multis.Count - 1; l >= 0; --l)
        {
            BaseMulti o = s.Multis[l];
            if (o?.Deleted != false)
            {
                continue;
            }

            MultiComponentList c = o.Components;

            int x, y, xo, yo;
            StaticTile[] t, r;

            for (x = bounds.Start.X; x < bounds.End.X; x++)
            {
                xo = x - (o.X + c.Min.X);

                if (xo < 0 || xo >= c.Width)
                {
                    continue;
                }

                for (y = bounds.Start.Y; y < bounds.End.Y; y++)
                {
                    yo = y - (o.Y + c.Min.Y);

                    if (yo < 0 || yo >= c.Height)
                    {
                        continue;
                    }

                    t = c.Tiles[xo][yo];

                    if (t.Length <= 0)
                    {
                        continue;
                    }

                    r = new StaticTile[t.Length];

                    for (var i = 0; i < t.Length; i++)
                    {
                        r[i] = t[i];
                        r[i].Z += o.Z;
                    }

                    yield return r;
                }
            }
        }
    }

    public static PooledEnumerable<NetState> GetClients(Map map, Rectangle2D bounds) =>
        PooledEnumerable<NetState>.Instantiate(map, bounds, ClientSelector ?? SelectClients);

    public static PooledEnumerable<IEntity> GetEntities(Map map, Rectangle2D bounds) =>
        PooledEnumerable<IEntity>.Instantiate(map, bounds, EntitySelector ?? SelectEntities);

    public static PooledEnumerable<BaseMulti> GetMultis(Map map, Rectangle2D bounds) =>
        PooledEnumerable<BaseMulti>.Instantiate(map, bounds, MultiSelector ?? SelectMultis);

    public static PooledEnumerable<StaticTile[]> GetMultiTiles(Map map, Rectangle2D bounds) =>
        PooledEnumerable<StaticTile[]>.Instantiate(map, bounds, MultiTileSelector ?? SelectMultiTiles);

    public static IEnumerable<Map.Sector> EnumerateSectors(Map map, Rectangle2D bounds)
    {
        if (map == null || map == Map.Internal)
        {
            yield break;
        }

        var x1 = bounds.Start.X;
        var y1 = bounds.Start.Y;
        var x2 = bounds.End.X;
        var y2 = bounds.End.Y;

        if (!Bound(map, ref x1, ref y1, ref x2, ref y2, out var xSector, out var ySector))
        {
            yield break;
        }

        var index = 0;

        while (NextSector(map, x1, y1, x2, y2, ref index, ref xSector, ref ySector, out var s))
        {
            yield return s;
        }
    }

    public static bool Bound(
        Map map,
        ref int x1,
        ref int y1,
        ref int x2,
        ref int y2,
        out int xSector,
        out int ySector
    )
    {
        if (map == null || map == Map.Internal)
        {
            xSector = ySector = 0;
            return false;
        }

        map.Bound(x1, y1, out x1, out y1);
        map.Bound(x2 - 1, y2 - 1, out x2, out y2);

        x1 >>= Map.SectorShift;
        y1 >>= Map.SectorShift;
        x2 >>= Map.SectorShift;
        y2 >>= Map.SectorShift;

        xSector = x1;
        ySector = y1;

        return true;
    }

    private static bool NextSector(
        Map map,
        int x1,
        int y1,
        int x2,
        int y2,
        ref int index,
        ref int xSector,
        ref int ySector,
        out Map.Sector s
    )
    {
        if (map == null)
        {
            s = null;
            xSector = ySector = 0;
            return false;
        }

        if (map == Map.Internal)
        {
            s = map.InvalidSector;
            xSector = ySector = 0;
            return false;
        }

        if (index++ > 0)
        {
            if (++ySector > y2)
            {
                ySector = y1;

                if (++xSector > x2)
                {
                    xSector = x1;

                    s = map.InvalidSector;
                    return false;
                }
            }
        }

        s = map.GetRealSector(xSector, ySector);
        return true;
    }

    public class NullEnumerable<T> : IPooledEnumerable<T>
    {
        public static readonly NullEnumerable<T> Instance = new();

        private readonly IEnumerable<T> m_Empty = Enumerable.Empty<T>();

        IEnumerator IEnumerable.GetEnumerator() => m_Empty.GetEnumerator();

        public IEnumerator<T> GetEnumerator() => m_Empty.GetEnumerator();

        public void Dispose()
        {
        }
    }

    public sealed class PooledEnumerable<T> : IPooledEnumerable<T>
    {
        private static readonly Queue<PooledEnumerable<T>> _Buffer = new(0x400);

        private bool m_IsDisposed;

        private List<T> m_Pool = new(0x40);

        public PooledEnumerable(IEnumerable<T> pool)
        {
            m_Pool.AddRange(pool);
        }

        public void Dispose()
        {
            if (m_IsDisposed)
            {
                return;
            }

            m_IsDisposed = true;

            m_Pool.Clear();
            m_Pool.Capacity = Math.Max(m_Pool.Capacity, 0x100);

            lock (((ICollection)_Buffer).SyncRoot)
            {
                _Buffer.Enqueue(this);
            }
        }

        ~PooledEnumerable()
        {
            Dispose();
        }

        IEnumerator IEnumerable.GetEnumerator() => m_Pool.GetEnumerator();

        public IEnumerator<T> GetEnumerator() => m_Pool.GetEnumerator();


#pragma warning disable CA1000 // Do not declare static members on generic types
        public static PooledEnumerable<T> Instantiate(
            Map map, Rectangle2D bounds, Selector<T> selector
        )
        {
            PooledEnumerable<T> e = null;

            lock (((ICollection)_Buffer).SyncRoot)
            {
                if (_Buffer.Count > 0)
                {
                    e = _Buffer.Dequeue();
                }
            }

            var pool = EnumerateSectors(map, bounds).SelectMany(s => selector(s, bounds));

            if (e == null)
            {
                return new PooledEnumerable<T>(pool);
            }

            e.m_Pool.AddRange(pool);
            return e;
        }
    }
}
