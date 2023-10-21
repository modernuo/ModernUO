using System;
using System.Runtime.CompilerServices;
using Server.Factions;
using Server.Mobiles;

namespace Server.Misc;

public static class SkillCheck
{
    private static int _statMax;
    private static double _statGainChanceMultiplier;
    private static bool _usePub45StatGain;
    private static double _primaryStatGainChance;
    private static TimeSpan _statGainDelay;
    private static TimeSpan _petStatGainDelay;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool RollStatIncreaseChance(double statGain) =>
        statGain / 33.3 * _statGainChanceMultiplier > Utility.RandomDouble();

    public static void Configure()
    {
        _statMax = ServerConfiguration.GetOrUpdateSetting("stats.statMax", Core.LBR ? 125 : 100);
        _statGainChanceMultiplier = ServerConfiguration.GetOrUpdateSetting("stats.gainChanceMultiplier", 1.0);
        _primaryStatGainChance = ServerConfiguration.GetSetting("stats.primaryStatGainChance", 0.75);
        _statGainDelay = ServerConfiguration.GetSetting("stats.gainDelay", TimeSpan.FromMinutes(Core.ML ? 0.05 : 10));
        _petStatGainDelay = ServerConfiguration.GetSetting("stats.petGainDelay", TimeSpan.FromMinutes(5.0));

        // Publish 45 - Preparation for UOKR
        _usePub45StatGain = ServerConfiguration.GetSetting("stats.usePub45StatGain", Core.ML);
    }

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

            // Gain one stat per 10 minutes, regardless of whether you gain in the skill
            if (!_usePub45StatGain && success)
            {
                LegacyGain(from, skill.Info);
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
                        toLower.BaseFixedPoint = Math.Max(toLower.BaseFixedPoint - toGain, 0);
                        break;
                    }
                }
            }

            if (from is PlayerMobile pm && skill.SkillName == pm.AcceleratedSkill && pm.AcceleratedStart > Core.Now)
            {
                toGain *= Utility.RandomMinMax(2, 5);
            }

            if (!from.Player || skills.Total < skills.Cap)
            {
                skill.BaseFixedPoint = Math.Min(skill.BaseFixedPoint + toGain, skill.CapFixedPoint);
            }
        }

        if (_usePub45StatGain && skill.Lock == SkillLock.Up)
        {
            var info = skill.Info;

            var primaryStat = info.PrimaryStat;
            var secondaryStat = info.SecondaryStat;

            var primaryStatLock = primaryStat.ToLock(from);
            var secondaryStatLock = secondaryStat.ToLock(from);

            if (primaryStatLock != StatLockType.Up && secondaryStatLock != StatLockType.Up)
            {
                return;
            }

            // Flat 1 in 20 chance to gain anything
            if (0.05 * _statGainChanceMultiplier > Utility.RandomDouble())
            {
                // 75% for primary, 25% for secondary - Unless primary is not set to gain.
                var statToGain = primaryStatLock is StatLockType.Up && _primaryStatGainChance > Utility.RandomDouble()
                    ? primaryStat
                    : secondaryStat;

                GainStat(from, statToGain);
            }
        }
    }

    public static void LegacyGain(Mobile from, SkillInfo info)
    {
        if (info.StrGain > 0 && from.StrLock == StatLockType.Up && RollStatIncreaseChance(info.StrGain))
        {
            GainStat(from, Stat.Str);
        }

        if (info.DexGain > 0 && from.DexLock == StatLockType.Up && RollStatIncreaseChance(info.DexGain))
        {
            GainStat(from, Stat.Dex);
        }

        if (info.IntGain > 0 && from.IntLock == StatLockType.Up && RollStatIncreaseChance(info.IntGain))
        {
            GainStat(from, Stat.Int);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StatLockType ToLock(this Stat stat, Mobile from) =>
        stat switch
        {
            Stat.Str => from.StrLock,
            Stat.Dex => from.DexLock,
            Stat.Int => from.IntLock,
            _ => StatLockType.Up
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            Stat.Str => from.StrLock == StatLockType.Up && from.RawStr < _statMax,
            Stat.Dex => from.DexLock == StatLockType.Up && from.RawDex < _statMax,
            Stat.Int => from.IntLock == StatLockType.Up && from.RawInt < _statMax,
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
                            from.RawDex -= 1;
                        }
                        else if (CanLower(from, Stat.Int))
                        {
                            from.RawInt -= 1;
                        }
                    }

                    if (CanRaise(from, Stat.Str))
                    {
                        from.RawStr = Math.Min(from.RawStr + 1, _statMax);
                    }

                    break;
                }
            case Stat.Dex:
                {
                    if (atrophy)
                    {
                        if (CanLower(from, Stat.Str) && (from.RawStr < from.RawInt || !CanLower(from, Stat.Int)))
                        {
                            from.RawStr -= 1;
                        }
                        else if (CanLower(from, Stat.Int))
                        {
                            from.RawInt -= 1;
                        }
                    }

                    if (CanRaise(from, Stat.Dex))
                    {
                        from.RawDex = Math.Min(from.RawDex + 1, _statMax);
                    }

                    break;
                }
            case Stat.Int:
                {
                    if (atrophy)
                    {
                        if (CanLower(from, Stat.Str) && (from.RawStr < from.RawDex || !CanLower(from, Stat.Dex)))
                        {
                            from.RawStr -= 1;
                        }
                        else if (CanLower(from, Stat.Dex))
                        {
                            from.RawDex -= 1;
                        }
                    }

                    if (CanRaise(from, Stat.Int))
                    {
                        from.RawInt = Math.Min(from.RawInt + 1, _statMax);
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
                        if (creature.LastStrGain + _petStatGainDelay >= Core.Now)
                        {
                            return;
                        }
                    }
                    else if (from.LastStrGain + _statGainDelay >= Core.Now)
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
                        if (creature.LastDexGain + _petStatGainDelay >= Core.Now)
                        {
                            return;
                        }
                    }
                    else if (from.LastDexGain + _statGainDelay >= Core.Now)
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
                        if (creature.LastIntGain + _petStatGainDelay >= Core.Now)
                        {
                            return;
                        }
                    }
                    else if (from.LastIntGain + _statGainDelay >= Core.Now)
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
