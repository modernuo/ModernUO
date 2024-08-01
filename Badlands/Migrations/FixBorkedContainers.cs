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

public class FixBorkedContainers : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        var brokenContainers = World.Items.Values.Where( i => i is Container { GumpID: 0 } ).ToList();

        foreach ( var item in brokenContainers )
        {
            if ( item is Container container )
            {
                container.GumpID = container.DefaultGumpID;
                container.MaxItems = container.DefaultMaxItems;
                container.LiftOverride = false;
                container.DropSound = container.DefaultDropSound;
            }

            serials.Add( item.Serial );
        }

        return serials;
    }

    public void Down()
    {
    }
}
