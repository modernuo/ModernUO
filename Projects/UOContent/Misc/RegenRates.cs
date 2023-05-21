using System;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;

namespace Server.Misc
{
    public static class RegenRates
    {
        [CallPriority(10)]
        public static void Configure()
        {
            Mobile.DefaultHitsRate = TimeSpan.FromSeconds(11.0);
            Mobile.DefaultStamRate = TimeSpan.FromSeconds(7.0);
            Mobile.DefaultManaRate = TimeSpan.FromSeconds(7.0);

            Mobile.ManaRegenRateHandler = Mobile_ManaRegenRate;

            if (Core.AOS)
            {
                Mobile.StamRegenRateHandler = Mobile_StamRegenRate;
                Mobile.HitsRegenRateHandler = Mobile_HitsRegenRate;
            }
        }

        private static void CheckBonusSkill(Mobile m, int cur, int max, SkillName skill)
        {
            if (!m.Alive)
            {
                return;
            }

            var n = (double)cur / max;
            var v = Math.Sqrt(m.Skills[skill].Value * 0.005);

            n *= 1.0 - v;
            n += v;

            m.CheckSkill(skill, n);
        }

        private static TimeSpan Mobile_HitsRegenRate(Mobile from)
        {
            var points = AosAttributes.GetValue(from, AosAttribute.RegenHits);

            var bc = from as BaseCreature;

            if (bc?.IsAnimatedDead == false)
            {
                points += 4;
            }

            if (bc?.IsParagon == true || from is Leviathan)
            {
                points += 40;
            }

            if (Core.ML && from is PlayerMobile) // does racial bonus go before/after?
            {
                if (from.Race == Race.Human)
                {
                    points += 2;
                }

                points = Math.Min(points, 18);
            }

            if (points < 0)
            {
                points = 0;
            }

            if (TransformationSpellHelper.UnderTransformation(from, typeof(HorrificBeastSpell)))
            {
                points += 20;
            }

            if (AnimalForm.UnderTransformation(from, typeof(Dog)) || AnimalForm.UnderTransformation(from, typeof(Cat)))
            {
                points += from.Skills.Ninjitsu.Fixed / 30;
            }

            return TimeSpan.FromSeconds(10.0 / (1 + points));
        }

        private static TimeSpan Mobile_StamRegenRate(Mobile from)
        {
            if (from.Skills == null)
            {
                return Mobile.DefaultStamRate;
            }

            CheckBonusSkill(from, from.Stam, from.StamMax, SkillName.Focus);

            var points = (int)(from.Skills.Focus.Value * 0.1);

            if (from is BaseCreature creature && creature.IsParagon || from is Leviathan)
            {
                points += 40;
            }

            var cappedPoints = AosAttributes.GetValue(from, AosAttribute.RegenStam);

            if (TransformationSpellHelper.UnderTransformation(from, typeof(VampiricEmbraceSpell)))
            {
                cappedPoints += 15;
            }

            if (AnimalForm.UnderTransformation(from, typeof(Kirin)))
            {
                cappedPoints += 20;
            }

            if (Core.ML && from is PlayerMobile)
            {
                cappedPoints = Math.Min(cappedPoints, 24);
            }

            points += cappedPoints;

            if (points < -1)
            {
                points = -1;
            }

            return TimeSpan.FromSeconds(1.0 / (0.1 * (2 + points)));
        }

        private static TimeSpan Mobile_ManaRegenRate(Mobile from)
        {
            if (from.Skills == null)
            {
                return Mobile.DefaultManaRate;
            }

            if (!from.Meditating)
            {
                CheckBonusSkill(from, from.Mana, from.ManaMax, SkillName.Meditation);
            }

            double rate;
            var armorPenalty = GetArmorOffset(from);

            if (Core.AOS)
            {
                var medPoints = from.Int + from.Skills.Meditation.Value * 3;

                medPoints *= from.Skills.Meditation.Value < 100.0 ? 0.025 : 0.0275;

                CheckBonusSkill(from, from.Mana, from.ManaMax, SkillName.Focus);

                var focusPoints = from.Skills.Focus.Value * 0.05;

                if (armorPenalty > 0)
                {
                    medPoints = 0; // In AOS, wearing any meditation-blocking armor completely removes meditation bonus
                }

                var totalPoints = focusPoints + medPoints + (from.Meditating ? medPoints > 13.0 ? 13.0 : medPoints : 0.0);

                if (from is BaseCreature creature && creature.IsParagon || from is Leviathan)
                {
                    totalPoints += 40;
                }

                var cappedPoints = AosAttributes.GetValue(from, AosAttribute.RegenMana);

                if (TransformationSpellHelper.UnderTransformation(from, typeof(VampiricEmbraceSpell)))
                {
                    cappedPoints += 3;
                }
                else if (TransformationSpellHelper.UnderTransformation(from, typeof(LichFormSpell)))
                {
                    cappedPoints += 13;
                }

                if (Core.ML && from is PlayerMobile)
                {
                    cappedPoints = Math.Min(cappedPoints, 18);
                }

                totalPoints += cappedPoints;

                if (totalPoints < -1)
                {
                    totalPoints = -1;
                }

                if (Core.ML)
                {
                    totalPoints = Math.Floor(totalPoints);
                }

                rate = 1.0 / (0.1 * (2 + totalPoints));
            }
            else
            {
                var medPoints = (from.Int + from.Skills.Meditation.Value) * 0.5;

                rate = medPoints switch
                {
                    <= 0   => 7.0,
                    <= 100 => 7.0 - 239 * medPoints / 2400 + 19 * medPoints * medPoints / 48000,
                    < 120  => 1.0,
                    _      => 0.75
                };

                rate += armorPenalty;

                if (from.Meditating)
                {
                    rate *= 0.5;
                }

                rate = Math.Clamp(rate, 0.5, 7.0);
            }

            return TimeSpan.FromSeconds(rate);
        }

        public static double GetArmorOffset(Mobile from)
        {
            var rating = 0.0;

            if (!Core.AOS)
            {
                rating += GetArmorMeditationValue(from.ShieldArmor as BaseArmor);
            }

            rating += GetArmorMeditationValue(from.NeckArmor as BaseArmor);
            rating += GetArmorMeditationValue(from.HandArmor as BaseArmor);
            rating += GetArmorMeditationValue(from.HeadArmor as BaseArmor);
            rating += GetArmorMeditationValue(from.ArmsArmor as BaseArmor);
            rating += GetArmorMeditationValue(from.LegsArmor as BaseArmor);
            rating += GetArmorMeditationValue(from.ChestArmor as BaseArmor);

            return rating / 4;
        }

        private static double GetArmorMeditationValue(BaseArmor ar)
        {
            if (ar?.ArmorAttributes.MageArmor != 0 || ar.Attributes.SpellChanneling != 0)
            {
                return 0.0;
            }

            return ar.MeditationAllowance switch
            {
                ArmorMeditationAllowance.None => ar.BaseArmorRatingScaled,
                ArmorMeditationAllowance.Half => ar.BaseArmorRatingScaled / 2.0,
                ArmorMeditationAllowance.All  => 0.0,
                _                             => ar.BaseArmorRatingScaled
            };
        }
    }
}
