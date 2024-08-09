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
using Server.Engines.MLQuests.Definitions;
using Server.Engines.MLQuests.Mobiles;
using Server.Engines.Spawners;
using Server.Items;
using Server.Mobiles;

namespace Badlands.Migrations;

[MigrationPriority( 6 )]
public class AddHumanToElfItems : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        foreach ( var map in new[] { Map.Felucca, Map.Trammel } )
        {
            var timberWolfs = new Spawner(
                10,
                TimeSpan.FromMinutes( 5 ),
                TimeSpan.FromMinutes( 10 ),
                0,
                20,
                nameof( TimberWolf )
            );

            timberWolfs.MoveToWorld( new Point3D( 1673, 592, 16 ), map );

            serials.Add( timberWolfs.Serial );

            var darius = new Spawner( 1, TimeSpan.FromMinutes( 5 ), TimeSpan.FromMinutes( 10 ), 0, 4, nameof( Darius ) );

            darius.MoveToWorld( new Point3D( 4307, 955, 10 ), map );

            serials.Add( darius.Serial );

            var sap = new Spawner(
                10,
                TimeSpan.FromMinutes( 5 ),
                TimeSpan.FromMinutes( 10 ),
                0,
                20,
                nameof( SapOfSosaria )
            );

            sap.MoveToWorld( new Point3D( 755, 1005, 0 ), map );

            serials.Add( sap.Serial );

            var strongroot = new Spawner(
                1,
                TimeSpan.FromMinutes( 5 ),
                TimeSpan.FromMinutes( 10 ),
                0,
                4,
                nameof( Strongroot )
            );

            strongroot.MoveToWorld( new Point3D( 599, 1744, 0 ), map );

            serials.Add( strongroot.Serial );

            var maulTheBear = new Spawner(
                1,
                TimeSpan.FromMinutes( 5 ),
                TimeSpan.FromMinutes( 10 ),
                0,
                4,
                nameof( MaulTheBear )
            );

            maulTheBear.MoveToWorld( new Point3D( 1732, 259, 16 ), map );

            serials.Add( maulTheBear.Serial );
        }

        var arielle = new Spawner( 1, TimeSpan.FromMinutes( 5 ), TimeSpan.FromMinutes( 10 ), 0, 4, nameof( Arielle ) );

        arielle.MoveToWorld( new Point3D( 1541, 1190, -25 ), Map.Ilshenar );

        serials.Add( arielle.Serial );

        var baubles = new Spawner( 5, TimeSpan.FromMinutes( 5 ), TimeSpan.FromMinutes( 10 ), 0, 20, nameof( ABauble ) );

        baubles.MoveToWorld( new Point3D( 1541, 1190, -25 ), Map.Ilshenar );

        serials.Add( baubles.Serial );

        return serials;
    }

    public void Down()
    {
    }
}
