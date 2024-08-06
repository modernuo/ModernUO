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

using System.Text.Json;
using System.Text.Json.Serialization;
using Badlands.Items;
using Server;
using Server.Items;
using Server.Logging;

namespace Badlands.Commands;

public class GenRuneLibrary
{
    private static readonly ILogger logger = LogFactory.GetLogger( typeof( GenRuneLibrary ) );

    public static void Configure()
    {
        CommandSystem.Register( "GenRuneLibrary", AccessLevel.Owner, Generate );
    }

    private static void Generate( CommandEventArgs e )
    {
        var directory = "./Assemblies/Data/Runebooks/";

        if ( !Path.Exists( directory ) )
        {
            e.Mobile.SendMessage( "Directory does not exist" );
            return;
        }

        if ( File.Exists( "./Assemblies/Data/runebook-benches.json" ) )
        {
            var entries = JsonSerializer.Deserialize<RunebookEntry[]>(File.ReadAllText("./Assemblies/Data/runebook-benches.json"));

            foreach ( var data in entries )
            {
                var bench = new WoodenBench();

                bench.MoveToWorld(new Point3D(e.Mobile.X + data.XOffset, e.Mobile.Y + data.YOffset, e.Mobile.Z + data.ZOffset), e.Mobile.Map);

            }
        }

        var files = Directory.GetFiles( directory, "*.json" );

        if ( files.Length == 0 )
        {
            e.Mobile.SendMessage( "No files found in directory" );
            return;
        }

        foreach ( var file in files.Reverse() )
        {
            var data = JsonSerializer.Deserialize<RunebookEntry>( File.ReadAllText( file ) );

            var runebook = new DemiseRunebook( data.Serial, data.X, data.Y );

            runebook.Name = data.Name;
            runebook.Hue = data.Hue;

            foreach ( var entry in data.Entries )
            {
                var rune = new RecallRune
                {
                    Marked = true,
                    TargetMap = Map.Maps[entry.Map]
                };
                var z = Map.Maps[entry.Map].GetAverageZ( entry.X, entry.Y );
                rune.Target = new Point3D( entry.X, entry.Y, z );

                var x = entry.X;
                var y = entry.Y;

                var points = GetPoints( x, y );

                bool found = false;

                if ( !rune.TargetMap.CanSpawnMobile( x, y, z ) )
                {
                    foreach ( var point in points )
                    {
                        var newZ = Map.Maps[entry.Map].GetAverageZ(point.X, point.Y);

                        if ( rune.TargetMap.CanSpawnMobile( point.X, point.Y, newZ ) )
                        {
                            x = point.X;
                            y = point.Y;
                            z = newZ;
                            found = true;
                            break;
                        }
                    }
                }
                else
                {
                    found = true;
                }

                if ( !found )
                {
                    e.Mobile.SendMessage( $"Cannot find point from {entry.Name}" );
                    continue;
                }

                rune.Target = new Point3D( x, y, z );
                rune.Description = entry.Name;

                runebook.OnDragDrop( e.Mobile, rune );
            }

            runebook.MoveToWorld( new Point3D( e.Mobile.X + data.XOffset, e.Mobile.Y + data.YOffset, e.Mobile.Z + data.ZOffset ), e.Mobile.Map );
        }
    }

    private static List<(int X, int Y)> GetPoints( int x, int y )
    {
        var points = new List<(int, int)>();

        for ( var i = 1; i <= 10; i++ )
        {
            // Add points to the left and right of x
            points.Add( ( x - i, y ) );
            points.Add( ( x + i, y ) );

            // Add points above and below y
            points.Add( ( x, y - i ) );
            points.Add( ( x, y + i ) );
        }

        // Sort the points by their distance to (x, y)
        points.Sort(
            ( p1, p2 ) =>
            {
                var d1 = Math.Abs( p1.Item1 - x ) + Math.Abs( p1.Item2 - y );
                var d2 = Math.Abs( p2.Item1 - x ) + Math.Abs( p2.Item2 - y );
                return d1.CompareTo( d2 );
            }
        );

        return points;
    }

    private class RunebookEntry
    {
        [JsonPropertyName( "z" )]
        public int Z { get; set; }

        [JsonPropertyName( "xoffset" )]
        public int XOffset { get; set; }

        [JsonPropertyName( "name" )]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName( "serial" )]
        public int Serial { get; set; }

        [JsonPropertyName( "zoffset" )]
        public int ZOffset { get; set; }

        [JsonPropertyName( "hue" )]
        public int Hue { get; set; }

        [JsonPropertyName( "yoffset" )]
        public int YOffset { get; set; }

        [JsonPropertyName( "y" )]
        public int Y { get; set; }

        [JsonPropertyName( "x" )]
        public int X { get; set; }

        [JsonPropertyName( "entries" )]
        public RunebookEntryEntry[] Entries { get; set; }
    }

    private class RunebookEntryEntry
    {
        [JsonPropertyName( "x" )]
        public int X { get; set; }

        [JsonPropertyName( "y" )]
        public int Y { get; set; }

        [JsonPropertyName( "name" )]
        public string Name { get; set; }

        [JsonPropertyName( "map" )]
        public int Map { get; set; }
    }
}
