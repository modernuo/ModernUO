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
using Server.Items;
using Server.Mobiles;

namespace Badlands.Migrations;

[MigrationPriority( 5 )]
public class AddHavenAnkh : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        var ankh = new AnkhNorth();

        ankh.MoveToWorld( new Point3D( 3526, 2518, 25 ), Map.Trammel );

        var fightersSpawner = new Spawner(
            3,
            TimeSpan.FromMinutes( 5 ),
            TimeSpan.FromMinutes( 10 ),
            0,
            10,
            nameof( HireFighter )
        );

        fightersSpawner.MoveToWorld( new Point3D( 3527, 2519, 65 ), Map.Trammel );

        serials.Add( ankh.Serial );

        var cookSpawner = new Spawner(
            1,
            TimeSpan.FromMinutes( 5 ),
            TimeSpan.FromMinutes( 10 ),
            0,
            10,
            [nameof( InnKeeper ), nameof( Cook )]
        );

        cookSpawner.Count = cookSpawner.Entries.Count;

        cookSpawner.MoveToWorld( new Point3D( 3504, 2518, 27 ), Map.Trammel );

        serials.Add( cookSpawner.Serial );

        var fisherSpawner = new Spawner(
            3,
            TimeSpan.FromMinutes( 5 ),
            TimeSpan.FromMinutes( 10 ),
            0,
            10,
            nameof( Fisherman )
        );

        fisherSpawner.MoveToWorld( new Point3D( 3514, 2594, 0 ), Map.Trammel );

        serials.Add( fisherSpawner.Serial );

        var shipWrightSpawner = new Spawner(
            1,
            TimeSpan.FromMinutes( 5 ),
            TimeSpan.FromMinutes( 10 ),
            0,
            4,
            [nameof( Shipwright ), nameof( Mapmaker )]
        );

        shipWrightSpawner.Count = shipWrightSpawner.Entries.Count;

        shipWrightSpawner.MoveToWorld( new Point3D( 3493, 2590, 35 ), Map.Trammel );

        serials.Add( shipWrightSpawner.Serial );

        var townCrierSpawner = new Spawner(
            1,
            TimeSpan.FromMinutes( 5 ),
            TimeSpan.FromMinutes( 10 ),
            0,
            4,
            nameof( TownCrier )
        );

        townCrierSpawner.MoveToWorld( new Point3D( 3494, 2583, 14 ), Map.Trammel );

        serials.Add( townCrierSpawner.Serial );

        var nobleSpawner = new Spawner(
            3,
            TimeSpan.FromMinutes( 5 ),
            TimeSpan.FromMinutes( 10 ),
            0,
            4,
            [nameof( Noble ), nameof( SeekerOfAdventure ), nameof( EscortableMage )]
        );

        nobleSpawner.Count = nobleSpawner.Entries.Count;

        nobleSpawner.MoveToWorld( new Point3D( 3463, 2620, 17 ), Map.Trammel );

        serials.Add( nobleSpawner.Serial );

        var nobleSpawner2 = new Spawner(
            3,
            TimeSpan.FromMinutes( 5 ),
            TimeSpan.FromMinutes( 10 ),
            0,
            4,
            [nameof( Noble ), nameof( SeekerOfAdventure ), nameof( HireFighter )]
        );

        nobleSpawner2.Count = nobleSpawner.Entries.Count;

        nobleSpawner2.MoveToWorld( new Point3D( 3440, 2603, 40 ), Map.Trammel );

        serials.Add( nobleSpawner2.Serial );

        var bardSpawner = new Spawner(
            3,
            TimeSpan.FromMinutes( 5 ),
            TimeSpan.FromMinutes( 10 ),
            0,
            4,
            nameof( Bard )
        );

        bardSpawner.MoveToWorld( new Point3D( 3415, 2602, 55 ), Map.Trammel );

        serials.Add( bardSpawner.Serial );

        serials.AddRange( AddMaginciaDeco2.ApplyDecoration( "haven-deco.json", Map.Trammel ) );

        return serials;
    }

    public void Down()
    {
    }
}
