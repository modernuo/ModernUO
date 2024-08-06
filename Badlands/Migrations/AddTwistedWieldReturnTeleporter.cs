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

public class AddTwistedWieldReturnTeleporter : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        var teleporter = new Teleporter( new Point3D( 1450, 1470, -22 ), Map.Ilshenar, true );

        teleporter.MoveToWorld( new Point3D( 2186, 1251, 4 ), Map.Ilshenar );

        serials.Add( teleporter.Serial );

        var sparkle = new Static( 0x373a );

        sparkle.MoveToWorld( new Point3D( 2186, 1251, 4 ), Map.Ilshenar );

        serials.Add( sparkle.Serial );

        return serials;
    }

    public void Down()
    {
    }
}
