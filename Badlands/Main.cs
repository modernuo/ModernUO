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

using Badlands.Commands;
using Badlands.Items;
using Server;
using Server.Commands.Generic;
using Server.Logging;

namespace Badlands;

public static class Main
{
    private static readonly ILogger logger = LogFactory.GetLogger( typeof( StartingItems ) );

    public static void Configure()
    {
        logger.Information( "Configuring Badlands" );

        ServerConfiguration.SetSetting( "serverListing.serverName", "The Crossroads" );
        ServerConfiguration.SetSetting( "chat.enabled", true );

        TargetCommands.Register( new GotoSpawnerCommand() );
    }

    public static void Initialize()
    {
        logger.Information( "Performing migrations" );

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany( s => s.GetTypes() )
            .Where( p => typeof( IMigration ).IsAssignableFrom( p ) && p.IsClass );

        var (_, item) = World.Items.FirstOrDefault( e => e.Value?.GetType() == typeof( MigrationController ) );

        if ( item is not MigrationController migrationController )
        {
            logger.Warning( "No migration controller" );
            return;
        }

        var others = World.Items.Values.Where(
            e => e.GetType() == typeof( MigrationController ) && e.Serial != item.Serial
        );

        foreach ( var other in others )
        {
            logger.Debug( "Removing extra MigrationController = {serial}", other.Serial );

            other.Delete();
        }

        foreach ( var type in types )
        {
            if ( migrationController.HasMigration( type ) )
            {
                continue;
            }

            var method = type.GetMethod( "Up" );

            if ( method != null )
            {
                method.Invoke( Activator.CreateInstance( type ), null );

                migrationController.AddMigration( type );
            }
        }

        logger.Information( "Migrations complete" );
    }
}
