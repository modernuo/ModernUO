using System.Collections;

namespace Server.Engines.Mahjong
{
  public class MahjongTileTypeGenerator
  {
    public MahjongTileTypeGenerator(int count)
    {
      LeftTileTypes = new ArrayList(34 * count);

      for (int i = 1; i <= 34; i++)
      for (int j = 0; j < count; j++)
        LeftTileTypes.Add((MahjongTileType)i);
    }

    public ArrayList LeftTileTypes{ get; }

    public MahjongTileType Next()
    {
      int random = Utility.Random(LeftTileTypes.Count);
      MahjongTileType next = (MahjongTileType)LeftTileTypes[random];
      LeftTileTypes.RemoveAt(random);

      return next;
    }
  }
}