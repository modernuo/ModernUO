/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Map.MultiTileEnumerator.cs                                      *
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiTilesAtEnumerable GetMultiTilesAt(int x, int y) => GetMultiTilesAt(new Point2D(x, y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MultiTilesAtEnumerable GetMultiTilesAt(Point2D p) => new(this, p);

    public ref struct MultiTilesAtEnumerable
    {
        public static MultiTilesAtEnumerable Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new();
        }

        private readonly Map _map;
        private readonly Point2D _location;

        public MultiTilesAtEnumerable(Map map, Point2D loc)
        {
            _map = map;
            _location = loc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultiTilesAtEnumerator GetEnumerator() => new(_map, _location);
    }

    public ref struct MultiTilesAtEnumerator
    {
        private Point2D _location;
        private MultiAtEnumerator<BaseMulti> _multis;
        private BaseMulti _currentMulti;
        private StaticTile[] _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultiTilesAtEnumerator(Map map, Point2D loc)
        {
            _multis = (map == null ? MultiAtEnumerable<BaseMulti>.Empty : map.GetMultisAt(loc)).GetEnumerator();

            _current = null;
            _location = loc;
            _currentMulti = null;
        }

        private bool SetStaticTiles()
        {
            var mcl = _currentMulti.Components;
            var x = _location.X;
            var xo = x - (_currentMulti.X + mcl.Min.X);

            var y = _location.Y;
            if (xo < 0 || xo >= mcl.Width)
            {
                return false;
            }

            var yo = y - (_currentMulti.Y + mcl.Min.Y);
            if (yo < 0 || yo >= mcl.Height)
            {
                return false;
            }

            var t = mcl.Tiles[xo][yo];

            // TODO: Remove the allocation.
            var r = new StaticTile[t.Length];

            for (var i = 0; i < t.Length; i++)
            {
                r[i] = t[i];
                r[i].Z += _currentMulti.Z;
            }

            _current = r;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (_multis.MoveNext())
            {
                _currentMulti = _multis.Current;
                if (SetStaticTiles())
                {
                    return true;
                }
            }

            return false;
        }

        public StaticTile[] Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }
}
