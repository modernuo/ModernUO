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

public class AddSpeckledScorpions : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        var locations = new Point3D[]
        {
            new( 1588, 610, -14 ),
            new( 1576, 597, -14 ),
            new( 1607, 598, -14 ),
            new( 1546, 631, -14 )
        };

        foreach ( var location in locations )
        {
            var spawner = new Spawner( 2, TimeSpan.FromMinutes( 5 ), TimeSpan.FromMinutes( 10 ), 0, 5, "SpeckledScorpion" );

            spawner.MoveToWorld( new WorldLocation( location, Map.Ilshenar ) );

            serials.Add( spawner.Serial );

            spawner.Respawn();
        }

        return serials;
    }

    public void Down()
    {
    }
}
