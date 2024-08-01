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

public static class Decorations
{
    private static readonly ILogger logger = LogFactory.GetLogger( typeof( StartingItems ) );

    public static void ApplyDecoration( string fileName )
    {
        logger.Information( "Applying Decorations" );

        var decorationFile = Path.Combine( "./Assemblies/Data/Decorations/", fileName );

        if ( !File.Exists( decorationFile ) )
        {
            logger.Warning( "Decoration file does not exist, {}", decorationFile );
        }

        var json = File.ReadAllText( decorationFile );

        var decorations = JsonSerializer.Deserialize<List<DecorationEntry>>( json );

        foreach ( var decoration in decorations )
        {
            var existing = World.Items.Values.FirstOrDefault(
                x => x.X == decoration.X && x.Y == decoration.Y && x.Map == Map.Maps[decoration.Map] &&
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
                    i.MoveToWorld( new Point3D( decoration.X, decoration.Y, decoration.Z ), Map.Maps[decoration.Map] );

                    logger.Information(
                        $"Added decoration {decoration.Cliloc} at {decoration.X}, {decoration.Y}, {decoration.Z}. Name = {i.Name}"
                    );
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

                i.MoveToWorld( new Point3D( decoration.X, decoration.Y, decoration.Z ), Map.Maps[decoration.Map] );

                logger.Information(
                    $"Added decoration {decoration.ID} at {decoration.X}, {decoration.Y}, {decoration.Z}, Name = {i.Name}"
                );
            }
        }

        logger.Information( "Decorations complete" );
    }

    private static Type? FindItemByClilocAndID( int cliloc, int id )
    {
        // findall derivied types of Server.Item with constuctor with attribute Constructible
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany( s => s.GetTypes() )
            .Where(
                p => typeof( Item ).IsAssignableFrom( p ) && p.IsClass && !typeof( BaseMulti ).IsAssignableFrom( p ) &&
                     !typeof( BaseDoor ).IsAssignableFrom( p ) && !typeof( BaseAddon ).IsAssignableFrom( p )
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
