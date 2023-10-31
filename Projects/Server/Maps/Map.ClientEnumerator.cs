/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Map.ClientEnumerator.cs                                         *
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
using Server.Network;

namespace Server;

public partial class Map
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ClientAtEnumerable GetClientsAt(Point3D p) => GetClientsAt(new Point2D(p.X, p.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ClientAtEnumerable GetClientsAt(int x, int y) => GetClientsAt(new Point2D(x, y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ClientAtEnumerable GetClientsAt(Point2D p) => new(this, p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ClientBoundsEnumerable GetClientsInRange(Point3D p) => GetClientsInRange(p, Core.GlobalMaxUpdateRange);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ClientBoundsEnumerable GetClientsInRange(Point3D p, int range) =>
        GetClientsInRange(p.m_X, p.m_Y, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ClientBoundsEnumerable GetClientsInRange(Point2D p) => GetClientsInRange(p, Core.GlobalMaxUpdateRange);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ClientBoundsEnumerable GetClientsInRange(Point2D p, int range) =>
        GetClientsInRange(p.m_X, p.m_Y, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ClientBoundsEnumerable GetClientsInRange(int x, int y, int range) =>
        GetClientsInBounds(new Rectangle2D(x - range, y - range, range * 2 + 1, range * 2 + 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ClientBoundsEnumerable GetClientsInBounds(Rectangle2D bounds, bool makeBoundsInclusive = false) =>
        new(this, bounds, makeBoundsInclusive);

    public ref struct ClientAtEnumerable
    {
        public static ClientAtEnumerable Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new();
        }

        private readonly Map _map;
        private readonly Point2D _location;

        public ClientAtEnumerable(Map map, Point2D loc)
        {
            _map = map;
            _location = loc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ClientAtEnumerator GetEnumerator() => new(_map, _location);
    }

    public ref struct ClientAtEnumerator
    {
        private bool _started;
        private Point2D _location;
        private ref readonly ValueLinkList<NetState> _linkList;
        private int _version;
        private NetState _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ClientAtEnumerator(Map map, Point2D loc)
        {
            _started = false;
            _location = loc;
            _linkList = ref map.GetRealSector(loc.m_X, loc.m_Y).Clients;
            _version = 0;
            _current = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            ref var loc = ref _location;
            NetState current;
            Mobile m;

            if (!_started)
            {
                current = _linkList._first;
                _started = true;
                _version = _linkList.Version;

                m = current.Mobile;
                if (m?.Deleted == false && m.X == loc.m_X && m.Y == loc.m_Y)
                {
                    _current = current;
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

                m = current.Mobile;
                if (m?.Deleted == false && m.X == loc.m_X && m.Y == loc.m_Y)
                {
                    _current = current;
                    return true;
                }
            }

            return false;
        }

        public NetState Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }

    public ref struct ClientBoundsEnumerable
    {
        public static ClientBoundsEnumerable Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(null, Rectangle2D.Empty, false);
        }

        private readonly Map _map;
        private readonly Rectangle2D _bounds;
        private readonly bool _makeBoundsInclusive;

        public ClientBoundsEnumerable(Map map, Rectangle2D bounds, bool makeBoundsInclusive)
        {
            _map = map;
            _bounds = bounds;
            _makeBoundsInclusive = makeBoundsInclusive;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MobileEnumerator GetEnumerator() => new(_map, _bounds, _makeBoundsInclusive);
    }

    public ref struct MobileEnumerator
    {
        private readonly Map _map;
        private readonly int _sectorStartX;
        private readonly int _sectorEndX;
        private readonly int _sectorEndY;
        private Rectangle2D _bounds;

        private int _currentSectorX;
        private int _currentSectorY;

        private ref readonly ValueLinkList<NetState> _linkList;
        private int _currentVersion;
        private NetState _current;

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

            Mobile m;
            NetState current = _current;
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

                    _linkList = ref map.GetRealSector(currentSectorX, currentSectorY).Clients;
                    _currentVersion = _linkList.Version;
                    current = _linkList._first;
                }

                if (_linkList.Version != _currentVersion)
                {
                    throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_EnumFailedVersion);
                }

                m = current.Mobile;
                if (m?.Deleted == false && bounds.Contains(m.Location))
                {
                    _current = current;
                    return true;
                }
            }
        }

        public NetState Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }
}
