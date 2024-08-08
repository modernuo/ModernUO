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
using Server.Logging;

namespace Badlands.Migrations;

[MigrationPriority( 0 )]
public class RemoveMaginciaDecorations4 : IMigration
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(StartingItems));
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var X1 = 3498;
        var Y1 = 2014;
        var X2 = 3833;
        var Y2 = 2296;

        var serials = new List<Serial>();

        foreach ( var map in new[] { Map.Felucca, Map.Trammel} )
        {
            var nonSpawners = World.Items.Values.Where( i => i.X > X1 && i.Y > Y1 && i.X < X2 && i.Y < Y2 && i is not Spawner && i is not Moongate );

            foreach ( var item in nonSpawners )
            {
                serials.Add( item.Serial );
                item.Delete();
            }
        }

        return serials;
    }

    public void Down()
    {
    }
}
