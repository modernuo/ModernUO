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

public class AddTravestyTableTeleporter : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        var table = new CitadelAltar();

        table.MoveToWorld( new Point3D( 90, 1884, 0 ), Map.Malas );

        var exitTeleporter = new PeerlessTeleporter( table );

        exitTeleporter.MoveToWorld( new Point3D( 114, 1955, 0 ), Map.Malas );

        serials.Add( table.Serial );
        serials.Add( exitTeleporter.Serial );

        return serials;
    }

    public void Down()
    {
    }
}
