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

using System.Text.Json;
using Badlands.Data.Citadel;
using Badlands.Items.Citadel;
using Server;
using Server.Logging;

namespace Badlands.Migrations;

public class AddCitadelTeleporters : IMigration
{
    private static readonly ILogger logger = LogFactory.GetLogger( typeof( AddCitadelTeleporters ) );
    public DateTime MigrationTime { get; set; }

    public void Up()
    {
        var fileName = Path.Combine( "./Assemblies/Data/citadel-secret-doors.json" );

        var entries = JsonSerializer.Deserialize<List<CitadelTeleporterEntry>>( File.ReadAllText( fileName ) );

        foreach ( var entry in entries )
        {
            var item = new SecretWall( entry.ID )
            {
                Hue = entry.Hue,
                PointDest = new Point3D( entry.Destination.X, entry.Destination.Y, entry.Destination.Z ),
                MapDest = Map.Maps[entry.Destination.Map],
                Movable = false
            };

            item.MoveToWorld( new Point3D( entry.X, entry.Y, entry.Z ), Map.Maps[entry.Map] );

            logger.Information( "Added Citadel Teleporter at {0}, {1}, {2}", item.X, item.Y, item.Z );
        }
    }

    public void Down()
    {
    }
}
