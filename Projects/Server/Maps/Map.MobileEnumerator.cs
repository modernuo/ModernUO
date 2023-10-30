/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Map.MobileEnumerator.cs                                         *
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
using System.Runtime.CompilerServices;
using Server.Collections;

namespace Server;

public partial class Map
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileAtEnumerable<Mobile> GetMobilesAt(Point3D p) => GetMobilesAt<Mobile>(p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileAtEnumerable<T> GetMobilesAt<T>(Point3D p) where T : Mobile => GetMobilesAt<T>(new Point2D(p.X, p.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileAtEnumerable<Mobile> GetMobilesAt(int x, int y) => GetMobilesAt<Mobile>(new Point2D(x, y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileAtEnumerable<T> GetMobilesAt<T>(int x, int y) where T : Mobile => GetMobilesAt<T>(new Point2D(x, y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileAtEnumerable<Mobile> GetMobilesAt(Point2D p) => GetMobilesAt<Mobile>(p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileAtEnumerable<T> GetMobilesAt<T>(Point2D p) where T : Mobile => new(this, p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileBoundsEnumerable<Mobile> GetMobilesInRange(Point3D p) => GetMobilesInRange<Mobile>(p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileBoundsEnumerable<Mobile> GetMobilesInRange(Point3D p, int range) => GetMobilesInRange<Mobile>(p, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileBoundsEnumerable<T> GetMobilesInRange<T>(Point3D p) where T : Mobile => GetMobilesInRange<T>(p, Core.GlobalMaxUpdateRange);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileBoundsEnumerable<T> GetMobilesInRange<T>(Point3D p, int range) where T : Mobile =>
        GetMobilesInRange<T>(p.m_X, p.m_Y, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileBoundsEnumerable<Mobile> GetMobilesInRange(Point2D p) => GetMobilesInRange<Mobile>(p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileBoundsEnumerable<Mobile> GetMobilesInRange(Point2D p, int range) => GetMobilesInRange<Mobile>(p, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileBoundsEnumerable<T> GetMobilesInRange<T>(Point2D p) where T : Mobile => GetMobilesInRange<T>(p, Core.GlobalMaxUpdateRange);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileBoundsEnumerable<T> GetMobilesInRange<T>(Point2D p, int range) where T : Mobile =>
        GetMobilesInRange<T>(p.m_X, p.m_Y, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileBoundsEnumerable<T> GetMobilesInRange<T>(int x, int y, int range) where T : Mobile =>
        GetMobilesInBounds<T>(new Rectangle2D(x - range, y - range, range * 2 + 1, range * 2 + 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileBoundsEnumerable<Mobile> GetMobilesInBounds(Rectangle2D bounds) => GetMobilesInBounds<Mobile>(bounds);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileBoundsEnumerable<T> GetMobilesInBounds<T>(Rectangle2D bounds, bool makeBoundsInclusive = false) where T : Mobile =>
        new(this, bounds, makeBoundsInclusive);

    public ref struct MobileAtEnumerable<T> where T : Mobile
    {
        public static MobileAtEnumerable<T> Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new();
        }

        private Map _map;
        private Point2D _location;

        public MobileAtEnumerable(Map map, Point2D loc)
        {
            _map = map;
            _location = loc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MobileAtEnumerator<T> GetEnumerator() => new(_map, _location);
    }

    public ref struct MobileAtEnumerator<T> where T : Mobile
    {
        private bool _started;
        private Point2D _location;
        private ref readonly ValueLinkList<Mobile> _linkList;
        private int _version;
        private T _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MobileAtEnumerator(Map map, Point2D loc)
        {
            _started = false;
            _location = loc;
            _linkList = ref map.GetRealSector(loc.m_X, loc.m_Y).Mobiles;
            _version = 0;
            _current = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            ref var loc = ref _location;
            Mobile current;

            if (!_started)
            {
                current = _linkList._first;
                _started = true;
                _version = _linkList.Version;

                if (current is T { Deleted: false } o && o.X == loc.m_X && o.Y == loc.m_Y)
                {
                    _current = o;
                    return true;
                }
            }
            else if (_linkList.Version != _version)
            {
                throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_EnumFailedVersion);
            }
            else
            {
                current = _current;
            }

            while (current != null)
            {
                current = current.Next;

                if (current is T { Deleted: false } o && o.X == loc.m_X && o.Y == loc.m_Y)
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

    public ref struct MobileBoundsEnumerable<T> where T : Mobile
    {
        public static MobileBoundsEnumerable<T> Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(null, Rectangle2D.Empty, false);
        }

        private Map _map;
        private Rectangle2D _bounds;
        private bool _makeBoundsInclusive;

        public MobileBoundsEnumerable(Map map, Rectangle2D bounds, bool makeBoundsInclusive)
        {
            _map = map;
            _bounds = bounds;
            _makeBoundsInclusive = makeBoundsInclusive;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MobileEnumerator<T> GetEnumerator() => new(_map, _bounds, _makeBoundsInclusive);
    }

    public ref struct MobileEnumerator<T> where T : Mobile
    {
        private Map _map;
        private int _sectorStartX;
        private int _sectorEndX;
        private int _sectorEndY;
        private Rectangle2D _bounds;

        private int _currentSectorX;
        private int _currentSectorY;

        private ref readonly ValueLinkList<Mobile> _linkList;
        private int _currentVersion;
        private T _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MobileEnumerator(Map map, Rectangle2D bounds, bool makeBoundsInclusive)
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var map = _map;

            if (map == null)
            {
                return false;
            }

            Mobile current = _current;
            ref Rectangle2D bounds = ref _bounds;
            var currentSectorX = _currentSectorX;
            var currentSectorY = _currentSectorY;
            var sectorEndX = _sectorEndX;
            var sectorEndY = _sectorEndY;

            while (true)
            {
                current = current?.Next;

                while (current == null)
                {
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

                    _linkList = ref map.GetRealSector(currentSectorX, currentSectorY).Mobiles;
                    _currentVersion = _linkList.Version;
                    current = _linkList._first;
                }

                if (_linkList.Version != _currentVersion)
                {
                    throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_EnumFailedVersion);
                }

                if (current is T { Deleted: false } o && bounds.Contains(o.Location))
                {
                    _current = o;
                    return true;
                }
            }
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }
}
