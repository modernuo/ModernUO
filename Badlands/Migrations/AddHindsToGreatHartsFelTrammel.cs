// Copyright (C) 2024 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using Badlands.Commands;
using Server;
using Server.Engines.Spawners;
using Server.Logging;
using Server.Mobiles;

namespace Badlands.Migrations;

public class AddHindsToGreatHartsFelTrammel : IMigration
{
    public DateTime MigrationTime { get; set; }
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AddHindsToGreatHartsFelTrammel));

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        foreach ( var map in new Map[] { Map.Felucca, Map.Trammel} )
        {
            var spawners = World.Items.Values.Where(i => i is Spawner s && s.Entries.Any(e => e.SpawnedName == nameof(GreatHart))).ToList();

            foreach ( var spawner in spawners )
            {
                if ( spawner is Spawner spawn )
                {
                    if ( spawn.Entries.Any( e => e.SpawnedName == nameof( Hind ) ) )
                    {
                        continue;
                    }

                    var count = Utility.RandomMinMax( 1, 3 );

                    spawn.Entries.Add( new SpawnerEntry( nameof( Hind ), 100, count ) );

                    spawn.Count += count;

                    serials.Add( spawn.Serial );

                    logger.Information( $"Added {count} hinds to spawner location {spawn.Location}, {spawn.Map}" );
                }
            }
        }

        return serials;
    }

    public void Down()
    {
    }
}
