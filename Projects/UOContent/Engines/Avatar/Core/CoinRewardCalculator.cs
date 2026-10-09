using System;
using Server.Mobiles;

namespace Server.Engines.Avatar;

public static class CoinRewardCalculator
{
    public static int GetKillCoinValue(BaseCreature creature)
    {
        var value = GetHitsBase(creature);
        if (value < 1)
        {
            return 0;
        }

        value += GetDefenseBase(creature);

        value = ApplyArchetypeMultiplier(value, creature);
        value = ApplyBreathMultiplier(value, creature);

        if (creature.HitPoison != null)
        {
            value = ApplyMultiplier(value, 115);
        }

        if (creature.IsParagon)
        {
            value = ApplyMultiplier(value, 150);
        }

        // Passive creature rewards are notably penalized
        if (creature.FightMode == FightMode.Aggressor)
        {
            value = ApplyMultiplier(value, 25);
        }

        return Math.Max(value, 0);
    }

    private static int ApplyArchetypeMultiplier(int value, BaseCreature creature)
    {
        var multiplier = 0;

        var accuracySkill = Math.Max(
            (int)creature.Skills[SkillName.Wrestling].Value,
            (int)creature.Skills[SkillName.Archery].Value
        );

        if (accuracySkill > 30)
        {
            if (creature.Weapon != null)
            {
                creature.Weapon.GetStatusDamage(creature, out var minDamage, out var maxDamage);

                var damageMinScalar = (int)(0.3 * minDamage);
                if (damageMinScalar > 0)
                {
                    multiplier += damageMinScalar * 5;
                }

                var damageMaxScalar = (int)(0.7 * maxDamage);
                if (damageMaxScalar > 0)
                {
                    multiplier += damageMaxScalar * 5;
                }
            }
            else
            {
                // No weapon, this is our best guess
                var damageMinScalar = creature.DamageMin / 10;
                if (damageMinScalar > 0)
                {
                    multiplier += damageMinScalar * 10;
                }

                var damageMaxScalar = creature.DamageMax / 10;
                if (damageMaxScalar > 0)
                {
                    multiplier += damageMaxScalar * 10;
                }

                multiplier += GetSkillTierBonus((int)creature.Skills[SkillName.Anatomy].Value, 100, 50, 30);
                multiplier += GetSkillTierBonus((int)creature.Skills[SkillName.Tactics].Value, 100, 50, 30);

                if (multiplier > 0)
                {
                    var strScalar = creature.RawStr / 200;
                    if (strScalar > 0)
                    {
                        multiplier += strScalar * 10;
                    }
                }
            }
        }

        var magerySkill = (int)creature.Skills[SkillName.Magery].Value;
        var necromancySkill = (int)creature.Skills[SkillName.Necromancy].Value;
        if (magerySkill > 30 || necromancySkill > 30)
        {
            if (magerySkill > 100)
            {
                multiplier += Math.Min(25, magerySkill - 100);
            }

            if (magerySkill > 80)
            {
                multiplier += 25;
            }

            if (magerySkill > 50)
            {
                multiplier += 25;
            }

            if (magerySkill > 30)
            {
                multiplier += 10;
            }

            if (magerySkill > 0)
            {
                var evalIntSkill = (int)creature.Skills[SkillName.EvalInt].Value;
                if (evalIntSkill > 100)
                {
                    multiplier += Math.Min(25, evalIntSkill - 100);
                }

                if (evalIntSkill > 80)
                {
                    multiplier += 25;
                }

                if (evalIntSkill > 50)
                {
                    multiplier += 25;
                }

                if (evalIntSkill > 30)
                {
                    multiplier += 10;
                }
            }

            if (necromancySkill > 80)
            {
                multiplier += Math.Min(25, necromancySkill - 80);
            }

            if (necromancySkill > 50)
            {
                multiplier += 25;
            }

            if (necromancySkill > 30)
            {
                multiplier += 10;
            }

            // Int only matters if the mob can actually cast
            if (multiplier > 0)
            {
                var intScalar = creature.RawInt / 100;
                if (intScalar > 0)
                {
                    multiplier += intScalar * 20;
                }
            }
        }

        return multiplier > 0 ? ApplyMultiplier(value, 100 + multiplier) : value;
    }

    private static int GetSkillTierBonus(int skill, int high, int mid, int low)
    {
        var bonus = 0;

        if (skill > high)
        {
            bonus += Math.Min(25, skill - high);
        }

        if (skill > mid)
        {
            bonus += 25;
        }

        if (skill > low)
        {
            bonus += 10;
        }

        return bonus;
    }

    private static int ApplyBreathMultiplier(int value, BaseCreature creature)
    {
        var breath = GetBreath(creature);
        if (breath == null)
        {
            return value;
        }

        value = ApplyMultiplier(value, GetBreathElementMultiplier(breath));

        return breath.BreathDamageScalar switch
        {
            >= 0.60 => ApplyMultiplier(value, 135),
            >= 0.40 => ApplyMultiplier(value, 120),
            > 0.20  => ApplyMultiplier(value, 110),
            _       => value
        };
    }

    private static FireBreath GetBreath(BaseCreature creature)
    {
        var abilities = creature.GetMonsterAbilities();
        if (abilities == null)
        {
            return null;
        }

        foreach (var ability in abilities)
        {
            if (ability is FireBreath breath)
            {
                return breath;
            }
        }

        return null;
    }

    private static int ApplyMultiplier(int value, int multiplier) => value * multiplier / 100;

    private static int GetBreathElementMultiplier(FireBreath breath)
    {
        var highest = Math.Max(
            Math.Max(breath.PhysicalDamage, breath.FireDamage),
            Math.Max(breath.ColdDamage, Math.Max(breath.PoisonDamage, breath.EnergyDamage))
        );

        if (highest == breath.PoisonDamage)
        {
            return 130;
        }

        if (highest == breath.PhysicalDamage)
        {
            return 125;
        }

        return Constants.KILL_COIN_BREATH_LARGE;
    }

    private static int GetDefenseBase(BaseCreature creature)
    {
        var lowestResistance = Math.Min(
            creature.PhysicalResistance,
            Math.Min(
                Math.Min(creature.ColdResistance, creature.FireResistance),
                Math.Min(creature.PoisonResistance, creature.EnergyResistance)
            )
        );

        return lowestResistance < 20 ? 0 : lowestResistance / 3;
    }

    private static int GetHitsBase(BaseCreature creature) =>
        creature.HitsMax switch
        {
            < 1   => 0,
            < 25  => 5,
            < 150 => 15,
            < 300 => 20,
            < 500 => 30,
            < 800 => 60,
            _     => 120
        };
}
