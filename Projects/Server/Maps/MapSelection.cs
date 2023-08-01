using System;
using System.Runtime.CompilerServices;
using Server.Text;

namespace Server.Maps;

[Flags]
public enum MapSelectionFlags
{
    Felucca = 0x00000001,
    Trammel = 0x00000002,
    Ilshenar = 0x00000004,
    Malas = 0x00000008,
    Tokuno = 0x00000010,
    TerMur = 0x00000020
}

public static class MapSelection
{
    public static MapSelectionFlags[] MapSelectionValues { get; } = Enum.GetValues<MapSelectionFlags>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Includes(this MapSelectionFlags flags, MapSelectionFlags flag) => (flags & flag) == flag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Toggle(this ref MapSelectionFlags flags, MapSelectionFlags flag)
    {
        flags ^= flag;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Enable(this ref MapSelectionFlags flags, MapSelectionFlags flag)
    {
        flags |= flag;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Disable(this ref MapSelectionFlags flags, MapSelectionFlags flag)
    {
        flags &= ~flag;
    }

    public static string ToCommaDelimitedString(this MapSelectionFlags flags)
    {
        using var builder = ValueStringBuilder.Create();

        foreach (var flag in flags.GetEnumerable())
        {
            builder.Append(builder.Length > 0 ? $", {flag}" : $"{flag}");
        }

        return builder.Length == 0 ? "None" : builder.ToString();
    }

    public static MapSelectionFlags ToSelectionFlag(this Map map) => (MapSelectionFlags)(1 << map.MapID);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MapSelectionEnumerable GetEnumerable(this MapSelectionFlags flags) => new(flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MapSelectionEnumerable EnumFromExpansion(Expansion expansion) => ExpansionInfo.GetInfo(expansion).MapSelectionFlags.GetEnumerable();

    public class MapSelectionEnumerable
    {
        private readonly MapSelectionFlags _flags;

        public MapSelectionEnumerable(MapSelectionFlags flags) => _flags = flags;

        public MapSelectionEnumerator GetEnumerator() => new(MapSelectionValues, _flags);
    }

    public ref struct MapSelectionEnumerator
    {
        private readonly MapSelectionFlags[] _allMaps;
        private readonly MapSelectionFlags _mapSelections;
        private int _index;
        private MapSelectionFlags _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal MapSelectionEnumerator(MapSelectionFlags[] allMaps, MapSelectionFlags mapSelections)
        {
            _allMaps = allMaps;
            _mapSelections = mapSelections;
            _index = 0;
            _current = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            MapSelectionFlags[] localList = _allMaps;

            while ((uint)_index < (uint)localList.Length)
            {
                _current = localList[_index++];

                if ((_mapSelections & _current) == _current)
                {
                    return true;
                }
            }

            return false;
        }

        public MapSelectionFlags Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }
}

