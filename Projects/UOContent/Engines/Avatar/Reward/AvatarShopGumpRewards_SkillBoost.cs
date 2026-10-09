using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Engines.Avatar;

public static partial class RewardFactory
{
    public static List<IReward> CreateSkillBoostRewards(PlayerMobile from, bool isPrimary, PlayerContext context)
    {
        var rewards = new List<IReward>();

        for (var i = 0; i < from.Skills.Length; i++)
        {
            var skill = from.Skills[i];
            if (skill.SkillName.IsExcludedSkill() || skill.IsSecondarySkill() == isPrimary)
            {
                continue;
            }

            const int NEOPHYTE_SKILL_VALUE = 300;
            var archiveValue = context.Skills[skill.SkillName];
            if (!context.UnlockFullSkillArchive && archiveValue < NEOPHYTE_SKILL_VALUE)
            {
                continue;
            }

            var maxValue = Math.Min(archiveValue / 10f, context.GetRecordedSkillCap());
            var maxValueFixedPoint = (int)(maxValue * 10);
            rewards.Add(
                ActionReward.Create(
                    maxValueFixedPoint <= skill.BaseFixedPoint,
                    AvatarShopGump.COST_FREE,
                    AvatarShopGump.NO_ITEM_ID,
                    skill.Name,
                    $"Raise your skill in {skill.Name} up to {maxValue:n1}",
                    () => skill.RaiseTo(from, maxValueFixedPoint)
                ).WithPrereq(
                    isPrimary ? context.UnlockPrimarySkillBoost : context.UnlockSecondarySkillBoost,
                    isPrimary ? "Requires Jack of No Trades to be unlocked." : "Requires Artisan's Mastery to be unlocked."
                )
            );
        }

        return rewards;
    }
}
