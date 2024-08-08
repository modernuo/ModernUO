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
using Badlands.Commands;
using Badlands.Items;
using Badlands.Migrations;
using Server;
using Server.Commands.Generic;
using Server.Logging;

namespace Badlands;

public static class Main
{
    private static readonly ILogger logger = LogFactory.GetLogger( typeof( StartingItems ) );
    private static MigrationPersistence _migrationPersistence;

    public static void Configure()
    {
        logger.Information( "Configuring Badlands" );

        ServerConfiguration.SetSetting( "serverListing.serverName", "The Crossroads" );
        ServerConfiguration.SetSetting( "chat.enabled", true );

        TargetCommands.Register( new GotoSpawnerCommand() );

        _migrationPersistence = new MigrationPersistence();
        _migrationPersistence.Register();
    }

    public static void Initialize()
    {
        if ( World.Mobiles.Count == 0 )
        {
            logger.Information( "No mobiles found, skipping migrations" );
            return;
        }

        var mobile = World.Mobiles.Values.FirstOrDefault( e => e.Serial == 1 );

        if ( mobile != null && mobile.AccessLevel != AccessLevel.Owner )
        {
            mobile.AccessLevel = AccessLevel.Owner;
        }

        logger.Information( "Performing migrations" );

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IMigration).IsAssignableFrom(p) && p.IsClass);

        var sortedTypes = types.OrderBy(
            e => e.GetCustomAttribute<MigrationPriorityAttribute>( false ) != null
                ? e.GetCustomAttribute<MigrationPriorityAttribute>( false ).Priority
                : -1
        );

        foreach (var type in sortedTypes)
        {
            if (_migrationPersistence.Contains( type))
            {
                continue;
            }

            logger.Information("Applying migration {name}", type.FullName);

            var method = type.GetMethod("Up");

            if (method != null)
            {
                var items = method.Invoke(Activator.CreateInstance(type), null);

                List<Serial> serials = new();

                if ( items is List<Serial> serialList )
                {
                    serials = serialList;
                }

                _migrationPersistence.Add(new MigrationEntry
                {
                    Type = type,
                    Name = type.FullName,
                    MigrationDateTime = DateTime.UtcNow,
                    Entities = serials
                });
            }

            logger.Information("Finished migration {name}", type.FullName);
        }

        logger.Information("Migrations complete");
    }
}
