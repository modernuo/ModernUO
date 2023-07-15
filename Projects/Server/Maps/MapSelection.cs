using System;
using System.Collections.Generic;

namespace Server.Maps
{
    public class MapSelection
    {
        public MapSelection() { }

        public MapSelection(List<string> mapList)
        {
            foreach (MapSelectionFlags value in Enum.GetValues(typeof(MapSelectionFlags)))
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
            foreach (MapSelectionFlags value in Enum.GetValues(typeof(MapSelectionFlags)))
            {
                if (value == 0)
                    continue;

                if (value.ToString() != mapName)
                    continue;

                if ((_selectedMaps & value) == value)
                    return true;

                return false;
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
            MapSelectionFlags[] allMapsInExpansion =
                ExpansionMapSelectionFlags.FromExpansion(expansion);
            
            foreach(MapSelectionFlags flags in allMapsInExpansion)
            {
                _selectedMaps |= flags;
            }
        }

        public string ToCommaDelimitedString()
        {
            string formatted = "";
            foreach (MapSelectionFlags value in Enum.GetValues(typeof(MapSelectionFlags)))
            {
                if (value == 0)
                    continue;

                if ((_selectedMaps & value) == value)
                {
                    formatted += value.ToString() + " ";
                }
            }
            formatted = formatted.TrimEnd().Replace(" ", ", ");

            if (formatted == "")
                formatted = "None";

            return formatted;
        }

        public List<string> ToList()
        {
            List<string> mapList = new();
            foreach (MapSelectionFlags value in Enum.GetValues(typeof(MapSelectionFlags)))
            {
                if (value == 0)
                    continue;

                if ((_selectedMaps & value) == value)
                {
                    mapList.Add(value.ToString());
                }
            }
            return mapList;
        }

        private MapSelectionFlags _selectedMaps;
    }
}

