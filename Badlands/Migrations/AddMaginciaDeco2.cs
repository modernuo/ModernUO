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
using Badlands.Items.Decorations;
using Server;
using Server.Items;
using Server.Logging;

namespace Badlands.Migrations;

[MigrationPriority( 2 )]
public class AddMaginciaDeco2 : IMigration
{
    private static readonly ILogger logger = LogFactory.GetLogger( typeof( AddMaginciaDeco2 ) );
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        foreach ( var map in new[] { Map.Felucca, Map.Trammel} )
        {
            serials.AddRange( ApplyDecoration( "mag-deco.json", map ) );
        }

        return serials;
    }

    public void Down()
    {
    }

    public static List<Serial> ApplyDecoration( string fileName, Map map )
    {
        logger.Information( "Applying Decorations" );

        var serials = new List<Serial>();

        var decorationFile = Path.Combine( "./Data/Decorations/", fileName );

        if ( !File.Exists( decorationFile ) )
        {
            logger.Warning( "Decoration file does not exist, {}", decorationFile );
        }

        var json = File.ReadAllText( decorationFile );

        var decorations = JsonSerializer.Deserialize<List<DecorationEntry>>( json );

        foreach ( var decoration in decorations )
        {
            var existing = World.Items.Values.FirstOrDefault(
                x => x.X == decoration.X && x.Y == decoration.Y && x.Map == map &&
                     ( x.Z == decoration.Z || true ) &&
                     x.ItemID == decoration.ID
            );

            if ( existing != null )
            {
                continue;
            }

            var type = FindItemByClilocAndID( decoration.Cliloc, decoration.ID );

            if ( type != null )
            {
                if ( Activator.CreateInstance( type ) is Item i )
                {
                    i.Hue = decoration.Hue;
                    i.ItemID = decoration.ID;
                    i.Movable = false;
                    i.MoveToWorld( new Point3D( decoration.X, decoration.Y, decoration.Z ), map );

                    logger.Information(
                        $"Added decoration {decoration.Cliloc} at {decoration.X}, {decoration.Y}, {decoration.Z}. Name = {i.Name}"
                    );

                    serials.Add( i.Serial );
                }
            }
            else
            {
                var i = new LocalizedStatic( decoration.ID, decoration.Cliloc )
                {
                    Hue = decoration.Hue,
                    ItemID = decoration.ID,
                    Movable = false
                };

                i.MoveToWorld( new Point3D( decoration.X, decoration.Y, decoration.Z ), map );

                logger.Information(
                    $"Added decoration {decoration.ID} at {decoration.X}, {decoration.Y}, {decoration.Z}, Name = {i.Name}"
                );

                serials.Add( i.Serial );
            }
        }

        logger.Information( "Decorations complete" );

        return serials;
    }

    private static Type? FindItemByClilocAndID( int cliloc, int id )
    {
        // findall derivied types of Server.Item with constuctor with attribute Constructible
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany( s => s.GetTypes() )
            .Where(
                p => typeof( Item ).IsAssignableFrom( p ) && p.IsClass && !typeof( BaseMulti ).IsAssignableFrom( p ) &&
                     !typeof( BaseDoor ).IsAssignableFrom( p ) && !typeof( BaseAddon ).IsAssignableFrom( p ) &&
                     ( p.BaseType == typeof( Item ) || p.BaseType == typeof( Container ) )
            );

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

                    var flipable = type.GetCustomAttribute<FlippableAttribute>();

                    if ( item?.LabelNumber == cliloc &&
                         ( item?.ItemID == id || flipable != null && flipable.ItemIDs.Contains( id ) ) )
                    {
                        return type;
                    }
                }
            }
        }

        return null;
    }
}
