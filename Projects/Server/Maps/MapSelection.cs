using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Maps
{
    public class MapSelection
    {
        public MapSelection()
        {
            _selectedMaps = MapSelectionFlags.None;
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

                if (value.ToString() == mapName)
                {
                    if ((_selectedMaps & value) == value)
                    {
                        return true;
                    }
                    return false;
                }
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

        private MapSelectionFlags _selectedMaps;
    }
}

