using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Engines.Avatar;

public static partial class RewardFactory
{
    public const int ONE_HUNDRED_GOLD = 1000;
    public const int ONE_THOUSAND_GOLD = 10000;
    public const int TEN_GOLD = 100;

    public static List<IReward> CreateRewards(
        PlayerMobile from, Categories selectedCategory, PlayerContext context, bool isInSanctuary
    ) =>
        selectedCategory switch
        {
            Categories.Ascensions       => CreateAscensionRewards(from, context),
            Categories.Templates        => CreateTemplateRewards(from, context),
            Categories.FullSkillArchive => CreateSkillArchiveRewards(from, context),
            Categories.PrimaryBoosts    => CreateSkillBoostRewards(from, true, context),
            Categories.SecondaryBoosts  => CreateSkillBoostRewards(from, false, context),
            Categories.Items            => CreateItemRewards(from, context),
            Categories.Draft            => CreateDraftRewards(from, context, isInSanctuary),
            _                           => null // Information and Statistics have no rewards
        };

    private static int ExponentialCost(int baseCost, int level)
    {
        var cost = baseCost;
        for (var i = 0; i < level; i++)
        {
            cost *= 2;
        }

        return cost;
    }

    private static int SecondOrderCost(double baseCost, int level) =>
        (int)(baseCost * Math.Pow(level, 2) + baseCost * level);
}
