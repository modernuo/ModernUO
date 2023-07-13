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

        public bool IsEnabled(MapSelectionFlags mapFlags)
        {
            if ((_selectedMaps & mapFlags) == mapFlags)
            {
                return true;
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

        private MapSelectionFlags _selectedMaps;
    }
}

