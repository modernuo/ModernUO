using System;
using Server.Factions;
using Server.Mobiles;
using Server.Regions;

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

    public const int Allowance = 3; // How many times may we use the same location/target for gain

    private const int
        LocationSize = 5; // The size of eeach location, make this smaller so players dont have to move as far

    private static readonly bool AntiMacroCode = !Core.ML; // Change this to false to disable anti-macro code

    public static TimeSpan AntiMacroExpire = TimeSpan.FromMinutes(5.0); // How long do we remember targets/locations?

    private static readonly bool[] UseAntiMacro =
    {
        // true if this skill uses the anti-macro code, false if it does not
        false, // Alchemy = 0,
        true,  // Anatomy = 1,
        true,  // AnimalLore = 2,
        true,  // ItemID = 3,
        true,  // ArmsLore = 4,
        false, // Parry = 5,
        true,  // Begging = 6,
        false, // Blacksmith = 7,
        false, // Fletching = 8,
        true,  // Peacemaking = 9,
        true,  // Camping = 10,
        false, // Carpentry = 11,
        false, // Cartography = 12,
        false, // Cooking = 13,
        true,  // DetectHidden = 14,
        true,  // Discordance = 15,
        true,  // EvalInt = 16,
        true,  // Healing = 17,
        true,  // Fishing = 18,
        true,  // Forensics = 19,
        true,  // Herding = 20,
        true,  // Hiding = 21,
        true,  // Provocation = 22,
        false, // Inscribe = 23,
        true,  // Lockpicking = 24,
        true,  // Magery = 25,
        true,  // MagicResist = 26,
        false, // Tactics = 27,
        true,  // Snooping = 28,
        true,  // Musicianship = 29,
        true,  // Poisoning = 30,
        false, // Archery = 31,
        true,  // SpiritSpeak = 32,
        true,  // Stealing = 33,
        false, // Tailoring = 34,
        true,  // AnimalTaming = 35,
        true,  // TasteID = 36,
        false, // Tinkering = 37,
        true,  // Tracking = 38,
        true,  // Veterinary = 39,
        false, // Swords = 40,
        false, // Macing = 41,
        false, // Fencing = 42,
        false, // Wrestling = 43,
        true,  // Lumberjacking = 44,
        true,  // Mining = 45,
        true,  // Meditation = 46,
        true,  // Stealth = 47,
        true,  // RemoveTrap = 48,
        true,  // Necromancy = 49,
        false, // Focus = 50,
        true,  // Chivalry = 51
        true,  // Bushido = 52
        true,  // Ninjitsu = 53
        true,  // Spellweaving
        true,  // Mysticism = 55
        true,  // Imbuing = 56
        false, // Throwing = 57
    };

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

        if (value >= maxSkill)
        {
            return true; // No challenge
        }

        var chance = (value - minSkill) / (maxSkill - minSkill);

        var loc = new Point2D(from.Location.X / LocationSize, from.Location.Y / LocationSize);
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

        var loc = new Point2D(from.Location.X / LocationSize, from.Location.Y / LocationSize);
        return CheckSkill(from, skill, loc, chance);
    }

    public static bool CheckSkill(Mobile from, Skill skill, object amObj, double chance)
    {
        if (from.Skills.Cap == 0)
        {
            return false;
        }

        var success = chance >= Utility.RandomDouble();
        var gc = (double)(from.Skills.Cap - from.Skills.Total) / from.Skills.Cap;
        gc += (skill.Cap - skill.Base) / skill.Cap;
        gc /= 2;

        gc += (1.0 - chance) * (success ? 0.5 :
            Core.AOS ? 0.0 : 0.2);
        gc /= 2;

        gc *= skill.Info.GainFactor;

        if (gc < 0.01)
        {
            gc = 0.01;
        }

        if (from is BaseCreature creature && creature.Controlled)
        {
            gc *= 2;
        }

        if (from.Alive && (gc >= Utility.RandomDouble() && AllowGain(from, skill, amObj) || skill.Base < 10.0))
        {
            Gain(from, skill);
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

        if (value >= maxSkill)
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

        if (AntiMacroCode && from is PlayerMobile mobile && UseAntiMacro[skill.Info.SkillID])
        {
            return mobile.AntiMacroCheck(skill, obj);
        }

        return true;
    }

    public static void Gain(Mobile from, Skill skill)
    {
        if (from.Region.IsPartOf<JailRegion>())
        {
            return;
        }

        if (from is BaseCreature creature && creature.IsDeadPet)
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

            if (from.Player && skills.Total / skills.Cap >= Utility.RandomDouble())
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
