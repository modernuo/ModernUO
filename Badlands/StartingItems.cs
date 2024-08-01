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

using Badlands.Items;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Logging;

namespace Badlands;

public class StartingItems
{
    private static readonly ILogger logger = LogFactory.GetLogger( typeof( StartingItems ) );

    public static void Initialize()
    {
        EventSink.PlayerLogin += EventSink_PlayerLogin;
    }

    private static void EventSink_PlayerLogin( EventSink.PlayerLoginEventArgs eventArgs )
    {
        if ( eventArgs.Mobile?.Account is not Account account )
        {
            return;
        }

        if ( account.GetTag( nameof( StartingItems ) ) != null )
        {
            return;
        }

        logger.Information( $"Dispensing starting items to '{eventArgs.Mobile.Name}'" );

        account.SetTag( nameof( StartingItems ), DateTime.UtcNow.ToString( "o" ) );

        var items = new Item[]
        {
            new AccountBoundBankCheck( account, 25000 ),
            new RecallRune(),
            new RecallRune(),
            new RecallRune(),
            new MarkScroll(),
            new MarkScroll(),
            new MarkScroll(),
            new Runebook { CurCharges = 20, MaxCharges = 20 },
            new EtherealRottweiler()
        };

        foreach ( var item in items )
        {
            eventArgs.Mobile.AddToBackpack( item );
        }
    }
}
