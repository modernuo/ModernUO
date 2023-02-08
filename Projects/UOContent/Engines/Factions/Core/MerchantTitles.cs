namespace Server.Factions
{
    public enum MerchantTitle
    {
        None,
        Scribe,
        Carpenter,
        Blacksmith,
        Bowyer,
        Tailor
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
                1060773, // Scribe
                1011468, // SCRIBE
                1010121 // You now have the faction title of scribe
            ),
            new(
                SkillName.Carpentry,
                90.0,
                1060774, // Carpenter
                1011469, // CARPENTER
                1010122 // You now have the faction title of carpenter
            ),
            new(
                SkillName.Tinkering,
                90.0,
                1022984, // Tinker
                1011470, // TINKER
                1010123 // You now have the faction title of tinker
            ),
            new(
                SkillName.Blacksmith,
                90.0,
                1023016, // Blacksmith
                1011471, // BLACKSMITH
                1010124 // You now have the faction title of blacksmith
            ),
            new(
                SkillName.Fletching,
                90.0,
                1023022, // Bowyer
                1011472, // BOWYER
                1010125 // You now have the faction title of Bowyer
            ),
            new(
                SkillName.Tailoring,
                90.0,
                1022982, // Tailor
                1018300, // TAILOR
                1042162 // You now have the faction title of Tailor
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
