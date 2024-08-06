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

using Server;
using Server.Engines.Spawners;
using Server.Logging;

namespace Badlands.Migrations;

public class AdjustAllSpawners : IMigration
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AdjustAllSpawners));
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        var spawners = World.Items.Values.Where( i => i is Spawner spawner && spawner.MinDelay == TimeSpan.FromMinutes( 5 ) ).Cast<Spawner>().ToArray();

        foreach ( var spawner in spawners )
        {
            var newMinTime = spawner.Map == Map.Felucca ? TimeSpan.FromMinutes( 1 ) : TimeSpan.FromMinutes( 2.5 );
            var newMaxTime = spawner.Map == Map.Felucca ? TimeSpan.FromMinutes( 3 ) : TimeSpan.FromMinutes( 5 );

            spawner.MinDelay = newMinTime;
            spawner.MaxDelay = newMaxTime;

            logger.Information( "Adjusted Spawner at {0}, {1}, {2} to MinDelay: {3}, MaxDelay: {4}", spawner.X, spawner.Y, spawner.Z, newMinTime, newMaxTime );

            serials.Add( spawner.Serial );
        }

        return serials;
    }

public void Down()
    {
    }
}
