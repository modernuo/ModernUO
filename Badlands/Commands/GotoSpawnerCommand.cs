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

using Server.Commands.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Badlands.Commands;

public class GotoSpawnerCommand : BaseCommand
{
    public GotoSpawnerCommand()
    {
        AccessLevel = AccessLevel.GameMaster;
        Supports = CommandSupport.AllMobiles;
        Commands = ["GotoSpawner"];
        ObjectTypes = ObjectTypes.Mobiles;
    }

    public override void Execute( CommandEventArgs e, object obj )
    {
        if ( obj is not BaseCreature baseCreature)
        {
            return;
        }

        if ( baseCreature.Spawner is not null )
        {
            e.Mobile.MoveToWorld( baseCreature.Spawner.Location, baseCreature.Spawner.Map );
        }
    }
}
