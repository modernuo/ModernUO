namespace Server.Engines.Avatar;

public static class SkillExtensions
{
    /// <summary>
    /// Crafting and gathering skills, which the shop lists under "Secondary Skills".
    /// </summary>
    public static bool IsSecondarySkill(this Skill skill) => IsSecondarySkill(skill.SkillName);

    public static bool IsSecondarySkill(this SkillName skillName) =>
        skillName switch
        {
            SkillName.Alchemy or SkillName.Blacksmith or SkillName.Fletching or SkillName.Carpentry or
                SkillName.Cooking or SkillName.Inscribe or SkillName.Tailoring or SkillName.Tinkering => true,
            SkillName.Forensics or SkillName.Lumberjacking or SkillName.Mining => true,
            _ => false
        };

    public static bool IsExcludedSkill(this SkillName skillName) =>
        skillName is SkillName.Mysticism or SkillName.Imbuing or SkillName.Throwing;

    /// <summary>
    /// Raises a skill toward <paramref name="targetFixedPoint"/>, pulling points from skills
    /// locked Down when the total cap would otherwise be exceeded.
    /// </summary>
    public static void RaiseTo(this Skill skill, Mobile from, int targetFixedPoint)
    {
        var amountToIncrease = targetFixedPoint - skill.BaseFixedPoint;
        if (amountToIncrease <= 0)
        {
            return;
        }

        var amountAvailable = System.Math.Max(0, from.Skills.Cap - from.Skills.Total);

        if (amountAvailable < amountToIncrease)
        {
            var amountRequired = amountToIncrease - amountAvailable;
            for (var i = 0; i < from.Skills.Length; ++i)
            {
                var other = from.Skills[i];
                if (other.Lock != SkillLock.Down)
                {
                    continue;
                }

                if (amountRequired >= other.BaseFixedPoint)
                {
                    amountRequired -= other.BaseFixedPoint;
                    other.Base = 0.0;
                }
                else
                {
                    other.BaseFixedPoint -= amountRequired;
                    amountRequired = 0;
                    break;
                }
            }

            // Didn't get enough free points, so we'll just take what we can get
            if (amountRequired > 0)
            {
                amountToIncrease -= amountRequired;
            }
        }

        skill.BaseFixedPoint += amountToIncrease;
    }
}
