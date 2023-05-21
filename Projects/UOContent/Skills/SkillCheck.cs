using System;
using Server.Factions;
using Server.Mobiles;

namespace Server.Misc;

// TODO: Make this entirely configurable
public static class SkillCheck
{
    public enum Stat
    {
        Str,
        Dex,
        Int
    }

    // Publish 16 changed max stats from 100 to 125
    private static int StatMax = Core.LBR ? 125 : 100;

    private static readonly TimeSpan m_StatGainDelay = TimeSpan.FromMinutes(Core.ML ? 0.05 : 15);
    private static readonly TimeSpan m_PetStatGainDelay = TimeSpan.FromMinutes(5.0);

    public static void Initialize()
    {
        Mobile.SkillCheckLocationHandler = Mobile_SkillCheckLocation;
        Mobile.SkillCheckDirectLocationHandler = Mobile_SkillCheckDirectLocation;

        Mobile.SkillCheckTargetHandler = Mobile_SkillCheckTarget;
        Mobile.SkillCheckDirectTargetHandler = Mobile_SkillCheckDirectTarget;
    }

    public static bool Mobile_SkillCheckLocation(Mobile from, SkillName skillName, double minSkill, double maxSkill)
    {
        var skill = from.Skills[skillName];

        if (skill == null)
        {
            return false;
        }

        var value = skill.Value;

        if (value < minSkill)
        {
            return false; // Too difficult
        }

        if (value >= maxSkill || minSkill >= maxSkill)
        {
            return true; // No challenge
        }

        var chance = (value - minSkill) / (maxSkill - minSkill);

        var size = AntiMacroSystem.Settings.LocationSize;
        var loc = new Point2D(from.Location.X / size, from.Location.Y / size);
        return CheckSkill(from, skill, loc, chance);
    }

    public static bool Mobile_SkillCheckDirectLocation(Mobile from, SkillName skillName, double chance)
    {
        var skill = from.Skills[skillName];

        if (skill == null)
        {
            return false;
        }

        if (chance < 0.0)
        {
            return false; // Too difficult
        }

        if (chance >= 1.0)
        {
            return true; // No challenge
        }

        var size = AntiMacroSystem.Settings.LocationSize;
        var loc = new Point2D(from.Location.X / size, from.Location.Y / size);
        return CheckSkill(from, skill, loc, chance);
    }

    public static bool CheckSkill(Mobile from, Skill skill, object amObj, double chance)
    {
        if (from.Skills.Cap == 0)
        {
            return false;
        }

        var success = chance >= Utility.RandomDouble();

        var region = from.Region;
        if (from.Alive && region.AllowGain(from, skill, amObj))
        {
            if (skill.Base < 10.0) // Gain regardless of the AllowGain check
            {
                Gain(from, skill);
            }
            else if (AllowGain(from, skill, amObj))
            {
                var gc = (double)(from.Skills.Cap - from.Skills.Total) / from.Skills.Cap;
                gc += (skill.Cap - skill.Base) / skill.Cap;
                gc /= 2;

                gc += (1.0 - chance) * (success ? 0.5 : Core.AOS ? 0.0 : 0.2);
                gc /= 2;

                gc *= skill.Info.GainFactor;

                if (gc < 0.01)
                {
                    gc = 0.01;
                }

                if (from is BaseCreature { Controlled: true })
                {
                    gc *= 2;
                }

                if (gc >= Utility.RandomDouble())
                {
                    Gain(from, skill);
                }
            }
        }

        return success;
    }

    public static bool Mobile_SkillCheckTarget(
        Mobile from, SkillName skillName, object target, double minSkill,
        double maxSkill
    )
    {
        var skill = from.Skills[skillName];

        if (skill == null)
        {
            return false;
        }

        var value = skill.Value;

        if (value < minSkill)
        {
            return false; // Too difficult
        }

        if (value >= maxSkill || minSkill >= maxSkill)
        {
            return true; // No challenge
        }

        var chance = (value - minSkill) / (maxSkill - minSkill);

        return CheckSkill(from, skill, target, chance);
    }

    public static bool Mobile_SkillCheckDirectTarget(Mobile from, SkillName skillName, object target, double chance)
    {
        var skill = from.Skills[skillName];

        if (skill == null)
        {
            return false;
        }

        if (chance < 0.0)
        {
            return false; // Too difficult
        }

        if (chance >= 1.0)
        {
            return true; // No challenge
        }

        return CheckSkill(from, skill, target, chance);
    }

    private static bool AllowGain(Mobile from, Skill skill, object obj)
    {
        if (Core.AOS && Faction.InSkillLoss(from)) // Changed some time between the introduction of AoS and SE.
        {
            return false;
        }

        return from is not PlayerMobile mobile || AntiMacroSystem.AntiMacroCheck(mobile, skill, obj);
    }

    public static void Gain(Mobile from, Skill skill)
    {
        if (from is BaseCreature { IsDeadPet: true })
        {
            return;
        }

        if (skill.SkillName == SkillName.Focus && from is BaseCreature)
        {
            return;
        }

        if (skill.Base < skill.Cap && skill.Lock == SkillLock.Up)
        {
            var toGain = 1;

            if (skill.Base <= 10.0)
            {
                toGain = Utility.Random(4) + 1;
            }

            var skills = from.Skills;

            if (from.Player && skills.Total / (double)skills.Cap >= Utility.RandomDouble())
            {
                for (var i = 0; i < skills.Length; ++i)
                {
                    var toLower = skills[i];

                    if (toLower != skill && toLower.Lock == SkillLock.Down && toLower.BaseFixedPoint >= toGain)
                    {
                        toLower.BaseFixedPoint -= toGain;
                        break;
                    }
                }
            }

            if (from is PlayerMobile pm && skill.SkillName == pm.AcceleratedSkill &&
                pm.AcceleratedStart > Core.Now)
            {
                toGain *= Utility.RandomMinMax(2, 5);
            }

            if (!from.Player || skills.Total + toGain <= skills.Cap)
            {
                skill.BaseFixedPoint += toGain;
            }
        }

        if (skill.Lock == SkillLock.Up)
        {
            var info = skill.Info;

            if (from.StrLock == StatLockType.Up && info.StrGain / 33.3 > Utility.RandomDouble())
            {
                GainStat(from, Stat.Str);
            }
            else if (from.DexLock == StatLockType.Up && info.DexGain / 33.3 > Utility.RandomDouble())
            {
                GainStat(from, Stat.Dex);
            }
            else if (from.IntLock == StatLockType.Up && info.IntGain / 33.3 > Utility.RandomDouble())
            {
                GainStat(from, Stat.Int);
            }
        }
    }

    public static bool CanLower(Mobile from, Stat stat)
    {
        return stat switch
        {
            Stat.Str => from.StrLock == StatLockType.Down && from.RawStr > 10,
            Stat.Dex => from.DexLock == StatLockType.Down && from.RawDex > 10,
            Stat.Int => from.IntLock == StatLockType.Down && from.RawInt > 10,
            _        => false
        };
    }

    public static bool CanRaise(Mobile from, Stat stat)
    {
        if (!(from is BaseCreature creature && creature.Controlled))
        {
            if (from.RawStatTotal >= from.StatCap)
            {
                return false;
            }
        }

        return stat switch
        {
            Stat.Str => from.StrLock == StatLockType.Up && from.RawStr < StatMax,
            Stat.Dex => from.DexLock == StatLockType.Up && from.RawDex < StatMax,
            Stat.Int => from.IntLock == StatLockType.Up && from.RawInt < StatMax,
            _        => false
        };
    }

    public static void IncreaseStat(Mobile from, Stat stat, bool atrophy)
    {
        atrophy = atrophy || from.RawStatTotal >= from.StatCap;

        switch (stat)
        {
            case Stat.Str:
                {
                    if (atrophy)
                    {
                        if (CanLower(from, Stat.Dex) && (from.RawDex < from.RawInt || !CanLower(from, Stat.Int)))
                        {
                            --from.RawDex;
                        }
                        else if (CanLower(from, Stat.Int))
                        {
                            --from.RawInt;
                        }
                    }

                    if (CanRaise(from, Stat.Str))
                    {
                        ++from.RawStr;
                    }

                    break;
                }
            case Stat.Dex:
                {
                    if (atrophy)
                    {
                        if (CanLower(from, Stat.Str) && (from.RawStr < from.RawInt || !CanLower(from, Stat.Int)))
                        {
                            --from.RawStr;
                        }
                        else if (CanLower(from, Stat.Int))
                        {
                            --from.RawInt;
                        }
                    }

                    if (CanRaise(from, Stat.Dex))
                    {
                        ++from.RawDex;
                    }

                    break;
                }
            case Stat.Int:
                {
                    if (atrophy)
                    {
                        if (CanLower(from, Stat.Str) && (from.RawStr < from.RawDex || !CanLower(from, Stat.Dex)))
                        {
                            --from.RawStr;
                        }
                        else if (CanLower(from, Stat.Dex))
                        {
                            --from.RawDex;
                        }
                    }

                    if (CanRaise(from, Stat.Int))
                    {
                        ++from.RawInt;
                    }

                    break;
                }
        }
    }

    public static void GainStat(Mobile from, Stat stat)
    {
        switch (stat)
        {
            case Stat.Str:
                {
                    if (from is BaseCreature creature && creature.Controlled)
                    {
                        if (creature.LastStrGain + m_PetStatGainDelay >= Core.Now)
                        {
                            return;
                        }
                    }
                    else if (from.LastStrGain + m_StatGainDelay >= Core.Now)
                    {
                        return;
                    }

                    from.LastStrGain = Core.Now;
                    break;
                }
            case Stat.Dex:
                {
                    if (from is BaseCreature creature && creature.Controlled)
                    {
                        if (creature.LastDexGain + m_PetStatGainDelay >= Core.Now)
                        {
                            return;
                        }
                    }
                    else if (from.LastDexGain + m_StatGainDelay >= Core.Now)
                    {
                        return;
                    }

                    from.LastDexGain = Core.Now;
                    break;
                }
            case Stat.Int:
                {
                    if (from is BaseCreature creature && creature.Controlled)
                    {
                        if (creature.LastIntGain + m_PetStatGainDelay >= Core.Now)
                        {
                            return;
                        }
                    }
                    else if (from.LastIntGain + m_StatGainDelay >= Core.Now)
                    {
                        return;
                    }

                    from.LastIntGain = Core.Now;
                    break;
                }
        }

        var atrophy = from.RawStatTotal / (double)from.StatCap >= Utility.RandomDouble();

        IncreaseStat(from, stat, atrophy);
    }
}
