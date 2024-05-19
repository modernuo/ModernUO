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
using Server.Commands.Generic;

namespace Badlands.Commands;

public static class GotoSerialCommand
{
    public static void Configure()
    {
        CommandSystem.Register("GotoSerial", AccessLevel.Counselor, Execute);
    }

    public static void Execute( CommandEventArgs e )
    {
        var serial = 0;

        if ( e.Arguments[0].Contains( "0x", StringComparison.CurrentCultureIgnoreCase ) )
        {
            serial = Convert.ToInt32( e.Arguments[0], 16 );
        }
        else
        {
            serial = Convert.ToInt32( e.Arguments[0] );
        }

        var item = World.Items.FirstOrDefault( f => f.Key.Value == serial );

        if ( item.Value != null )
        {
            e.Mobile.MoveToWorld( item.Value.Location, item.Value.Map );

            return;
        }

        var mobile = World.Mobiles.FirstOrDefault( f => f.Key.Value == serial );

        if ( mobile.Value != null )
        {
            e.Mobile.MoveToWorld( mobile.Value.Location, mobile.Value.Map );
        }
    }
}
