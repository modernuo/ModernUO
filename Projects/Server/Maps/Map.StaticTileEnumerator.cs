/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Map.StaticTileEnumerator.cs                                     *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Runtime.CompilerServices;
using Server.Items;

namespace Server;

public partial class Map
{
    public ref struct StaticTileEnumerable
    {
        public static StaticTileEnumerable Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new();
        }

        private readonly Map _map;
        private readonly Point2D _location;
        private readonly bool _includeStatics;
        private readonly bool _includeMultis;

        public StaticTileEnumerable(Map map, Point2D loc, bool includeStatics = true, bool includeMultis = true)
        {
            _map = map;
            _location = loc;
            _includeStatics = includeStatics;
            _includeMultis = includeMultis;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StaticTileEnumerator GetEnumerator() => new(_map, _location, _includeStatics, _includeMultis);
    }

    public ref struct StaticTileEnumerator
    {
        private readonly Map _map;
        private readonly Point2D _point;

        private StaticTile[] _tiles;
        private MultiSectorEnumerator<BaseMulti> _multis;
        private BaseMulti _currentMulti;

        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StaticTileEnumerator(Map map, Point2D p, bool includeStatics, bool includeMultis)
        {
            _map = map;
            _point = p;

            if (_map == null)
            {
                return;
            }

            if (includeStatics)
            {
                var tiles = map.Tiles.GetStaticBlock(p.X >> SectorShift, p.Y >> SectorShift);
                _tiles = tiles[p.X & 0x7][p.Y & 0x7];
                _index = -1;
            }

            _multis = includeMultis
                ? _map.GetMultisInSector(p).GetEnumerator()
                : MultiSectorEnumerable<BaseMulti>.Empty.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SetMulti()
        {
            ref var multis = ref _multis;
            ref readonly var p = ref _point;

            if (multis.MoveNext())
            {
                var multi = multis.Current;
                _currentMulti = multi;
                var components = multi!.Components;
                var location = multi!.Location;

                int offsetX = p.X - location.X - components.Min.X;
                int offsetY = p.Y - location.Y - components.Min.Y;

                if (offsetX >= 0 && offsetY >= 0 && offsetX < components.Width && offsetY < components.Height)
                {
                    _tiles = multi.Components.Tiles[offsetX][offsetY];
                    _index = -1;
                    return SetTile();
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SetTile() => _tiles != null && ++_index < _tiles.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => _map != null && (SetTile() || SetMulti());

        public StaticTile Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_currentMulti == null)
                {
                    return _tiles[_index];
                }

                var location = _currentMulti.Location;
                ref readonly var tile = ref _tiles[_index];
                return new StaticTile
                {
                    m_ID = tile.m_ID,
                    X = tile.m_X,
                    Y = tile.m_Y,
                    Z = tile.m_Z + location.Z,
                    m_Hue = tile.m_Hue
                };
            }
        }
    }
}
