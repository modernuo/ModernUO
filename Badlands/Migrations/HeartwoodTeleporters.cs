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

public class HeartwoodTeleporters : IMigration
{
    public DateTime MigrationTime { get; set; } = DateTime.Parse( "2024-05-19" );

    public void Up()
    {
        var entranceStaticTrammel = new Static( 14695 );
        entranceStaticTrammel.MoveToWorld( new Point3D( 535, 992, 0 ), Map.Trammel );

        var entranceStaticFelucca = new Static( 14695 );
        entranceStaticFelucca.MoveToWorld( new Point3D( 535, 992, 0 ), Map.Felucca );

        var entranceTeleportTrammel = new Teleporter( new Point3D( 6985, 340, 0 ), Map.Trammel );
        entranceTeleportTrammel.MoveToWorld( new Point3D( 535, 992, 0 ), Map.Trammel );

        var entranceTeleportFelucca = new Teleporter( new Point3D( 6985, 340, 0 ), Map.Felucca );
        entranceTeleportFelucca.MoveToWorld( new Point3D( 535, 992, 0 ), Map.Felucca );

        var exitStaticTrammel = new Static( 14695 );
        exitStaticTrammel.MoveToWorld( new Point3D( 6984, 338, 0 ), Map.Trammel );

        var exitStaticFelucca = new Static( 14695 );
        exitStaticFelucca.MoveToWorld( new Point3D( 6984, 338, 0 ), Map.Felucca );

        var exitTeleportTrammel = new Teleporter( new Point3D( 535, 992, 0 ), Map.Trammel );
        exitTeleportTrammel.MoveToWorld( new Point3D( 6984, 338, 0 ), Map.Trammel );

        var exitTeleportFelucca = new Teleporter( new Point3D( 535, 992, 0 ), Map.Felucca );
        exitTeleportFelucca.MoveToWorld( new Point3D( 6984, 338, 0 ), Map.Felucca );
    }

    public void Down()
    {
        throw new NotImplementedException();
    }
}
