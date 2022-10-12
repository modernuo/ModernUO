namespace Server.Engines.Mahjong;

public class MahjongTileTypeGenerator
{
    private MahjongTileType[] _leftTileTypes;
    private int _nextTile;

    public MahjongTileTypeGenerator()
    {
        _leftTileTypes = new MahjongTileType[136];

        for (int i = 1, j = 0; i <= 34; i++)
        {
            var tile = (MahjongTileType)i;
            _leftTileTypes[j++] = tile;
            _leftTileTypes[j++] = tile;
            _leftTileTypes[j++] = tile;
            _leftTileTypes[j++] = tile;
        }

        _leftTileTypes.Shuffle();
        _leftTileTypes.Shuffle();
        _leftTileTypes.Shuffle();
        _leftTileTypes.Shuffle();
    }

    public MahjongTileType Next() => _leftTileTypes[_nextTile++];
}
