namespace Server.Items
{
  public class RandomWand
  {
    public static BaseWand CreateWand() => CreateRandomWand();

    public static BaseWand CreateRandomWand() => Loot.RandomWand();
  }
}