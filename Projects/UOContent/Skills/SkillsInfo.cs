using System.IO;
using Server.Json;

namespace Server;

public static class SkillsInfo
{
    public static SkillName RandomSkill()
    {
        var exclusiveMaxIndex = Core.Expansion switch
        {
            >= Expansion.SA => 58, // <= Throwing
            >= Expansion.ML => 55, // <= Spellweaving
            Expansion.SE    => 54, // <= Ninjitsu
            Expansion.AOS   => 52, // <= Chivalry
            _               => 49  // <= RemoveTrap
        };

        return (SkillName)Utility.Random(exclusiveMaxIndex);
    }

    public static readonly SkillName[] CombatSkills =
    [
        SkillName.Parry,
        SkillName.Tactics,
        SkillName.Archery,
        SkillName.Swords,
        SkillName.Macing,
        SkillName.Fencing,
        SkillName.Wrestling,

        // AOS
        SkillName.Focus,

        // SE
        SkillName.Bushido,
        SkillName.Ninjitsu,

        // SA
        SkillName.Throwing
    ];

    public static SkillName RandomCombatSkill()
    {
        var exclusiveMaxIndex = Core.Expansion switch
        {
            >= Expansion.SA => 11,
            >= Expansion.SE => 10,
            Expansion.AOS   => 8,
            _               => 7
        };

        return CombatSkills[Utility.Random(exclusiveMaxIndex)];
    }

    public static readonly SkillName[] CraftSkills =
    [
        SkillName.Alchemy,
        SkillName.Blacksmith,
        SkillName.Fletching,
        SkillName.Carpentry,
        SkillName.Cartography,
        SkillName.Cooking,
        SkillName.Inscribe,
        SkillName.Tailoring,
        SkillName.Tinkering,

        // SA
        SkillName.Imbuing
    ];

    public static SkillName RandomCraftSkill()
    {
        var exclusiveMaxIndex = Core.Expansion switch
        {
            >= Expansion.SA => 10,
            _               => 9
        };

        return CraftSkills[Utility.Random(exclusiveMaxIndex)];
    }

    public static readonly SkillName[] MagicSkills =
    [
        SkillName.EvalInt,
        SkillName.Magery,
        SkillName.MagicResist,
        SkillName.Meditation,

        // AOS
        SkillName.Necromancy,
        SkillName.Chivalry,

        // ML
        SkillName.Spellweaving,

        // SA
        SkillName.Mysticism
    ];

    public static SkillName RandomMagicSkill()
    {
        var exclusiveMaxIndex = Core.Expansion switch
        {
            >= Expansion.SA => 8,
            >= Expansion.ML => 7,
            Expansion.AOS   => 6,
            _               => 4
        };

        return MagicSkills[Utility.Random(exclusiveMaxIndex)];
    }

    public static readonly SkillName[] GatheringSkills =
    {
        SkillName.Fishing,
        SkillName.Mining,
        SkillName.Lumberjacking
    };

    public static SkillName RandomGatheringSkill() => GatheringSkills.RandomElement();

    public static readonly SkillName[] BardSkills =
    {
        SkillName.Peacemaking,
        SkillName.Discordance,
        SkillName.Provocation,
        SkillName.Musicianship,
    };

    public static SkillName RandomBardSkill() => BardSkills.RandomElement();

    public static void Configure()
    {
        SkillInfo.Table = JsonConfig.Deserialize<SkillInfo[]>(Path.Combine(Core.BaseDirectory, "Data/skills.json"));

        if (Core.AOS)
        {
            AOS.DisableStatInfluences();
        }
    }
}
