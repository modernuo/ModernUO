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
using Server.Commands;
using Server.Logging;

namespace Badlands.Migrations;

public class AddCitadelTeleporters2 : IMigration
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AddCitadelTeleporters2));
    public DateTime MigrationTime { get; set; }

    public void Up()
    {
        var list = DecorationList.ReadAll( "./Assemblies/Data/Decorations/Citadel.cfg" );

        var count = 0;

        for ( var j = 0; j < list.Count; ++j )
        {
            count += list[j].Generate( [Map.Malas] );
        }

        logger.Information( "Generated {count} items", count );
    }

    public void Down()
    {
    }
}
