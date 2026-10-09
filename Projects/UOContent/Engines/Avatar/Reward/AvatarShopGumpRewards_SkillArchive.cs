using System.Collections.Generic;
using System.Linq;
using Server.Mobiles;

namespace Server.Engines.Avatar;

public static partial class RewardFactory
{
    public static List<IReward> CreateSkillArchiveRewards(PlayerMobile from, PlayerContext context)
    {
        var skills = new List<Skill>();
        for (var i = 0; i < from.Skills.Length; i++)
        {
            var skill = from.Skills[i];
            if (skill.SkillName.IsExcludedSkill() || context.Skills[skill.SkillName] < 1)
            {
                continue;
            }

            skills.Add(skill);
        }

        var rewards = new List<IReward>();
        foreach (var skill in skills.OrderBy(s => s.IsSecondarySkill()).ThenBy(s => s.Name))
        {
            var value = context.Skills[skill.SkillName] / 10f;

            rewards.Add(
                ActionReward.Create(
                    AvatarShopGump.COST_FREE,
                    AvatarShopGump.NO_ITEM_ID,
                    skill.Name,
                    value.ToString("n1"),
                    () => AvatarConfirm.Send(
                        from,
                        "Reduce Skill to Zero?",
                        $"Are you sure you want to lower this skill to zero? This is a <BASEFONT COLOR={AvatarConfirm.Red}>destructive action</BASEFONT> and cannot be undone.",
                        () =>
                        {
                            from.SendMessage($"You have reduced '{skill.Name}' to zero.");
                            skill.Base = 0;
                        }
                    )
                )
            );
        }

        return rewards;
    }
}
