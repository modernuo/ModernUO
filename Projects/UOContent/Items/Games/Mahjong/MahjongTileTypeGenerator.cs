using System.Collections.Generic;

namespace Server.Engines.Mahjong
{
    public class MahjongTileTypeGenerator
    {
        public MahjongTileTypeGenerator()
        {
            LeftTileTypes = new List<MahjongTileType>(136);

            for (var i = 1; i <= 34; i++)
            {
                var tile = (MahjongTileType)i;
                LeftTileTypes.Add(tile);
                LeftTileTypes.Add(tile);
                LeftTileTypes.Add(tile);
                LeftTileTypes.Add(tile);
            }
        }

        public List<MahjongTileType> LeftTileTypes { get; }

        public MahjongTileType Next()
        {
            var next = LeftTileTypes.RandomElement();
            LeftTileTypes.Remove(next);

            return next;
        }
    }
}
