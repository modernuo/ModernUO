namespace Server.Factions
{
    public enum MerchantTitle
    {
        None,
        Scribe,
        Carpenter,
        Blacksmith,
        Bowyer,
        Tialor
    }

    public class MerchantTitleInfo
    {
        public MerchantTitleInfo(
            SkillName skill, double requirement, TextDefinition title, TextDefinition label,
            TextDefinition assigned
        )
        {
            Skill = skill;
            Requirement = requirement;
            Title = title;
            Label = label;
            Assigned = assigned;
        }

        public SkillName Skill { get; }

        public double Requirement { get; }

        public TextDefinition Title { get; }

        public TextDefinition Label { get; }

        public TextDefinition Assigned { get; }
    }

    public static class MerchantTitles
    {
        public static MerchantTitleInfo[] Info { get; } =
        {
            new(
                SkillName.Inscribe,
                90.0,
                new TextDefinition(1060773, "Scribe"),
                new TextDefinition(1011468, "SCRIBE"),
                new TextDefinition(1010121, "You now have the faction title of scribe")
            ),
            new(
                SkillName.Carpentry,
                90.0,
                new TextDefinition(1060774, "Carpenter"),
                new TextDefinition(1011469, "CARPENTER"),
                new TextDefinition(1010122, "You now have the faction title of carpenter")
            ),
            new(
                SkillName.Tinkering,
                90.0,
                new TextDefinition(1022984, "Tinker"),
                new TextDefinition(1011470, "TINKER"),
                new TextDefinition(1010123, "You now have the faction title of tinker")
            ),
            new(
                SkillName.Blacksmith,
                90.0,
                new TextDefinition(1023016, "Blacksmith"),
                new TextDefinition(1011471, "BLACKSMITH"),
                new TextDefinition(1010124, "You now have the faction title of blacksmith")
            ),
            new(
                SkillName.Fletching,
                90.0,
                new TextDefinition(1023022, "Bowyer"),
                new TextDefinition(1011472, "BOWYER"),
                new TextDefinition(1010125, "You now have the faction title of Bowyer")
            ),
            new(
                SkillName.Tailoring,
                90.0,
                new TextDefinition(1022982, "Tailor"),
                new TextDefinition(1018300, "TAILOR"),
                new TextDefinition(1042162, "You now have the faction title of Tailor")
            )
        };

        public static MerchantTitleInfo GetInfo(MerchantTitle title)
        {
            var idx = (int)title - 1;

            if (idx >= 0 && idx < Info.Length)
            {
                return Info[idx];
            }

            return null;
        }

        public static bool HasMerchantQualifications(Mobile mob)
        {
            for (var i = 0; i < Info.Length; ++i)
            {
                if (IsQualified(mob, Info[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsQualified(Mobile mob, MerchantTitle title) => IsQualified(mob, GetInfo(title));

        public static bool IsQualified(Mobile mob, MerchantTitleInfo info)
        {
            if (mob == null || info == null)
            {
                return false;
            }

            return mob.Skills[info.Skill].Value >= info.Requirement;
        }
    }
}
