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
    public MobileDistanceEnumerable<Mobile> GetMobilesInRangeByDistance(Point3D p) =>
        GetMobilesInRangeByDistance<Mobile>(p, Core.GlobalMaxUpdateRange);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<Mobile> GetMobilesInRangeByDistance(Point3D p, int range) =>
        GetMobilesInRangeByDistance<Mobile>(p, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<T> GetMobilesInRangeByDistance<T>(Point3D p) where T : Mobile =>
        GetMobilesInRangeByDistance<T>(p, Core.GlobalMaxUpdateRange);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<T> GetMobilesInRangeByDistance<T>(Point3D p, int range) where T : Mobile =>
        GetMobilesInRangeByDistance<T>(p.m_X, p.m_Y, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<Mobile> GetMobilesInRangeByDistance(Point2D p) =>
        GetMobilesInRangeByDistance<Mobile>(p, Core.GlobalMaxUpdateRange);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<Mobile> GetMobilesInRangeByDistance(Point2D p, int range) =>
        GetMobilesInRangeByDistance<Mobile>(p, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<T> GetMobilesInRangeByDistance<T>(Point2D p) where T : Mobile =>
        GetMobilesInRangeByDistance<T>(p, Core.GlobalMaxUpdateRange);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<T> GetMobilesInRangeByDistance<T>(Point2D p, int range) where T : Mobile =>
        GetMobilesInRangeByDistance<T>(p.m_X, p.m_Y, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<Mobile> GetMobilesInRangeByDistance(int x, int y, int range) =>
        GetMobilesInRangeByDistance<Mobile>(x, y, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<T> GetMobilesInRangeByDistance<T>(int x, int y, int range) where T : Mobile
    {
        var clampedRange = Math.Max(0, range);
        var edge = clampedRange * 2 + 1;
        return GetMobilesInBoundsByDistance<T>(
            new Rectangle2D(x - clampedRange, y - clampedRange, edge, edge),
            new Point2D(x, y)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<Mobile> GetMobilesInBoundsByDistance(Rectangle2D bounds, bool makeBoundsInclusive = false) =>
        GetMobilesInBoundsByDistance<Mobile>(bounds, makeBoundsInclusive);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MobileDistanceEnumerable<T> GetMobilesInBoundsByDistance<T>(Rectangle2D bounds, bool makeBoundsInclusive = false) where T : Mobile =>
        GetMobilesInBoundsByDistance<T>(bounds, new Point2D(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2), makeBoundsInclusive);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private MobileDistanceEnumerable<T> GetMobilesInBoundsByDistance<T>(
        Rectangle2D bounds, Point2D center, bool makeBoundsInclusive = false
    ) where T : Mobile => new(this, bounds, center, makeBoundsInclusive);

    public ref struct MobileDistanceEnumerable<T> where T : Mobile
    {
        private readonly Map _map;
        private readonly Rectangle2D _bounds;
        private readonly Point2D _center;
        private readonly bool _makeBoundsInclusive;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MobileDistanceEnumerable(Map map, Rectangle2D bounds, Point2D center, bool makeBoundsInclusive)
        {
            _map = map;
            _bounds = bounds;
            _center = center;
            _makeBoundsInclusive = makeBoundsInclusive;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MobileDistanceEnumerator<T> GetEnumerator() => new(_map, _bounds, _center, _makeBoundsInclusive);
    }

    public ref struct MobileDistanceEnumerator<T> where T : Mobile
    {
        private Map _map;
        private Point2D _center;
        private Rectangle2D _bounds;

        private int _sectorStartX;
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
        public MobileDistanceEnumerator(Map map, Rectangle2D bounds, Point2D center, bool makeBoundsInclusive)
        {
            _map = map;
            _center = center;
            _bounds = makeBoundsInclusive
                ? new Rectangle2D(bounds.X, bounds.Y, bounds.Width + 1, bounds.Height + 1)
                : bounds;

            _current = null;

            if (map != null)
            {
                var centerSectorX = center.m_X / SectorSize;
                var centerSectorY = center.m_Y / SectorSize;

                map.CalculateSectors(_bounds, out _sectorStartX, out var sectorStartY, out var sectorEndX, out var sectorEndY);

                // Calculate max ring based on bounds
                var dx = Math.Max(centerSectorX - _sectorStartX, sectorEndX - centerSectorX);
                var dy = Math.Max(centerSectorY - sectorStartY, sectorEndY - centerSectorY);
                _maxRing = Math.Max(dx, dy);
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
                    while (!TryNextSectorInRing(out _currentSectorX, out _currentSectorY))
                    {
                        // Current ring exhausted, try next ring
                        if (_ring >= _maxRing)
                        {
                            return false; // No more rings to search
                        }

                        _ring++;
                        _ringIndex = -1;
                    }

                    _linkList = ref map.GetRealSector(_currentSectorX, _currentSectorY).Mobiles;
                    _currentVersion = _linkList.Version;
                    current = _linkList._first;

                    if (current != null)
                    {
                        _minDistance = MinDistToSectorSqrt(_center.m_X, _center.m_Y, _currentSectorX, _currentSectorY);
                    }
                }

                if (current is T { Deleted: false } o && _bounds.Contains(o.Location))
                {
                    _current = o;
                    return true;
                }
            }
        }

        public (T Value, int MinDistance) Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_current, _minDistance);
        }
        private bool TryNextSectorInRing(out int sx, out int sy)
        {
            if (_ring == 0)
            {
                // Center sector
                if (_ringIndex < 0)
                {
                    _ringIndex = 0;
                    sx = _center.m_X / SectorSize;
                    sy = _center.m_Y / SectorSize;
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
            var centerSectorX = _center.m_X / SectorSize;
            var centerSectorY = _center.m_Y / SectorSize;

            var ringSize = _ring * 2;
            var startX = centerSectorX - _ring;
            var startY = centerSectorY - _ring;

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
        private static int MinDistToSectorSqrt(int cx, int cy, int sectorX, int sectorY)
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

            return (int)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
