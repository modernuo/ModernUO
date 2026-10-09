using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Engines.Avatar;

public static partial class RewardFactory
{
    public static List<IReward> CreateAscensionRewards(PlayerMobile from, PlayerContext context)
    {
        var currentErudianBonus = context.GetRecordedSkillCap();
        var erudianLevel = context.RecordedSkillCapLevel + 1;
        var erudianCapCost = currentErudianBonus switch
        {
            < 70  => SecondOrderCost(100, erudianLevel),
            < 90  => SecondOrderCost(200, erudianLevel),
            < 100 => SecondOrderCost(400, erudianLevel),
            < 105 => SecondOrderCost(800, erudianLevel),
            < 110 => SecondOrderCost(1000, erudianLevel),
            < 115 => SecondOrderCost(1200, erudianLevel),
            < 120 => SecondOrderCost(2400, erudianLevel),
            _     => SecondOrderCost(4800, erudianLevel)
        };

        var currentSkillCap = Constants.SKILL_CAP_BASE / 10 + context.SkillCapLevel * Constants.SKILL_CAP_PER_LEVEL;
        var skillCapCost = currentSkillCap switch
        {
            < 400  => SecondOrderCost(200, 1),
            < 500  => SecondOrderCost(800, 1),
            < 600  => SecondOrderCost(1600, 1),
            < 700  => SecondOrderCost(3200, 1),
            < 800  => SecondOrderCost(6400, 1),
            < 900  => SecondOrderCost(12800, 1),
            < 1000 => SecondOrderCost(51200, 1),
            _      => SecondOrderCost(4000, 1)
        };

        var statCapCost = context.StatCapLevel switch
        {
            < 10  => SecondOrderCost(200, 1),
            < 20  => SecondOrderCost(600, 1),
            < 30  => SecondOrderCost(1200, 1),
            < 40  => SecondOrderCost(2400, 1),
            < 50  => SecondOrderCost(4800, 1),
            < 60  => SecondOrderCost(9600, 1),
            < 70  => SecondOrderCost(19200, 1),
            < 80  => SecondOrderCost(38400, 1),
            < 90  => SecondOrderCost(76800, 1),
            < 100 => SecondOrderCost(153600, 1),
            < 110 => SecondOrderCost(307200, 1),
            < 120 => SecondOrderCost(614400, 1),
            < 130 => SecondOrderCost(1228800, 1),
            < 140 => SecondOrderCost(2457600, 1),
            _     => SecondOrderCost(4915200, 1)
        };

        var pointGainRateCost = SecondOrderCost(50, context.PointGainRateLevel + 1);
        var skillGainRateCost = ExponentialCost(2000, context.SkillGainRateLevel + 1);

        return
        [
            !context.HasSafetyDepositBox
                ? ActionReward.Create(
                    context.HasSafetyDepositBox,
                    ONE_HUNDRED_GOLD,
                    AvatarShopGump.NO_ITEM_ID,
                    "Persistent Storage Container",
                    "A safety deposit box is placed in your bankbox. Items in this container will persist through death.",
                    () =>
                    {
                        context.GetOrCreateSafetyDepositBox(from);
                        context.SafetyDepositBoxLevel = Math.Max(1, context.SafetyDepositBoxLevel);
                        from.SendMessage("A safety deposit box has been placed in your bank box.");
                    }
                )
                : ActionReward.Create(
                    Constants.SAFETY_DEPOSIT_BOX_MAX_LEVEL <= context.SafetyDepositBoxLevel,
                    SecondOrderCost(ONE_HUNDRED_GOLD, context.SafetyDepositBoxLevel + 1),
                    AvatarShopGump.NO_ITEM_ID,
                    $"Safety Deposit Box ({context.SafetyDepositBoxLevel} of {Constants.SAFETY_DEPOSIT_BOX_MAX_LEVEL})",
                    $"Increase storage capacity of your safety deposit box. Current capacity: {context.SafetyDepositBoxLevel}",
                    () =>
                    {
                        var box = context.GetOrCreateSafetyDepositBox(from);
                        context.SafetyDepositBoxLevel += 1;
                        box.MaxItems = context.SafetyDepositBoxLevel;
                        from.SendMessage($"Your safety deposit box can now hold {context.SafetyDepositBoxLevel} items.");
                    }
                ),
            ActionReward.Create(
                context.UnlockRecordSkillCaps,
                ONE_HUNDRED_GOLD,
                AvatarShopGump.NO_ITEM_ID,
                "Erudian Teachings",
                "Reinforce your mind. Higher learning will become second nature.",
                () =>
                {
                    context.UnlockRecordSkillCaps = true;
                    context.ClearRewardCache(Categories.PrimaryBoosts);
                    context.ClearRewardCache(Categories.SecondaryBoosts);
                    from.SendMessage("Your increased skill caps are now permanently unlocked.");
                }
            ),
            ActionReward.Create(
                context.UnlockPrimarySkillBoost,
                2 * ONE_HUNDRED_GOLD,
                AvatarShopGump.NO_ITEM_ID,
                "Jack of No Trades",
                "Learn from the greatest masters. Unlock the ability to restore Primary skills.",
                () =>
                {
                    context.UnlockPrimarySkillBoost = true;
                    context.ClearRewardCache(Categories.PrimaryBoosts);
                    context.ClearRewardCache(Categories.SecondaryBoosts);
                    from.SendMessage("Some of your Primary skills are now available in the Skill Archive.");
                }
            ).WithPrereq(context.UnlockRecordSkillCaps, "Requires Erudian Teachings to be unlocked."),
            ActionReward.Create(
                context.UnlockSecondarySkillBoost,
                2 * ONE_HUNDRED_GOLD,
                AvatarShopGump.NO_ITEM_ID,
                "Artisan's Mastery",
                "Master the crafts. Unlock the ability to restore Secondary skills.",
                () =>
                {
                    context.UnlockSecondarySkillBoost = true;
                    context.ClearRewardCache(Categories.PrimaryBoosts);
                    context.ClearRewardCache(Categories.SecondaryBoosts);
                    from.SendMessage("Some of your Secondary skills are now available in the Skill Archive.");
                }
            ).WithPrereq(context.UnlockRecordSkillCaps, "Requires Erudian Teachings to be unlocked."),
            ActionReward.Create(
                context.UnlockRecordRecipes,
                ONE_THOUSAND_GOLD,
                AvatarShopGump.NO_ITEM_ID,
                "Crafter Lineage",
                "Record recipes that you have learned.",
                () =>
                {
                    context.UnlockRecordRecipes = true;
                    from.SendMessage("Your recipes are now permanently unlocked.");
                }
            ),
            ActionReward.Create(
                Constants.IMPROVED_TEMPLATE_MAX_COUNT <= context.ImprovedTemplateCount,
                ONE_HUNDRED_GOLD * (context.ImprovedTemplateCount + 1),
                AvatarShopGump.NO_ITEM_ID,
                $"Blessed Beginnings ({context.ImprovedTemplateCount} of {Constants.IMPROVED_TEMPLATE_MAX_COUNT})",
                "Awaken to your true potential. Ancestral relatives may enhance your template choices.",
                () =>
                {
                    context.ImprovedTemplateCount += 1;
                    from.SendMessage("Your templates may now spawn as (Improved).");
                }
            ),

            // Limits
            ActionReward.Create(
                Constants.RECORDED_SKILL_CAP_MAX_LEVEL <= context.RecordedSkillCapLevel,
                erudianCapCost,
                AvatarShopGump.NO_ITEM_ID,
                $"Erudian Knowledge ({context.RecordedSkillCapLevel} of {Constants.RECORDED_SKILL_CAP_MAX_LEVEL})",
                $"Increases the maximum of skill that your Skill Archive can provide by {Constants.RECORDED_SKILL_CAP_INTERVAL}. Current maximum: {context.GetRecordedSkillCap()}",
                () =>
                {
                    context.RecordedSkillCapLevel += 1;
                    context.ClearRewardCache(Categories.PrimaryBoosts);
                    context.ClearRewardCache(Categories.SecondaryBoosts);
                }
            ).WithPrereq(
                context.UnlockPrimarySkillBoost || context.UnlockSecondarySkillBoost,
                "Requires Jack of No Trades or Artisan's Mastery to be unlocked."
            ),
            ActionReward.Create(
                Constants.SKILL_CAP_MAX_LEVEL <= context.SkillCapLevel,
                skillCapCost,
                AvatarShopGump.NO_ITEM_ID,
                $"Skill Cap ({context.SkillCapLevel} of {Constants.SKILL_CAP_MAX_LEVEL})",
                $"Increases the skill cap by {Constants.SKILL_CAP_PER_LEVEL}. Current bonus: {Constants.SKILL_CAP_PER_LEVEL * context.SkillCapLevel}",
                () => context.SkillCapLevel += 1
            ),
            ActionReward.Create(
                Constants.STAT_CAP_MAX_LEVEL <= context.StatCapLevel,
                statCapCost,
                AvatarShopGump.NO_ITEM_ID,
                $"Stat Cap ({context.StatCapLevel} of {Constants.STAT_CAP_MAX_LEVEL})",
                $"Increases the stat cap by {Constants.STAT_CAP_PER_LEVEL}. Current bonus: {Constants.STAT_CAP_PER_LEVEL * context.StatCapLevel}",
                () => context.StatCapLevel += 1
            ),

            // Rates
            ActionReward.Create(
                Constants.POINT_GAIN_RATE_MAX_LEVEL <= context.PointGainRateLevel,
                pointGainRateCost,
                AvatarShopGump.NO_ITEM_ID,
                $"Coins Gain Rate ({context.PointGainRateLevel} of {Constants.POINT_GAIN_RATE_MAX_LEVEL})",
                $"Increases the coins gain rate by {Constants.POINT_GAIN_RATE_PER_LEVEL}%. Current bonus: {Constants.POINT_GAIN_RATE_PER_LEVEL * context.PointGainRateLevel}%",
                () => context.PointGainRateLevel += 1
            ),
            ActionReward.Create(
                Constants.SKILL_GAIN_RATE_MAX_LEVEL <= context.SkillGainRateLevel,
                skillGainRateCost,
                AvatarShopGump.NO_ITEM_ID,
                $"Skill Gain Rate ({context.SkillGainRateLevel} of {Constants.SKILL_GAIN_RATE_MAX_LEVEL})",
                $"Increases the skill gain rate by {Constants.SKILL_GAIN_RATE_PER_LEVEL}%. Current bonus: {Constants.SKILL_GAIN_RATE_PER_LEVEL * context.SkillGainRateLevel}%",
                () => context.SkillGainRateLevel += 1
            )
        ];
    }
}
