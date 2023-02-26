namespace Server.Items;

public enum DawnsMusicRarity
{
    Common,
    Uncommon,
    Rare
}

public record DawnsMusicInfo(int Name, DawnsMusicRarity Rarity);
