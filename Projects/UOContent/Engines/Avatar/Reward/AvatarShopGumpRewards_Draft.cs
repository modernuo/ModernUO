using System;
using System.Collections.Generic;
using System.Linq;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Avatar;

public static partial class RewardFactory
{
    public static List<IReward> CreateDraftRewards(PlayerMobile from, PlayerContext context, bool isInSanctuary)
    {
        if (!context.DraftModeEnabled)
        {
            return [];
        }

        if (context.SelectedTemplate == AvatarStarterTemplates.None)
        {
            return
            [
                ActionReward.Create(
                    false,
                    AvatarShopGump.COST_NO_BUY,
                    AvatarShopGump.NO_ITEM_ID,
                    "No Template Selected",
                    "You have not selected a template. Please select a template to continue.",
                    () => { }
                ).AsStatic()
            ];
        }

        if (context.DraftPicksSpent >= context.DraftPicksAvailable)
        {
            if (context.DraftLevel == 1)
            {
                return
                [
                    ActionReward.Create(
                        false,
                        AvatarShopGump.COST_FREE,
                        AvatarShopGump.NO_ITEM_ID,
                        "My template is unplayable",
                        "If you believe your template is unplayable, you may restart the drafting process.",
                        () =>
                        {
                            context.SetDraftModeEnabled(from, false);
                            context.SetDraftModeEnabled(from, true);
                        }
                    ).AsStatic()
                ];
            }

            var nextPickLevel =
                context.DraftLevel + Constants.DRAFT_LEVELS_PER_PICK - context.DraftLevel % Constants.DRAFT_LEVELS_PER_PICK;

            return
            [
                context.DraftLevel == Constants.DRAFT_MAX_LEVEL
                    ? ActionReward.Create(
                        false,
                        AvatarShopGump.COST_NO_BUY,
                        AvatarShopGump.NO_ITEM_ID,
                        "No More Picks",
                        "You have reached the maximum level for Draft.",
                        () => { }
                    ).AsStatic()
                    : ActionReward.Create(
                        false,
                        AvatarShopGump.COST_NO_BUY,
                        AvatarShopGump.NO_ITEM_ID,
                        "Next Pick",
                        $"You have no more picks available. Your next pick will be available at level {nextPickLevel}.",
                        () => { }
                    ).AsStatic()
            ];
        }

        var allSkills = new List<SkillName>();
        foreach (var skillInfo in SkillInfo.Table)
        {
            var skillName = (SkillName)skillInfo.SkillID;
            if (skillName.IsExcludedSkill() || context.IsSkillDrafted(skillName) ||
                !context.HasPrerequisiteSkills(skillName) || context.DraftBannedSkills.Contains(skillName))
            {
                continue;
            }

            allSkills.Add(skillName);
        }

        var isInitialDraft = context.DraftPicksSpent < Constants.DRAFT_START_PICK_AMOUNT;
        var skills = context.DraftPicksSpent <= Constants.DRAFT_START_PICK_AMOUNT
            ? allSkills.Where(context.IsSmartSkill)
            : allSkills;

        var rewards = new List<IReward>();
        foreach (var skillName in skills)
        {
            var skill = from.Skills[skillName];
            const int NEOPHYTE_SKILL_VALUE = 300;
            var archiveValue = context.Skills[skill.SkillName];
            if (isInitialDraft)
            {
                archiveValue = Math.Max(archiveValue, NEOPHYTE_SKILL_VALUE);
            }

            var maxValue = Math.Min(archiveValue / 10f, context.GetRecordedSkillCap());
            var maxValueFixedPoint = (int)(maxValue * 10);
            rewards.Add(
                ActionReward.Create(
                    false,
                    AvatarShopGump.COST_FREE,
                    AvatarShopGump.NO_ITEM_ID,
                    skill.Name,
                    isInSanctuary
                        ? $"Add this skill to your list of available skills and increase it to up to {maxValue:n1}"
                        : "Add this skill to your list of available skills.",
                    () =>
                    {
                        context.ClearRewardCache(Categories.Draft);
                        context.AddDraftedSkill(skill.SkillName);
                        skill.SetLockNoRelay(SkillLock.Up);
                        from.NetState.SendSkillChange(skill);

                        if (isInSanctuary)
                        {
                            skill.RaiseTo(from, maxValueFixedPoint);
                        }
                    }
                ).WithPrereq(context.DraftModeEnabled, "Requires Draft Mode to be enabled.").AllowSelectAnywhere()
            );
        }

        return rewards;
    }
}
