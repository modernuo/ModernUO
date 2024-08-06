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
using Server.Items;

namespace Badlands.Migrations;

public class AddTwistedWieldTeleporter : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        var teleporter = new TwistedWieldTeleporter();
        teleporter.MoveToWorld( new Point3D( 1450, 1470, -21 ), Map.Ilshenar );

        serials.Add( teleporter.Serial );

        var sparkle = new Static( 0x375a );
        sparkle.MoveToWorld(new Point3D(1450, 1470, -21), Map.Ilshenar);

        serials.Add( sparkle.Serial );


        var mushroomLocations = new Point3D[]
        {
            new( 1449, 1468, -22 ),
            new( 1451, 1468, -22 ),
            new( 1452, 1469, -22 ),
            new( 1452, 1471, -22 ),
            new( 1451, 1472, -22 ),
            new( 1449, 1472, -22 ),
            new( 1448, 1471, -22 ),
            new( 1448, 1469, -22 )
        };

        foreach ( var location in mushroomLocations )
        {
            var mushroom = new Static( 0xd16 );
            mushroom.MoveToWorld( location, Map.Ilshenar );

            serials.Add( mushroom.Serial );
        }

        return serials;
    }

    public void Down()
    {
    }
}
