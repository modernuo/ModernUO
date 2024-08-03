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

public class EliteNinjaWarriorSpawnerToEliteNinjaAndGrandMage : IMigration
{
    private static readonly ILogger logger = LogFactory.GetLogger( typeof(EliteNinjaWarriorSpawnerToEliteNinjaAndGrandMage) );
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        var spawners = World.Items.Values.OfType<Spawner>().ToList();

        foreach ( var spawner in spawners )
        {
            foreach ( var spawnerEntry in spawner.Entries )
            {
                if ( spawnerEntry.SpawnedName.ToLower().Trim() == "eliteninjawarrior" )
                {
                    spawnerEntry.SpawnedName = "eliteninja";
                    serials.Add( spawner.Serial );
                    logger.Information( $"Updated EliteNinjaWarrior spawner to EliteNinja: {spawner.Serial}" );
                } else if ( spawnerEntry.SpawnedName.ToLower().Trim() == "magedragonsflamemage")
                {
                    spawnerEntry.SpawnedName = "dragonsflamegrandmage";
                    serials.Add(spawner.Serial);
                    logger.Information($"Updated MageDragonsFlameMage spawner to DragonsFlameGrandMage: {spawner.Serial}");
                }
            }
        }

        return serials;
    }

    public void Down()
    {
    }
}
