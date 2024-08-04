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

namespace Badlands.Migrations;

public class AddVerityPetrus : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        foreach ( var maps in new[] { Map.Felucca, Map.Trammel } )
        {
            var veritySpawner = new Spawner(
                1,
                TimeSpan.FromMinutes( 5 ),
                TimeSpan.FromMinutes( 10 ),
                0,
                4,
                "Verity"
            );

            veritySpawner.MoveToWorld( new WorldLocation( new Point3D( 1416, 1604, 30 ), maps ) );

            serials.Add( veritySpawner.Serial );

            veritySpawner.Respawn();
        }

        var petrusSpawner = new Spawner(
            1,
            TimeSpan.FromMinutes( 5 ),
            TimeSpan.FromMinutes( 10 ),
            0,
            4,
            "Petrus"
        );

        petrusSpawner.MoveToWorld( new WorldLocation( new Point3D( 1420, 407, 9 ), Map.Ilshenar ) );

        serials.Add( petrusSpawner.Serial );

        petrusSpawner.Respawn();

        return serials;
    }

    public void Down()
    {
    }
}
