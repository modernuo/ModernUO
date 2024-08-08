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
using Server.Mobiles;

namespace Badlands.Migrations;

[MigrationPriority( 3 )]
public class AddMagVendors2 : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        var locations = new Dictionary<Point3D, string[]>
        {
            { new Point3D( 3649, 2297, -2 ), [nameof( Fisherman )] },
            { new Point3D( 3706, 2245, 20 ), [nameof( Blacksmith ), nameof( BlacksmithGuildmaster )] },
            { new Point3D( 3705, 2251, 20 ), [nameof( Tailor ), nameof( TailorGuildmaster )] },
            { new Point3D( 3695, 2220, 20 ), [nameof( Healer ), nameof( HealerGuildmaster )] },
            { new Point3D( 3697, 2209, 20 ), [nameof( Noble ), nameof( SeekerOfAdventure ), nameof( Peasant )] },
            { new Point3D( 3694, 2247, 25 ), [nameof( InnKeeper )] },
            { new Point3D( 3667, 2257, 25 ), [nameof( Shipwright ), nameof( Fisherman )] },
            { new Point3D( 3787, 2250, 20 ), [nameof( Banker ), nameof( Minter )] }
        };

        foreach ( var map in new[] { Map.Felucca, Map.Trammel } )
        {
            foreach ( var location in locations )
            {
                var spawner = new Spawner( 1, TimeSpan.FromMinutes( 5 ), TimeSpan.FromMinutes( 10 ), 0, 5, location.Value );

                spawner.MoveToWorld( location.Key, map );
                spawner.Count += spawner.Entries.Count;
                spawner.Respawn();

                serials.Add( spawner.Serial );
            }
        }

        return serials;
    }

    public void Down()
    {
    }
}
