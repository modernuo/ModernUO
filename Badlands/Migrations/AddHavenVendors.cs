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

public class AddHavenVendors : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        var _locationTypes = new Dictionary<Point3D, string[]>
        {
            { new( 3481, 2572, 20 ), [nameof( Banker ), nameof( Minter )] },
            { new( 3529, 2575, 7 ), [nameof( AnimalTrainer ), nameof( Veterinarian )] },
            { new( 3468, 2540, 36 ), [nameof( Blacksmith ), nameof( Armorer )] },
            { new( 3458, 2528, 53 ), [nameof( Tinker ), nameof( TinkerGuildmaster )] },
            { new( 3458, 2550, 35 ), [nameof( Healer ), nameof( HealerGuildmaster )] },
            { new( 3460, 2565, 35 ), [nameof( Alchemist ), nameof( Herbalist )] },
            { new( 3495, 2528, 27 ), [nameof( Scribe ), nameof( Mage ), nameof( Herbalist )] }
        };

        foreach ( var location in _locationTypes )
        {
            var spawner = new Spawner(
                1,
                TimeSpan.FromMinutes( 5 ),
                TimeSpan.FromMinutes( 10 ),
                0,
                4,
                location.Value
            )
            {
                Count = location.Value.Length
            };

            spawner.MoveToWorld( location.Key, Map.Trammel );
            spawner.Respawn();

            serials.Add( spawner.Serial );
        }

        return serials;
    }

    public void Down()
    {
    }
}
