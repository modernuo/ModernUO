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

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server;

public partial class Map
{
    private int _iteratingMobiles;
    private List<(MapAction, Point3D, Mobile)> _delayedMobileActions = new();

    public bool IsIteratingMobiles
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _iteratingMobiles > 0;
    }

    public MobileEnumerable<Mobile> GetMobilesInRange(Point3D p) => GetMobilesInRange(p, Core.GlobalMaxUpdateRange);

    public MobileEnumerable<Mobile> GetMobilesInRange(Point3D p, int range) => GetMobilesInRange<Mobile>(p, range);

    public MobileEnumerable<T> GetMobilesInRange<T>(Point3D p, int range) where T : Mobile =>
        GetMobilesInBounds<T>(new Rectangle2D(p.m_X - range, p.m_Y - range, range * 2 + 1, range * 2 + 1));

    public MobileEnumerable<Mobile> GetMobilesInBounds(Rectangle2D bounds) => GetMobilesInBounds<Mobile>(bounds);

    public MobileEnumerable<T> GetMobilesInBounds<T>(Rectangle2D bounds, bool makeBoundsInclusive = false) where T : Mobile =>
        new(this, bounds, makeBoundsInclusive);

    private void BeginIteratingMobiles()
    {
#if THREADGUARD
            if (Thread.CurrentThread != Core.Thread)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine($"Iterating through mobiles on {this} from an invalid thread!");
                Console.WriteLine(new StackTrace());
                Utility.PopColor();
                return;
            }
#endif

        _iteratingMobiles++;
    }

    private void EndIteratingMobiles()
    {
#if THREADGUARD
            if (Thread.CurrentThread != Core.Thread)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine($"Iterating through mobiles on {this} from an invalid thread!");
                Console.WriteLine(new StackTrace());
                Utility.PopColor();
                return;
            }
#endif

        _iteratingMobiles--;

        // Finished iterating, check for deferred actions
        if (_iteratingMobiles == 0 && _delayedMobileActions.Count > 0)
        {
            foreach (var (a, p, m) in _delayedMobileActions)
            {
                switch (a)
                {
                    case MapAction.Enter:
                        {
                            OnEnter(p, m);
                            break;
                        }
                    case MapAction.Leave:
                        {
                            OnLeave(p, m);
                            break;
                        }
                    case MapAction.Move:
                        {
                            OnMove(p, m);
                            break;
                        }
                }
            }

            _delayedMobileActions.Clear();
        }
    }

    public ref struct MobileEnumerable<T> where T : Mobile
    {
        public static MobileEnumerable<T> Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(null, Rectangle2D.Empty, false);
        }

        private Map _map;
        private Rectangle2D _bounds;
        private bool _makeBoundsInclusive;

        public MobileEnumerable(Map map, Rectangle2D bounds, bool makeBoundsInclusive)
        {
            _map = map;
            _bounds = bounds;
            _makeBoundsInclusive = makeBoundsInclusive;
        }

        // The enumerator MUST be disposed. Not disposing it will damage the sector irreparably.
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

            _map.BeginIteratingMobiles();
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

                    current = map.GetRealSector(currentSectorX, currentSectorY).Mobiles.First;
                }

                if (current is T { Deleted: false } o && bounds.Contains(o.Location))
                {
                    _current = o;
                    return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _map.EndIteratingMobiles();

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }
}
