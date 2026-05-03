namespace Server.Items;

public static class TrophyRankExtensions
{
    /// <summary>
    /// Returns a static lowercase string for the given <see cref="TrophyRank"/> value.
    /// Avoids the two-allocation <c>rank.ToString().ToLower()</c> pattern at interpolation sites.
    /// </summary>
    public static string LowerName(this TrophyRank rank) =>
        rank switch
        {
            TrophyRank.Bronze => "bronze",
            TrophyRank.Silver => "silver",
            TrophyRank.Gold   => "gold",
            _                 => rank.ToString().ToLowerInvariant()
        };
}
