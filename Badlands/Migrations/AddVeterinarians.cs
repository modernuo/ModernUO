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

public class AddVeterinarians : IMigration
{
    public DateTime MigrationTime { get; set; }
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AddVeterinarians));

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        var spawners = World.Items.Values.OfType<Spawner>();

        foreach ( var spawner in spawners.ToArray() )
        {
            if ( spawner.Entries.Any( e => e.SpawnedName == "AnimalTrainer" ) )
            {
                spawner.Entries.Add( new SpawnerEntry( "Veterinarian" ) );
                spawner.Count += 1;

                logger.Information( $"Added Veterinarion to spawner ID {spawner.Guid}" );

                serials.Add( spawner.Serial);

                spawner.Respawn();
            }
        }

        return serials;
    }

    public void Down()
    {
    }
}
