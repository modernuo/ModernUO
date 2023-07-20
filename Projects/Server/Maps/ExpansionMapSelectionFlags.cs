using System.Collections.Generic;

namespace Server.Maps;

public static class ExpansionMapSelectionFlags
{
    public static List<MapSelectionFlags> FromExpansion(Expansion expansion)
    {
        MapSelectionFlags allMapsInExpansion = 
        expansion switch
        {
            >= Expansion.TOL => ExpansionInfo.Table[(int)Expansion.TOL].MapSelectionFlags,
            >= Expansion.SA => ExpansionInfo.Table[(int)Expansion.SA].MapSelectionFlags,
            >= Expansion.SE => ExpansionInfo.Table[(int)Expansion.SE].MapSelectionFlags,
            >= Expansion.AOS => ExpansionInfo.Table[(int)Expansion.AOS].MapSelectionFlags,
            >= Expansion.LBR => ExpansionInfo.Table[(int)Expansion.LBR].MapSelectionFlags,
            >= Expansion.T2A => ExpansionInfo.Table[(int)Expansion.T2A].MapSelectionFlags,
            _ => ExpansionInfo.Table[0].MapSelectionFlags
        };

        List<MapSelectionFlags> mapFlags = new();
        foreach (MapSelectionFlags mapFlag in MapSelection.MapSelectionValues)
        {
            if (mapFlag == MapSelectionFlags.None)
            {
                continue;
            }

            if ((allMapsInExpansion & mapFlag) == mapFlag)
            {
                mapFlags.Add(mapFlag);
            }
        }

        return mapFlags;
    }
}
