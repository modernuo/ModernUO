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

public class AddHeartwoodTrashBarrels : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        var locations = new Point3D[]
        {
            new( 7000, 385, 0 ),
            new( 7038, 378, 10 ),
            new( 7050, 378, 12 ),
            new( 7057, 411, 0 ),
            new( 7031, 436, 0 ),
            new( 7030, 414, 7 )
        };

        foreach ( var map in new[] { Map.Felucca, Map.Trammel } )
        {
            foreach ( var location in locations )
            {
                if ( location.Z >= 10 )
                {
                    var table = new Static( 0x0B35 );
                    var tableLocation = new Point3D( location.X, location.Y, location.Z - 10 );

                    table.MoveToWorld( tableLocation, map );
                    serials.Add( table.Serial );
                }

                var barrel = new TrashBarrel
                {
                    ItemID = 0xe7f,
                    Hue = 671
                };

                barrel.MoveToWorld( location, map );
                serials.Add( barrel.Serial );
            }
        }

        return serials;
    }

    public void Down()
    {
        throw new NotImplementedException();
    }
}
