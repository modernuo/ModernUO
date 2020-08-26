namespace Server.Items
{
  public enum DawnsMusicRarity
  {
    Common,
    Uncommon,
    Rare
  }

  public class DawnsMusicInfo
  {
    public DawnsMusicInfo(int name, DawnsMusicRarity rarity)
    {
      Name = name;
      Rarity = rarity;
    }

    public int Name { get; }

    public DawnsMusicRarity Rarity { get; }
  }
}