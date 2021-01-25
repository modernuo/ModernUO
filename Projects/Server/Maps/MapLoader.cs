/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: MapLoader.cs                                                    *
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
using System.IO;
using Server.Json;

namespace Server
{
    public static class MapLoader
    {
        /* Here we configure all maps. Some notes:
         *
         * 1) The first 32 maps are reserved for core use.
         * 2) Map 0x7F is reserved for core use.
         * 3) Map 0xFF is reserved for core use.
         * 4) Changing or removing any predefined maps may cause server instability.
         *
         * Example of registering a custom map:
         * RegisterMap( 32, 0, 0, 6144, 4096, 3, "Iceland", MapRules.FeluccaRules );
         *
         * Defined:
         * RegisterMap( <index>, <mapID>, <fileIndex>, <width>, <height>, <season>, <name>, <rules> );
         *  - <index> : An unreserved unique index for this map
         *  - <mapID> : An identification number used in client communications. For any visible maps, this value must be from 0-5
         *  - <fileIndex> : A file identification number. For any visible maps, this value must be from 0-5
         *  - <width>, <height> : Size of the map (in tiles)
         *  - <season> : Season of the map. 0 = Spring, 1 = Summer, 2 = Fall, 3 = Winter, 4 = Desolation
         *  - <name> : Reference name for the map, used in props gump, get/set commands, region loading, etc
         *  - <rules> : Rules and restrictions associated with the map. See documentation for details
         */
        public static void LoadMaps()
        {

            var path = Path.Combine(Core.BaseDirectory, "Data/map-definitions.json");
            var maps = JsonConfig.Deserialize<List<MapDefinition>>(path);

            foreach (var def in maps)
            {
                RegisterMap(def);
            }
        }

        private static void RegisterMap(MapDefinition mapDefinition)
        {
            var newMap = new Map(
                mapDefinition.Id,
                mapDefinition.Index,
                mapDefinition.FileIndex,
                mapDefinition.Width,
                mapDefinition.Height,
                mapDefinition.Season,
                mapDefinition.Name,
                mapDefinition.Rules
            );

            Map.Maps[mapDefinition.Index] = newMap;
            Map.AllMaps.Add(newMap);
        }
    }
}
