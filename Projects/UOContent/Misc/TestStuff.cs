using System;

namespace Server.Misc
{
    public class TestStuff
    {
        public static void Initialize()
        {
            var tiles = Map.Trammel.Tiles.GetStaticTiles(3486, 2573);

            foreach (var tile in tiles)
            {
                Console.WriteLine("Static {0:X} at Offset {1} {2} {3}", tile.ID, tile.X, tile.Y, tile.Z);
            }
        }
    }
}
