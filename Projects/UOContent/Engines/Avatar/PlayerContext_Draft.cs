using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Avatar;

public partial class PlayerContext
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PlayerContext));

    public IReadOnlyCollection<SkillName> DraftBannedSkills =>
        _draftBannedSkillSet ?? (IReadOnlyCollection<SkillName>)Array.Empty<SkillName>();

    [CommandProperty(AccessLevel.GameMaster)]
    public int DraftCurrentExperienceRequired
    {
        get
        {
            var requiredExperience = 0;
            for (var i = 1; i < DraftLevel; i++)
            {
                requiredExperience += GetDraftLevelExperience(PrestigeLevel, i);
            }

            return requiredExperience;
        }
    }

    public IReadOnlyCollection<SkillName> DraftedSkills =>
        _draftedSkillSet ?? (IReadOnlyCollection<SkillName>)Array.Empty<SkillName>();

    [CommandProperty(AccessLevel.GameMaster)]
    public int DraftExperienceToNextPick
    {
        get
        {
            if (Constants.DRAFT_MAX_LEVEL <= DraftLevel)
            {
                return 0;
            }

            var requiredExperience = DraftCurrentExperienceRequired;
            var draftLevel = DraftLevel;
            for (var i = 0; i < LevelsToNextPick; i++)
            {
                requiredExperience += GetDraftLevelExperience(PrestigeLevel, draftLevel + i);
            }

            return requiredExperience - DraftTotalExperienceGained;
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int DraftLevel
    {
        get
        {
            var level = 1;
            var currentExperience = DraftTotalExperienceGained;
            do
            {
                currentExperience -= GetDraftLevelExperience(PrestigeLevel, level);
                if (currentExperience < 0)
                {
                    break;
                }

                level++;
            } while (0 < currentExperience);

            return Math.Min(level, Constants.DRAFT_MAX_LEVEL);
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int DraftPicksAvailable => Constants.DRAFT_START_PICK_AMOUNT + DraftLevel / Constants.DRAFT_LEVELS_PER_PICK;

    [CommandProperty(AccessLevel.GameMaster)]
    public int DraftPicksSpent => _draftedSkillSet?.Count ?? 0;

    [CommandProperty(AccessLevel.GameMaster)]
    public int DraftTotalExperienceGained => GrandTotalPoints;

    private int LevelsToNextPick => Constants.DRAFT_LEVELS_PER_PICK - DraftLevel % Constants.DRAFT_LEVELS_PER_PICK;

    public void AddDraftBannedSkill(SkillName skill)
    {
        if (!DraftModeEnabled)
        {
            return;
        }

        _draftBannedSkillSet.Add(skill);
        this.MarkDirty();
    }

    public void AddDraftedSkill(SkillName skill)
    {
        if (!DraftModeEnabled)
        {
            return;
        }

        _draftedSkillSet.Add(skill);
        this.MarkDirty();
    }

    public bool HasPrerequisiteSkills(SkillName skill)
    {
        // If draft mode is not enabled, all skills are available
        if (!DraftModeEnabled)
        {
            return true;
        }

        return skill switch
        {
            // Crafting skills
            SkillName.Blacksmith => IsSkillDrafted(SkillName.Mining),
            SkillName.Fletching  => IsSkillDrafted(SkillName.Lumberjacking),
            SkillName.Carpentry  => IsSkillDrafted(SkillName.Lumberjacking),
            SkillName.Tinkering  => IsSkillDrafted(SkillName.Mining),
            SkillName.Alchemy    => IsSkillDrafted(SkillName.Cooking),
            SkillName.Stealth    => IsSkillDrafted(SkillName.Hiding),
            _                    => true
        };
    }

    public bool IsSkillDrafted(SkillName skill)
    {
        // If draft mode is not enabled, all skills are available
        if (!DraftModeEnabled)
        {
            return true;
        }

        return _draftedSkillSet?.Contains(skill) == true;
    }

    public bool IsSmartSkill(SkillName skillName)
    {
        if (IsSkillDrafted(skillName) || !HasPrerequisiteSkills(skillName))
        {
            return false;
        }

        switch (skillName)
        {
            case SkillName.Alchemy:
            case SkillName.Blacksmith:
            case SkillName.Fletching:
            case SkillName.Carpentry:
            case SkillName.Cooking:
            case SkillName.Inscribe:
            case SkillName.Tailoring:
            case SkillName.Tinkering:
                {
                    // Limit - Already has a Crafting skill
                    if (IsAnySkillDraftedExcept(
                            skillName,
                            SkillName.Alchemy,
                            SkillName.Blacksmith,
                            SkillName.Fletching,
                            SkillName.Carpentry,
                            SkillName.Cooking,
                            SkillName.Inscribe,
                            SkillName.Tailoring,
                            SkillName.Tinkering
                        ))
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.Forensics:
            case SkillName.Lumberjacking:
            case SkillName.Mining:
                {
                    // Limit - Already has a Gathering skill
                    if (IsAnySkillDraftedExcept(
                            skillName,
                            SkillName.Forensics,
                            SkillName.Lumberjacking,
                            SkillName.Mining
                        ))
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.Anatomy:
            case SkillName.Healing:
                {
                    if ((skillName == SkillName.Healing &&
                         !HasAllSkillsButThis(skillName, SkillName.Healing, SkillName.SpiritSpeak)) &&
                        !IsAnySkillDraftedExcept( // Combat skills
                            skillName,
                            SkillName.Anatomy,
                            SkillName.Healing,
                            SkillName.Chivalry,
                            SkillName.Macing,
                            SkillName.Fencing,
                            SkillName.Wrestling,
                            SkillName.Archery,
                            SkillName.Swords
                        ))
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.AnimalLore:
            case SkillName.Herding:
            case SkillName.Veterinary:
            case SkillName.Camping:
                {
                    // Make sure we have a Tamer skill
                    if (!IsAnySkillDraftedExcept(
                            skillName,
                            SkillName.AnimalLore,
                            SkillName.Herding,
                            SkillName.AnimalTaming,
                            SkillName.Veterinary
                        ))
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.ArmsLore:
            case SkillName.ItemID:
            case SkillName.TasteID:
                {
                    // Limit - Already has an ID skill
                    if (IsAnySkillDraftedExcept(
                            skillName,
                            SkillName.ArmsLore,
                            SkillName.ItemID,
                            SkillName.TasteID
                        ))
                    {
                        return false;
                    }

                    // Make sure we have a relevant Crafting skill
                    if (skillName == SkillName.ArmsLore && !IsAnySkillDrafted(
                            SkillName.Blacksmith,
                            SkillName.Fletching,
                            SkillName.Carpentry,
                            SkillName.Tailoring,
                            SkillName.Tinkering
                        ))
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.Begging:
                {
                    // Make sure we have a relevant Jester skill
                    if (!HasAllSkillsButThis(skillName, SkillName.Begging, SkillName.EvalInt))
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.Bushido:
            case SkillName.Chivalry:
            case SkillName.Ninjitsu:
            case SkillName.Tactics:
                {
                    // Make sure we have a Weapon skill
                    if (!IsAnySkillDrafted(
                            SkillName.Macing,
                            SkillName.Fencing,
                            SkillName.Wrestling,
                            SkillName.Archery,
                            SkillName.Swords
                        ))
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.Discordance:
            case SkillName.Peacemaking:
            case SkillName.Provocation:
                {
                    // Make sure we have a Bard skill
                    if (!IsAnySkillDraftedExcept(
                            skillName,
                            SkillName.Discordance,
                            SkillName.Musicianship,
                            SkillName.Peacemaking,
                            SkillName.Provocation
                        ))
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.Spellweaving:
            case SkillName.Magery:
            case SkillName.Necromancy:
                {
                    // Limit - Already have a Casting skill available
                    if (IsAnySkillDraftedExcept(
                            skillName,
                            SkillName.Spellweaving,
                            SkillName.Magery,
                            SkillName.Necromancy
                        ))
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.Focus:
            case SkillName.Meditation:
                {
                    if (!HasAllSkillsButThis(skillName, SkillName.Focus, SkillName.Wrestling, SkillName.Meditation) && // Monk
                        !IsAnySkillDraftedExcept( // Mana skills
                            skillName,
                            SkillName.Bushido,
                            SkillName.Spellweaving,
                            SkillName.Chivalry,
                            SkillName.Magery,
                            SkillName.Necromancy,
                            SkillName.Ninjitsu
                        ))
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.Hiding:
            case SkillName.Lockpicking:
            case SkillName.RemoveTrap:
            case SkillName.DetectHidden:
            case SkillName.Snooping:
            case SkillName.Stealing:
            case SkillName.Stealth:
                {
                    // Make sure we have a Thief skill
                    if (!IsAnySkillDraftedExcept(
                            skillName,
                            SkillName.Hiding,
                            SkillName.Ninjitsu,
                            SkillName.Lockpicking,
                            SkillName.RemoveTrap,
                            SkillName.DetectHidden,
                            SkillName.Snooping,
                            SkillName.Stealing,
                            SkillName.Stealth
                        ))
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.Macing:
            case SkillName.Fencing:
            case SkillName.Wrestling:
            case SkillName.Archery:
            case SkillName.Swords:
                {
                    // Limit - Already has a Combat skill
                    if (IsAnySkillDraftedExcept(
                            skillName,
                            SkillName.Macing,
                            SkillName.Fencing,
                            SkillName.Wrestling,
                            SkillName.Archery,
                            SkillName.Swords
                        ))
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.Parry:
                {
                    // Make sure we have a (melee) Combat skill
                    if (!IsAnySkillDraftedExcept(
                            skillName,
                            SkillName.Macing,
                            SkillName.Fencing,
                            SkillName.Wrestling,
                            SkillName.Swords
                        ))
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.EvalInt:
                {
                    if (!HasAllSkillsButThis(skillName, SkillName.Begging, SkillName.EvalInt) && // Jester
                        !IsSkillDrafted(SkillName.Magery) &&                                      // Mage
                        !IsSkillDrafted(SkillName.Swords))                                        // Jedi/Syth
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.SpiritSpeak:
                {
                    if (!HasAllSkillsButThis(skillName, SkillName.Healing, SkillName.SpiritSpeak)) // Holy Man
                    {
                        return false;
                    }

                    break;
                }

            case SkillName.Cartography:
            case SkillName.MagicResist:
            case SkillName.Poisoning:
            case SkillName.Fishing:
            case SkillName.Tracking:
                {
                    // No smart roll option
                    return false;
                }

            case SkillName.Musicianship:
            case SkillName.AnimalTaming:
                {
                    // Always possible
                    break;
                }

            default:
                {
                    logger.Warning("Unexpected skill during draft mode smart roll: {Skill}", skillName);
                    return false;
                }
        }

        return true;
    }

    public void RemoveDraftBannedSkill(SkillName skill)
    {
        if (!DraftModeEnabled)
        {
            return;
        }

        _draftBannedSkillSet.Remove(skill);
        this.MarkDirty();
    }

    public void SetDraftModeEnabled(PlayerMobile player, bool enabled)
    {
        if (enabled)
        {
            DraftedSkillSet = [];

            // Maintain bans
            DraftBannedSkillSet ??= [];

            ClearRewardCache(Categories.Draft);
            player.SendMessage("Draft mode enabled. All skills have been reset to 0 and Locked.");
        }
        else
        {
            DraftedSkillSet = null;
            DraftBannedSkillSet = null;
            player.SendMessage("Draft mode disabled. All skills have been reset to 0 and Unlocked.");
        }

        // Zero out all skills; undrafted skills cannot gain (see AvatarEngine.CanGainSkill)
        for (var i = 0; i < player.Skills.Length; i++)
        {
            var skill = player.Skills[i];
            skill.SetLockNoRelay(enabled ? SkillLock.Locked : SkillLock.Up);
            skill.Base = 0;
        }

        player.NetState.SendSkillsUpdate(player.Skills);

        DraftModeEnabled = enabled;
    }

    private static int GetDraftLevelExperience(int prestigeLevel, int currentLevel)
    {
        const double LEVEL_GROWTH = 0.08;
        const double PRESTIGE_GROWTH = 0.25;
        const int BASE_REQUIREMENT = 500;

        var required = BASE_REQUIREMENT;
        if (0 < prestigeLevel)
        {
            required += (int)(1 + prestigeLevel * PRESTIGE_GROWTH);
        }

        var levelMultiplier = 1 + LEVEL_GROWTH * (currentLevel - 1);

        return (int)(required * levelMultiplier);
    }

    private bool HasAllSkillsButThis(SkillName excludedSkill, params ReadOnlySpan<SkillName> skills)
    {
        foreach (var skill in skills)
        {
            if (skill != excludedSkill && !IsSkillDrafted(skill))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsAnySkillDrafted(params ReadOnlySpan<SkillName> skills)
    {
        foreach (var skill in skills)
        {
            if (IsSkillDrafted(skill))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAnySkillDraftedExcept(SkillName excludedSkill, params ReadOnlySpan<SkillName> skills)
    {
        foreach (var skill in skills)
        {
            if (skill != excludedSkill && IsSkillDrafted(skill))
            {
                return true;
            }
        }

        return false;
    }
}
