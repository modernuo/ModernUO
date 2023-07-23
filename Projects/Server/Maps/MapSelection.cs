using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Maps;

public class MapSelection
{
    public static IEnumerable<MapSelectionFlags> MapSelectionValues { get; } = Enum.GetValues<MapSelectionFlags>();

    public MapSelectionFlags Flags { get; private set; }

    public MapSelection()
    {
    }

    public MapSelection(MapSelectionFlags flags) => Flags = flags;

    public MapSelection(ICollection<string> mapList)
    {
        foreach (MapSelectionFlags value in MapSelectionValues)
        {
            if (mapList.Contains(value.ToString()))
            {
                Enable(value);
            }
        }
    }

    public bool Includes(MapSelectionFlags mapFlags) => (Flags & mapFlags) == mapFlags;

    public bool Includes(string mapName)
    {
        foreach (MapSelectionFlags value in MapSelectionValues)
        {
            if (value == 0)
            {
                continue;
            }
            if (value.ToString() != mapName)
            {
                continue;
            }
            return Includes(value);
        }
        return false;
    }

    public void Enable(MapSelectionFlags mapFlags)
    {
        Flags |= mapFlags;
    }

    public void Disable(MapSelectionFlags mapFlags)
    {
        Flags &= ~mapFlags;
    }

    public void EnableAll()
    {
        Flags =
            MapSelectionFlags.Felucca | MapSelectionFlags.Trammel | MapSelectionFlags.Ilshenar |
            MapSelectionFlags.Malas | MapSelectionFlags.Tokuno | MapSelectionFlags.TerMur;
    }

    public void EnableAllInExpansion(Expansion expansion)
    {
        List<MapSelectionFlags> allMapsInExpansion =
            ExpansionMapSelectionFlags.FromExpansion(expansion);

        foreach(MapSelectionFlags mapFlag in allMapsInExpansion)
        {
            Flags |= mapFlag;
        }
    }

    public string ToCommaDelimitedString()
    {
        string formatted = "";

        foreach (MapSelectionFlags value in MapSelectionValues)
        {
            if (value == 0)
            {
                continue;
            }

            if ((Flags & value) == value)
            {
                formatted += value + " ";
            }
        }
        formatted = formatted.TrimEnd().Replace(" ", ", ", StringComparison.Ordinal);

        if (formatted == "")
        {
            formatted = "None";
        }

        return formatted;
    }
}
