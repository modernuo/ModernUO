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
using Server.Mobiles;

namespace Badlands.Migrations;

[MigrationPriority( 1 )]
public class RemoveMaginciaVendors3 : IMigration
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(StartingItems));
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        foreach (var map in new[] { Map.Felucca, Map.Trammel })
        {
            var region = Region.Find("Magincia", map);

            if (region == null)
            {
                continue;
            }

            var items = region.GetItems().Where(i => i is Spawner);

            foreach (var item in items)
            {
                if (item is Spawner spawner)
                {
                    foreach (var entry in spawner.Entries.ToArray())
                    {
                        var type = AssemblyHandler.FindTypeByName(entry.SpawnedName);

                        if (typeof(BaseVendor).IsAssignableFrom(type))
                        {
                            spawner.RemoveEntry( entry );
                            logger.Information($"Removed vendor spawner {entry.SpawnedName} from {map}");
                        }
                    }

                    spawner.Respawn();

                    if (spawner.Entries.Count == 0)
                    {
                        item.Delete();
                        serials.Add(item.Serial);
                        logger.Information($"Removed spawner {item.Serial} from {map}");
                    }
                }
            }
        }

        return serials;
    }

    public void Down()
    {
    }
}
