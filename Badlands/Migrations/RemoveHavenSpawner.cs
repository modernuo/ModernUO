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

namespace Badlands.Migrations;

public class RemoveHavenSpawner : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var item = World.Items.FirstOrDefault(
            i => i.Value.Location == new Point3D( 3485, 2595, 12 ) && i.Value.Map == Map.Trammel
        );

        item.Value?.Delete();

        return [item.Key];
    }

    public void Down()
    {
    }
}
