using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Server.Zulu_Hotel
{
    [PropertyObject]
    public class ZuluClass
    {
        //reference original ZH Canada (ZH3) release
        private const double ClassPointsPerLevel = 120;
        private const double SkillBase = 480;
        private const double PercentPerLevel = 0.08;
        private const double PercentBase = 0.52;
        private const double PerLevel = 0.15; //15% per level
        private const double ClasseBonus = 1.5;
        public const int MaxLevel = 6;

        private readonly IZuluClassed m_Parent;

        public static readonly double[] MinSkills =
    Enumerable
        .Range(0, MaxLevel + 1) // Technically lvl 0 (none) is a level
        .Select(i => SkillBase + ClassPointsPerLevel * i)
        .ToArray();

        #region ClassSkills
        public static readonly IReadOnlyDictionary<ZuluClassType, SkillName[]> ClassSkills =
            new Dictionary<ZuluClassType, SkillName[]>
            {
                [ZuluClassType.Warrior] = new[]
                {
                    SkillName.Wrestling,
                    SkillName.Tactics,
                    SkillName.Healing,
                    SkillName.Anatomy,
                    SkillName.Swords,
                    SkillName.Macing,
                    SkillName.Fencing,
                    SkillName.Parry,
                },
                [ZuluClassType.Ranger] = new[]
                {
                    SkillName.Tracking,
                    SkillName.Archery,
                    SkillName.AnimalLore,
                    SkillName.Veterinary,
                    SkillName.AnimalTaming,
                    SkillName.Fishing,
                    SkillName.Camping,
                    SkillName.Cooking,
                },
                [ZuluClassType.Mage] = new[]
                {
                    SkillName.Alchemy,
                    SkillName.ItemID,
                    SkillName.EvalInt,
                    SkillName.Inscribe,
                    SkillName.MagicResist,
                    SkillName.Meditation,
                    SkillName.Magery,
                    SkillName.SpiritSpeak,
                },
                [ZuluClassType.Crafter] = new[]
                {
                    SkillName.Tinkering,
                    SkillName.ArmsLore,
                    SkillName.Fletching,
                    SkillName.Tailoring,
                    SkillName.Mining,
                    SkillName.Lumberjacking,
                    SkillName.Carpentry,
                    SkillName.Blacksmith,
                },
                [ZuluClassType.Thief] = new[]
                {
                    SkillName.Hiding,
                    SkillName.Stealth,
                    SkillName.Stealing,
                    SkillName.DetectHidden,
                    SkillName.RemoveTrap,
                    SkillName.Poisoning,
                    SkillName.Lockpicking,
                    SkillName.Snooping,
                },
                [ZuluClassType.Bard] = new[]
                {
                    SkillName.Provocation,
                    SkillName.Musicianship,
                    SkillName.Herding,
                    SkillName.Discordance,
                    SkillName.TasteID,
                    SkillName.Peacemaking,
                    SkillName.Cartography,
                    SkillName.Begging,
                }
            };
        #endregion


        [CommandProperty(AccessLevel.Counselor)]
        public int Level { get; set; } = 0;

        [CommandProperty(AccessLevel.Counselor)]
        public ZuluClassType Type { get; set; } = ZuluClassType.None;

        public ZuluClass(IZuluClassed parent)
        {
            m_Parent = parent;

            ComputeClass();
        }
        public double ClassBonus
        {
            get
            {
                return ClassBonus;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public double BonusLevel => m_Parent is BaseCreature || Type is ZuluClassType.PowerPlayer or ZuluClassType.None
            ? 1.0
            : 1.0 + Level * PerLevel;

        public static double GetBonusByLevel(int level) => 1.0 + level * PerLevel;

        public static void Initialize()
        {
            CommandSystem.Register("SetClasse", AccessLevel.GameMaster, SetClass);
        }

        [Usage("SetClasse <class> <level>")]
        [Description("Sets you to the desired class and level, sets all other skills to 0.")]
        public static void SetClass(CommandEventArgs e)
        {
            if (e.Mobile is not PlayerMobile pm)
                return;

            if (e.Length == 2 && Enum.TryParse(e.GetString(0), out ZuluClassType classType))
            {
                var level = e.GetInt32(1);
                SetClass(pm, classType, level);
            }
        }

        public static void SetClass(Mobile m, ZuluClassType classType, int level)
        {
            if (m is not PlayerMobile pm)
                return;

            if (level is > MaxLevel or < 0)
                level = 0;

            foreach (var skill in pm.Skills)
            {
                var skillLevel = ClassSkills[classType].Contains(skill.SkillName)
                    ? MinSkills[level] / ClassSkills[classType].Length
                    : 0.0;
                skill.Base = Math.Min(skillLevel, 130);
            }

            m.Dex = 130;
            m.Str = 130;
            m.Int = 130;
        }


        public void ComputeClass()
        {
            var allSkillsTotal = 0.0;
            foreach (var skill in m_Parent.Skills)
            {
                allSkillsTotal += skill.Value;
            }

            Type = ZuluClassType.None;
            Level = 0;

            double total = m_Parent.Skills.Total;
            total *= 0.1;

            switch (total)
            {
                case < 600.0:
                    Level = 0;
                    Type = ZuluClassType.None;
                    return;
                case >= 3920.0:
                    {
                        Type = ZuluClassType.PowerPlayer;
                        Level = 1;

                        if (total >= 5145)
                        {
                            Level = 2;

                            if (total >= 6370)
                            {
                                Level = 3;
                            }
                        }

                        //we're a pp so:
                        return;
                    }
            }

            foreach (var (classType, classSkills) in ClassSkills)
            {
                var classTotal = classSkills.Select(s => m_Parent.Skills[s].Value).Sum();

                var level = GetClassLevel(classTotal, allSkillsTotal);

                if (level > 0)
                {
                    Type = classType;
                    Level = level;
                }
            }

            if (Level > MaxLevel)
                Level = MaxLevel;

            if (Level <= 0)
            {
                Level = 0;
                Type = ZuluClassType.None;
            }
        }

        public static double GetClassLevelPercent(int level)
        {
            return PercentBase + PercentPerLevel * level;
        }

        //idx:    0    1     2     3     4     5      6
        //Min: [ 480, 600,  720,  840,  960,  1080, 1200 ]
        //Max: [ 923, 1000, 1058, 1105, 1142, 1173, 1200 ]
        private int GetClassLevel(double classTotal, double allSkillsTotal)
        {
            for (int level = MinSkills.Length - 1; level >= 0; level--)
            {
                var levelReq = GetClassLevelPercent(level);
                var classPct = classTotal / allSkillsTotal;

                if (classTotal >= MinSkills[level] && classPct >= levelReq)
                    return level;
            }

            return 0;
        }

        public bool IsSkillInClass(SkillName sn)
        {
            return ClassSkills.FirstOrDefault(kv => kv.Value.Contains(sn)).Key == Type;
        }

    }

    public interface IZuluClassed
    {
        public ZuluClass ZuluClass { get; }

        public Skills Skills { get; }
    }

    #region Class Bonus Hooks


    #endregion region

}
