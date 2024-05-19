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

public class Initial : IMigration
{
    public DateTime MigrationTime { get; set; } = DateTime.Parse( "2024-05-18" );

    public void Up()
    {
        var item = new MetalDoor( DoorFacing.SouthCW );

        item.MoveToWorld( new WorldLocation( new Point3D( 3491, 2573, 21 ), Map.Trammel ) );

        var item2 = new MetalDoor( DoorFacing.NorthCCW );

        item2.MoveToWorld( new WorldLocation( new Point3D( 3491, 2571, 21 ), Map.Trammel ) );

        var item3 = new MetalDoor( DoorFacing.WestCW );

        item3.MoveToWorld( new WorldLocation( new Point3D( 3481, 2579, 20 ), Map.Trammel ) );
    }

    public void Down()
    {
    }
}
