/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Map.Enumerators.cs                                              *
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
    private int _iteratingItems;
    private List<(MapAction, Point3D, Item)> _delayedItemActions = new();

    public bool IsIteratingItems
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _iteratingItems > 0;
    }

    public ItemEnumerable<Item> GetItemsInRange(Point3D p) => GetItemsInRange(p, Core.GlobalMaxUpdateRange);

    public ItemEnumerable<Item> GetItemsInRange(Point3D p, int range) => GetItemsInRange<Item>(p, range);

    public ItemEnumerable<T> GetItemsInRange<T>(Point3D p, int range) where T : Item =>
        GetItemsInBounds<T>(new Rectangle2D(p.m_X - range, p.m_Y - range, range * 2 + 1, range * 2 + 1));

    public ItemEnumerable<Item> GetItemsInBounds(Rectangle2D bounds) => GetItemsInBounds<Item>(bounds);

    public ItemEnumerable<T> GetItemsInBounds<T>(Rectangle2D bounds, bool makeBoundsInclusive = false) where T : Item =>
        new(this, bounds, makeBoundsInclusive);

    private void BeginIteratingItems()
    {
#if THREADGUARD
            if (Thread.CurrentThread != Core.Thread)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine($"Iterating through items on {this} from an invalid thread!");
                Console.WriteLine(new StackTrace());
                Utility.PopColor();
                return;
            }
#endif

        _iteratingItems++;
    }

    private void EndIteratingItems()
    {
#if THREADGUARD
            if (Thread.CurrentThread != Core.Thread)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine($"Iterating through items on {this} from an invalid thread!");
                Console.WriteLine(new StackTrace());
                Utility.PopColor();
                return;
            }
#endif

        _iteratingItems--;

        // Finished iterating, check for deferred actions
        if (_iteratingItems == 0 && _delayedItemActions.Count > 0)
        {
            foreach (var (a, p, i) in _delayedItemActions)
            {
                switch (a)
                {
                    case MapAction.Enter:
                        {
                            OnEnter(p, i);
                            break;
                        }
                    case MapAction.Leave:
                        {
                            OnLeave(p, i);
                            break;
                        }
                    case MapAction.Move:
                        {
                            OnMove(p, i);
                            break;
                        }
                }
            }

            _delayedItemActions.Clear();
        }
    }

    public ref struct ItemEnumerable<T> where T : Item
    {
        public static ItemEnumerable<T> Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(null, Rectangle2D.Empty, false);
        }

        private Map _map;
        private Rectangle2D _bounds;
        private bool _makeBoundsInclusive;

        public ItemEnumerable(Map map, Rectangle2D bounds, bool makeBoundsInclusive)
        {
            _map = map;
            _bounds = bounds;
            _makeBoundsInclusive = makeBoundsInclusive;
        }

        // The enumerator MUST be disposed. Not disposing it will damage the sector irreparably.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemEnumerator<T> GetEnumerator() => new(_map, _bounds, _makeBoundsInclusive);
    }

    public ref struct ItemEnumerator<T> where T : Item
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
        public ItemEnumerator(Map map, Rectangle2D bounds, bool makeBoundsInclusive)
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

            _map.BeginIteratingItems();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var map = _map;

            if (map == null)
            {
                return false;
            }

            Item current = _current;
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

                    current = map.GetRealSector(currentSectorX, currentSectorY).Items.First;
                }

                if (current is T { Deleted: false, Parent: null } o && bounds.Contains(o.Location))
                {
                    _current = o;
                    return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _map.EndIteratingItems();

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }
}
