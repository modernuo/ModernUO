using System;

namespace Server.Engines.Avatar;

/// <summary>
/// Stats and skills for the profession-style starter templates.
/// </summary>
public static class AvatarTemplateSkills
{
    public static (SkillName Name, int Value)[] GetSkills(AvatarStarterTemplates template) =>
        template switch
        {
            AvatarStarterTemplates.Mage =>
            [
                (SkillName.Magery, 30), (SkillName.EvalInt, 30), (SkillName.ItemID, 30), (SkillName.Wrestling, 30)
            ],
            AvatarStarterTemplates.Archer =>
            [
                (SkillName.Archery, 30), (SkillName.Tactics, 30), (SkillName.Fletching, 30), (SkillName.Lumberjacking, 30)
            ],
            AvatarStarterTemplates.Warrior =>
            [
                (SkillName.Swords, 30), (SkillName.Tactics, 30), (SkillName.Parry, 30), (SkillName.Healing, 30)
            ],
            AvatarStarterTemplates.Knight =>
            [
                (SkillName.Chivalry, 30), (SkillName.Tactics, 30), (SkillName.Healing, 30), (SkillName.Macing, 30)
            ],
            AvatarStarterTemplates.Ninja =>
            [
                (SkillName.Ninjitsu, 30), (SkillName.Hiding, 30), (SkillName.Stealth, 30), (SkillName.Fencing, 30)
            ],
            AvatarStarterTemplates.Bard =>
            [
                (SkillName.Musicianship, 30), (SkillName.Peacemaking, 30), (SkillName.Discordance, 30),
                (SkillName.Provocation, 30)
            ],
            AvatarStarterTemplates.Druid =>
            [
                (SkillName.AnimalLore, 30), (SkillName.AnimalTaming, 30), (SkillName.Veterinary, 30), (SkillName.Herding, 30)
            ],
            _ => []
        };

    public static (int Str, int Dex, int Int) GetStats(AvatarStarterTemplates template) =>
        template switch
        {
            AvatarStarterTemplates.Mage    => (35, 10, 45),
            AvatarStarterTemplates.Archer  => (35, 40, 15),
            AvatarStarterTemplates.Warrior => (50, 30, 10),
            AvatarStarterTemplates.Knight  => (50, 25, 15),
            AvatarStarterTemplates.Ninja   => (40, 30, 20),
            AvatarStarterTemplates.Bard    => (40, 30, 20),
            AvatarStarterTemplates.Druid   => (30, 20, 40),
            _                              => (10, 10, 10)
        };

    public static void SetTemplateSkills(Mobile m, AvatarStarterTemplates template)
    {
        var (str, dex, intel) = GetStats(template);
        m.InitStats(str, dex, intel);

        foreach (var (name, value) in GetSkills(template))
        {
            if (value > 0 && (name != SkillName.Stealth || template == AvatarStarterTemplates.Ninja) &&
                name != SkillName.RemoveTrap && name != SkillName.Spellweaving)
            {
                m.Skills[name].BaseFixedPoint = value * 10;
            }
        }
    }

    public static void AddSkillBasedItems(Mobile m, ReadOnlySpan<SkillName> skills)
    {
        foreach (var skill in skills)
        {
            CharacterCreation.CharacterCreation.AddStarterSkillItems(m, skill);
        }
    }
}
