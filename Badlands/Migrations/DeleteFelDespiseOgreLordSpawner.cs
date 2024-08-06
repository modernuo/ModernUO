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
using Server.Logging;

namespace Badlands.Migrations;

public class DeleteFelDespiseOgreLordSpawner : IMigration
{
    public DateTime MigrationTime { get; set; }
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(DeleteFelDespiseOgreLordSpawner));

    public List<Serial> Up()
    {
        var spawner = World.Items.Values.FirstOrDefault(i => i is Spawner s && s.Location == new Point3D(5558, 824, 45) && s.Map == Map.Felucca);

        if ( spawner != null )
        {
            spawner.Delete();
            logger.Information("Deleted Felucca Despise Ogre Lord Spawner");
        }

        return new List<Serial>();
    }

    public void Down()
    {
    }
}
