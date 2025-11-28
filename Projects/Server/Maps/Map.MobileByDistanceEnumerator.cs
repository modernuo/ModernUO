/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Map.MobileByDistanceEnumerator.cs                               *
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
    public MobileDistanceEnumerable<Mobile> GetMobilesInRangeByDistance(Point3D p, int range) =>
        GetMobilesInRangeByDistance<Mobile>(new Point2D(p.X, p.Y), range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<T> GetMobilesInRangeByDistance<T>(Point3D p, int range) where T : Mobile =>
        GetMobilesInRangeByDistance<T>(new Point2D(p.X, p.Y), range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<Mobile> GetMobilesInRangeByDistance(Point2D p, int range) =>
        GetMobilesInRangeByDistance<Mobile>(p, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<T> GetMobilesInRangeByDistance<T>(Point2D p, int range) where T : Mobile =>
        new(this, p, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<Mobile> GetMobilesInRangeByDistance(int x, int y, int range) =>
        GetMobilesInRangeByDistance<Mobile>(new Point2D(x, y), range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<T> GetMobilesInRangeByDistance<T>(int x, int y, int range) where T : Mobile =>
        GetMobilesInRangeByDistance<T>(new Point2D(x, y), range);

    public ref struct MobileDistanceEnumerable<T> where T : Mobile
    {
        private readonly Map _map;
        private readonly Point2D _center;
        private readonly int _range;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MobileDistanceEnumerable(Map map, Point2D center, int range)
        {
            _map = map;
            _center = center;
            _range = range;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MobileDistanceEnumerator<T> GetEnumerator() => new(_map, _center, _range);
    }

    public ref struct MobileDistanceEnumerator<T> where T : Mobile
    {
        private Map _map;
        private Point2D _center;
        private int _range;

        private int _sectorStartX;

        private int _centerSectorX;
        private int _centerSectorY;

        private int _maxRing;
        private int _ring;   // -1 = uninitialized, then 0.._maxRing
        private int _ringIndex; // Current index within the ring

        private int _currentSectorX;
        private int _currentSectorY;

        private ref readonly ValueLinkList<Mobile> _linkList;
        private int _currentVersion;
        private T _current;

        private int _minDistance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MobileDistanceEnumerator(Map map, Point2D center, int range)
        {
            _map = map;
            _center = center;
            _range = range <= 0 ? 0 : range * range;

            _current = null;

            if (map != null)
            {
                _centerSectorX = center.m_X / SectorSize;
                _centerSectorY = center.m_Y / SectorSize;

                var bounds = new Rectangle2D(center.m_X - range, center.m_Y - range, range * 2 + 1, range * 2 + 1);
                map.CalculateSectors(bounds, out _sectorStartX, out var sectorStartY, out var sectorEndX, out var sectorEndY);

                var ringByRange = range <= 0 ? 0 : (range + SectorSize - 1) / SectorSize;
                var dx = Math.Max(_centerSectorX - _sectorStartX, sectorEndX - _centerSectorX);
                var dy = Math.Max(_centerSectorY - sectorStartY, sectorEndY - _centerSectorY);
                _maxRing = Math.Min(ringByRange, Math.Max(dx, dy));
            }

            _ring = -1;
            _ringIndex = -1;
            _currentSectorX = 0;
            _currentSectorY = 0;

            _currentVersion = 0;
            _minDistance = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var map = _map;

            if (map == null)
            {
                return false;
            }

            if (!Unsafe.IsNullRef(in _linkList) && _linkList.Version != _currentVersion)
            {
                throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_EnumFailedVersion);
            }

            Mobile current = _current;

            while (true)
            {
                current = current?.Next;

                while (current == null)
                {
                    // Move to next sector
                    if (!AdvanceToNextSector())
                    {
                        return false;
                    }

                    _linkList = ref map.GetRealSector(_currentSectorX, _currentSectorY).Mobiles;
                    _currentVersion = _linkList.Version;
                    current = _linkList._first;

                    _minDistance = MinDistSqToSectorRect(_center.m_X, _center.m_Y, _currentSectorX, _currentSectorY);
                }

                if (current is T { Deleted: false } o)
                {
                    var dx = o.X - _center.m_X;
                    var dy = o.Y - _center.m_Y;
                    var dsq = dx * dx + dy * dy;

                    if (dsq <= _range)
                    {
                        _current = o;
                        return true;
                    }
                }
            }
        }

        public (T Value, int MinDistance) Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_current, _minDistance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AdvanceToNextSector()
        {
            while (true)
            {
                if (TryNextSectorInRing(out _currentSectorX, out _currentSectorY))
                {
                    return true;
                }

                if (_ring >= _maxRing)
                {
                    return false;
                }

                _ring++;
                _ringIndex = -1;
            }
        }
        private bool TryNextSectorInRing(out int sx, out int sy)
        {
            if (_ring == 0)
            {
                // Center sector
                if (_ringIndex < 0)
                {
                    _ringIndex = 0;
                    sx = _centerSectorX;
                    sy = _centerSectorY;
                    return sx >= _sectorStartX;
                }

                sx = sy = 0;
                return false;
            }

            var totalSectors = _ring * 8;

            // Keep trying sectors in this ring until we find a valid one or exhaust the ring
            while (true)
            {
                var nextIndex = _ringIndex + 1;

                if (nextIndex >= totalSectors)
                {
                    sx = sy = 0;
                    return false;
                }

                _ringIndex = nextIndex;
                CalculatePositionFromIndex(nextIndex, out sx, out sy);

                if (sx >= _sectorStartX)
                {
                    return true;
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalculatePositionFromIndex(int index, out int x, out int y)
        {
            var ringSize = _ring * 2;
            var startX = _centerSectorX - _ring;
            var startY = _centerSectorY - _ring;

            if (index <= ringSize) // Top edge
            {
                x = startX + index;
                y = startY;
            }
            else if (index <= ringSize * 2) // Right edge
            {
                x = startX + ringSize;
                y = startY + (index - ringSize);
            }
            else if (index <= ringSize * 3) // Bottom edge
            {
                x = startX + ringSize - (index - ringSize * 2);
                y = startY + ringSize;
            }
            else // Left edge
            {
                x = startX;
                y = startY + ringSize - (index - ringSize * 3);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MinDistSqToSectorRect(int cx, int cy, int sectorX, int sectorY)
        {
            var x0 = sectorX * SectorSize;
            var y0 = sectorY * SectorSize;
            var x1 = x0 + (SectorSize - 1);
            var y1 = y0 + (SectorSize - 1);

            var dx = 0;
            if (cx < x0)
            {
                dx = x0 - cx;
            }
            else if (cx > x1)
            {
                dx = cx - x1;
            }

            var dy = 0;
            if (cy < y0)
            {
                dy = y0 - cy;
            }
            else if (cy > y1)
            {
                dy = cy - y1;
            }

            return dx * dx + dy * dy;
        }
    }
}
