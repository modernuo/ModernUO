/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Map.MultiEnumerator.cs                                          *
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
using System.Runtime.InteropServices;
using Server.Items;

namespace Server;

public partial class Map
{
    private static SectorMultiValueLinkList _emptyMultiLinkList = new();
    public static ref readonly SectorMultiValueLinkList EmptyMultiLinkList => ref _emptyMultiLinkList;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiSectorEnumerable<BaseMulti> GetMultisInSector(Point3D p) => GetMultisInSector<BaseMulti>(p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiSectorEnumerable<BaseMulti> GetMultisInSector(Point2D p) => GetMultisInSector<BaseMulti>(p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiSectorEnumerable<BaseMulti> GetMultisInSector(int x, int y) => GetMultisInSector<BaseMulti>(x, y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiSectorEnumerable<T> GetMultisInSector<T>(Point3D p) where T : BaseMulti =>
        new(this, new Point2D(p.X, p.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiSectorEnumerable<T> GetMultisInSector<T>(Point2D p) where T : BaseMulti => new(this, p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiSectorEnumerable<T> GetMultisInSector<T>(int x, int y) where T : BaseMulti => new(this, new Point2D(x, y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiBoundsEnumerable<BaseMulti> GetMultisInRange(Point3D p) => GetMultisInRange<BaseMulti>(p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiBoundsEnumerable<BaseMulti> GetMultisInRange(Point3D p, int range) => GetMultisInRange<BaseMulti>(p, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiBoundsEnumerable<T> GetMultisInRange<T>(Point3D p) where T : BaseMulti => GetMultisInRange<T>(p, Core.GlobalMaxUpdateRange);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiBoundsEnumerable<T> GetMultisInRange<T>(Point3D p, int range) where T : BaseMulti =>
        GetMultisInRange<T>(p.m_X, p.m_Y, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiBoundsEnumerable<BaseMulti> GetMultisInRange(Point2D p) => GetMultisInRange<BaseMulti>(p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiBoundsEnumerable<BaseMulti> GetMultisInRange(Point2D p, int range) => GetMultisInRange<BaseMulti>(p, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiBoundsEnumerable<T> GetMultisInRange<T>(Point2D p) where T : BaseMulti => GetMultisInRange<T>(p, Core.GlobalMaxUpdateRange);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiBoundsEnumerable<T> GetMultisInRange<T>(Point2D p, int range) where T : BaseMulti =>
        GetMultisInRange<T>(p.m_X, p.m_Y, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiBoundsEnumerable<T> GetMultisInRange<T>(int x, int y, int range) where T : BaseMulti =>
        GetMultisInBounds<T>(new Rectangle2D(x - range, y - range, range * 2 + 1, range * 2 + 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiBoundsEnumerable<BaseMulti> GetMultisInBounds(Rectangle2D bounds, bool makeBoundsInclusive = false) =>
        GetMultisInBounds<BaseMulti>(bounds, makeBoundsInclusive);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiBoundsEnumerable<T> GetMultisInBounds<T>(Rectangle2D bounds, bool makeBoundsInclusive = false) where T : BaseMulti =>
        new(this, bounds, makeBoundsInclusive);

    public ref struct MultiSectorEnumerable<T>(Map map, Point2D loc) where T : BaseMulti
    {
        public static MultiSectorEnumerable<T> Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultiSectorEnumerator<T> GetEnumerator() => new(map, loc);
    }

    public ref struct MultiSectorEnumerator<T> where T : BaseMulti
    {
        private readonly Span<BaseMulti> _list;
        private int _index;
        private T _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultiSectorEnumerator(Map map, Point2D loc)
        {
            _list = map == null
                ? Span<BaseMulti>.Empty
                : CollectionsMarshal.AsSpan(map.GetSector(loc.m_X, loc.m_Y).Multis);

            _index = 0;
            _current = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while ((uint)_index < (uint)_list.Length)
            {
                var current = _list[_index++];
                if (current is T { Deleted: false } o)
                {
                    _current = o;
                    return true;
                }
            }

            return false;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }

    public ref struct MultiBoundsEnumerable<T> where T : BaseMulti
    {
        public static MultiBoundsEnumerable<T> Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(null, Rectangle2D.Empty, false);
        }

        private Map _map;
        private Rectangle2D _bounds;
        private bool _makeBoundsInclusive;

        public MultiBoundsEnumerable(Map map, Rectangle2D bounds, bool makeBoundsInclusive)
        {
            _map = map;
            _bounds = bounds;
            _makeBoundsInclusive = makeBoundsInclusive;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultiBoundsEnumerator<T> GetEnumerator() => new(_map, _bounds, _makeBoundsInclusive);
    }

    public ref struct MultiBoundsEnumerator<T> where T : BaseMulti
    {
        private readonly Map _map;
        private readonly int _sectorStartX;
        private readonly int _sectorEndX;
        private readonly int _sectorEndY;
        private Rectangle2D _bounds;

        private int _currentSectorX;
        private int _currentSectorY;

        private Span<BaseMulti> _list;
        private T _current;
        private int _index;

        private HashSet<Serial> _dupes;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultiBoundsEnumerator(Map map, Rectangle2D bounds, bool makeBoundsInclusive)
        {
            _map = map;
            _bounds = bounds;

            if (makeBoundsInclusive)
            {
                ++bounds.Width;
                ++bounds.Height;
            }

            _bounds = bounds;

            map.CalculateSectors(bounds, out _sectorStartX, out var _sectorStartY, out _sectorEndX, out _sectorEndY);

            // We start the X sector one short because it gets incremented immediately in MoveNext()
            _currentSectorX = _sectorStartX - 1;
            _currentSectorY = _sectorStartY;
            _index = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetMulti()
        {
            ref Rectangle2D bounds = ref _bounds;

            while ((uint)_index < (uint)_list.Length)
            {
                var current = _list[_index++];
                _dupes ??= new HashSet<Serial>();

                if (current is T { Deleted: false } o && bounds.Contains(o.Location) && !_dupes.Contains(o.Serial))
                {
                    _dupes.Add(o.Serial);
                    _current = o;
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetSector()
        {
            var currentSectorX = _currentSectorX;
            var currentSectorY = _currentSectorY;
            var sectorEndX = _sectorEndX;
            var sectorEndY = _sectorEndY;

            // Move to next sector
            if (currentSectorX < sectorEndX)
            {
                _currentSectorX = ++currentSectorX;
            }
            else if (currentSectorY < sectorEndY)
            {
                _currentSectorX = currentSectorX = _sectorStartX;
                _currentSectorY = ++currentSectorY;
            }
            else
            {
                // Ran out of sectors
                return false;
            }

            _list = CollectionsMarshal.AsSpan(_map.GetRealSector(currentSectorX, currentSectorY).Multis);
            return GetMulti();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => _map != null && (GetMulti() || GetSector());

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }
}
