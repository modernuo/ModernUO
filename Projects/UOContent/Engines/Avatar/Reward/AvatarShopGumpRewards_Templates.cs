using System;
using System.Collections.Generic;
using System.Linq;
using Server.Mobiles;

namespace Server.Engines.Avatar;

public static partial class RewardFactory
{
    private static readonly AvatarStarterTemplates[] _professionTemplates =
    [
        AvatarStarterTemplates.Ninja,
        AvatarStarterTemplates.Bard,
        AvatarStarterTemplates.Druid,
        AvatarStarterTemplates.Knight,
        AvatarStarterTemplates.Warrior,
        AvatarStarterTemplates.Mage,
        AvatarStarterTemplates.Archer
    ];

    public static List<IReward> CreateTemplateRewards(PlayerMobile from, PlayerContext context)
    {
        void ApplyTemplate(Func<PlayerMobile, bool> action)
        {
            if (from.NetState == null)
            {
                return;
            }

            AvatarConfirm.Send(
                from,
                "Select Template?",
                $"Are you sure you wish to select this template? This is a <BASEFONT COLOR={AvatarConfirm.Red}>destructive action</BASEFONT> that will recreate your backpack, reduce existing stats, and change skills.",
                () =>
                {
                    AvatarEngine.DisableSkillGains = true;

                    try
                    {
                        // Reduce all skills to 0
                        for (var i = 0; i < from.Skills.Length; i++)
                        {
                            var skill = from.Skills[i];
                            if (skill.Base > 0)
                            {
                                skill.Base = 0;
                            }
                        }

                        var boosted = action(from);

                        // Boost skills if necessary
                        if (boosted)
                        {
                            for (var i = 0; i < from.Skills.Length; i++)
                            {
                                var skill = from.Skills[i];
                                if (skill.Value > 0)
                                {
                                    skill.BaseFixedPoint += 100; // +10 to each skill that was set
                                }
                            }
                        }
                    }
                    finally
                    {
                        AvatarEngine.DisableSkillGains = false;
                    }

                    AvatarEngine.ApplyContext(from, from.Avatar);
                    context.ApplyTemplate(from, context.SelectedTemplate);
                    from.SendMessage("Your skills have been set to the chosen template.");
                }
            );
        }

        ActionReward StatTemplate(string name, int str, int dex, int intel, AvatarStarterTemplates template) =>
            ActionReward.Create(
                AvatarShopGump.COST_FREE,
                AvatarShopGump.NO_ITEM_ID,
                name,
                $"Starts with {str} strength, {dex} dexterity, and {intel} intelligence.",
                () => ApplyTemplate(
                    player =>
                    {
                        player.InitStats(str, dex, intel);
                        context.SelectedTemplate = template;
                        return false;
                    }
                )
            ).AsStatic(context.DraftModeEnabled);

        var rewards = new List<IReward>
        {
            StatTemplate("The Brute", 60, 10, 10, AvatarStarterTemplates.Brute),
            StatTemplate("The Acrobat", 10, 60, 10, AvatarStarterTemplates.Acrobat),
            StatTemplate("The Scholar", 10, 10, 60, AvatarStarterTemplates.Scholar),
            StatTemplate("The Well-Rounded", 40, 20, 20, AvatarStarterTemplates.WellRoundedStats)
        };

        if (context.DraftModeEnabled)
        {
            return rewards;
        }

        var boostedTemplates = context.BoostedTemplateCache ??= [];

        if (context.ImprovedTemplateCount > 0 && context.ImprovedTemplateCount <= _professionTemplates.Length)
        {
            // Keep boosting a random profession until we reach our max
            while (boostedTemplates.Count != context.ImprovedTemplateCount)
            {
                boostedTemplates.Add(_professionTemplates.RandomElement());
            }
        }

        foreach (var template in _professionTemplates.OrderBy(p => p.ToString()))
        {
            var boosted = boostedTemplates.Contains(template);
            rewards.Add(
                ActionReward.Create(
                    AvatarShopGump.COST_FREE,
                    AvatarShopGump.NO_ITEM_ID,
                    boosted ? $"The {template} (Improved)" : $"The {template}",
                    $"Start with the stats, skills, and items of a {template}.",
                    () => ApplyTemplate(
                        player =>
                        {
                            AvatarTemplateSkills.SetTemplateSkills(player, template);
                            context.SelectedTemplate = template;
                            return boosted;
                        }
                    )
                )
            );
        }

        return rewards;
    }
}
