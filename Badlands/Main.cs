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

using System.Reflection;
using System.Text.Json;
using Badlands.Commands;
using Badlands.Items;
using Badlands.Items.Decorations;
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

        logger.Information( "Applying Decorations" );

        var decorationFiles = Directory.EnumerateFiles( "./Assemblies/Data/Decorations/", "*.json" );

        foreach ( var file in decorationFiles )
        {
            var json = File.ReadAllText( file );

            var decorations = JsonSerializer.Deserialize<List<DecorationEntry>>( json );

            foreach ( var decoration in decorations )
            {
                var existing = World.Items.Values.FirstOrDefault(
                    x => x.X == decoration.X && x.Y == decoration.Y && x.Z == decoration.Z && x.ItemID == decoration.ID
                );

                if ( existing != null )
                {
                    // Just set the hue if different for now
                    if ( existing.Hue != decoration.Hue )
                    {
                        existing.Hue = decoration.Hue;
                    }

                    continue;
                }

                var type = FindItemByCliloc( decoration.Cliloc );

                if ( type != null )
                {
                    if ( Activator.CreateInstance( type ) is Item i )
                    {
                        i.Hue = decoration.Hue;
                        i.MoveToWorld( new Point3D( decoration.X, decoration.Y, decoration.Z ), Map.Maps[decoration.Map] );

                        logger.Information(
                            $"Added decoration {decoration.Cliloc} at {decoration.X}, {decoration.Y}, {decoration.Z}"
                        );
                    }
                }
            }
        }


        logger.Information( "Decorations complete" );
    }

    private static Type? FindItemByCliloc( int cliloc )
    {
        // findall derivied types of Server.Item with constuctor with attribute Constructible
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany( s => s.GetTypes() )
            .Where( p => typeof( Item ).IsAssignableFrom( p ) && p.IsClass );

        foreach ( var type in types )
        {
            var ctors = type.GetConstructors();

            foreach ( var ctor in ctors )
            {
                var attr = ctor.GetCustomAttribute<ConstructibleAttribute>();

                if ( attr != null )
                {
                    var param = ctor.GetParameters();

                    if ( param.Length != 0 )
                    {
                        continue;
                    }

                    var item = Activator.CreateInstance( type ) as Item;

                    if ( item?.LabelNumber == cliloc )
                    {
                        return type;
                    }
                }
            }
        }

        return null;
    }
}
