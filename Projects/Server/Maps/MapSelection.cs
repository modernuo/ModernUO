using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Maps;

public class MapSelection
{
    private MapSelectionFlags _selectedMaps;
    private static readonly Array _mapSelectionValues = Enum.GetValues(typeof(MapSelectionFlags));

    public static Array MapSelectionValues => _mapSelectionValues;

    public MapSelectionFlags Flags => _selectedMaps;

    public MapSelection()
    {
    }

    public MapSelection(MapSelectionFlags flags)
    {
        _selectedMaps = flags;
    }

    public MapSelection(List<string> mapList)
    {
        foreach (MapSelectionFlags value in _mapSelectionValues)
        {
            if (mapList.Contains(value.ToString()))
            {
                Enable(value);
            }
        }
    }

    public bool Includes(MapSelectionFlags mapFlags)
    {
        if ((_selectedMaps & mapFlags) == mapFlags)
        {
            return true;
        }

        return false;
    }

    public bool Includes(string mapName)
    {
        foreach (MapSelectionFlags value in _mapSelectionValues)
        {
            if (value == 0)
            {
                continue;
            }

            if (value.ToString() != mapName)
            {
                continue;
            }

            return (_selectedMaps & value) == value;
        }

        return false;
    }

    public void Enable(MapSelectionFlags mapFlags)
    {
        _selectedMaps |= mapFlags;
    }

    public void Disable(MapSelectionFlags mapFlags)
    {
        _selectedMaps &= ~mapFlags;
    }

    public void EnableAll()
    {
        _selectedMaps =
            MapSelectionFlags.Felucca | MapSelectionFlags.Trammel | MapSelectionFlags.Ilshenar |
            MapSelectionFlags.Malas | MapSelectionFlags.Tokuno | MapSelectionFlags.TerMur;
    }

    public void EnableAllInExpansion(Expansion expansion)
    {
        List<MapSelectionFlags> allMapsInExpansion =
            ExpansionMapSelectionFlags.FromExpansion(expansion);

        foreach(MapSelectionFlags mapFlag in allMapsInExpansion)
        {
            _selectedMaps |= mapFlag;
        }
    }

    public string ToCommaDelimitedString()
    {
        string formatted = "";

        foreach (MapSelectionFlags value in _mapSelectionValues)
        {
            if (value == 0)
            {
                continue;
            }

            if ((_selectedMaps & value) == value)
            {
                formatted += value + " ";
            }
        }
        formatted = formatted.TrimEnd().Replace(" ", ", ");

        if (formatted == "")
        {
            formatted = "None";
        }

        return formatted;
    }
}
