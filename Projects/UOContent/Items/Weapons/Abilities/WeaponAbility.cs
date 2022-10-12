using System;
using System.Collections.Generic;
using Server.Engines.ConPVP;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Bushido;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;

namespace Server.Items
{
    public abstract class WeaponAbility
    {
        public static WeaponAbility[] Abilities { get; } =
        {
            null,
            new ArmorIgnore(),
            new BleedAttack(),
            new ConcussionBlow(),
            new CrushingBlow(),
            new Disarm(),
            new Dismount(),
            new DoubleStrike(),
            new InfectiousStrike(),
            new MortalStrike(),
            new MovingShot(),
            new ParalyzingBlow(),
            new ShadowStrike(),
            new WhirlwindAttack(),

            new RidingSwipe(),
            new FrenziedWhirlwind(),
            new Block(),
            new DefenseMastery(),
            new NerveStrike(),
            new TalonStrike(),
            new Feint(),
            new DualWield(),
            new DoubleShot(),
            new ArmorPierce(),
            new Bladeweave(),
            new ForceArrow(),
            new LightningArrow(),
            new PsychicAttack(),
            new SerpentArrow(),
            new ForceOfNature(),
            new InfusedThrow(),
            new MysticArc(),
        };

        public static readonly WeaponAbility ArmorIgnore = Abilities[1];
        public static readonly WeaponAbility BleedAttack = Abilities[2];
        public static readonly WeaponAbility ConcussionBlow = Abilities[3];
        public static readonly WeaponAbility CrushingBlow = Abilities[4];
        public static readonly WeaponAbility Disarm = Abilities[5];
        public static readonly WeaponAbility Dismount = Abilities[6];
        public static readonly WeaponAbility DoubleStrike = Abilities[7];
        public static readonly WeaponAbility InfectiousStrike = Abilities[8];
        public static readonly WeaponAbility MortalStrike = Abilities[9];
        public static readonly WeaponAbility MovingShot = Abilities[10];
        public static readonly WeaponAbility ParalyzingBlow = Abilities[11];
        public static readonly WeaponAbility ShadowStrike = Abilities[12];
        public static readonly WeaponAbility WhirlwindAttack = Abilities[13];

        public static readonly WeaponAbility RidingSwipe = Abilities[14];
        public static readonly WeaponAbility FrenziedWhirlwind = Abilities[15];
        public static readonly WeaponAbility Block = Abilities[16];
        public static readonly WeaponAbility DefenseMastery = Abilities[17];
        public static readonly WeaponAbility NerveStrike = Abilities[18];
        public static readonly WeaponAbility TalonStrike = Abilities[19];
        public static readonly WeaponAbility Feint = Abilities[20];
        public static readonly WeaponAbility DualWield = Abilities[21];
        public static readonly WeaponAbility DoubleShot = Abilities[22];
        public static readonly WeaponAbility ArmorPierce = Abilities[23];

        public static readonly WeaponAbility Bladeweave = Abilities[24];
        public static readonly WeaponAbility ForceArrow = Abilities[25];
        public static readonly WeaponAbility LightningArrow = Abilities[26];
        public static readonly WeaponAbility PsychicAttack = Abilities[27];
        public static readonly WeaponAbility SerpentArrow = Abilities[28];
        public static readonly WeaponAbility ForceOfNature = Abilities[29];

        public static readonly WeaponAbility InfusedThrow = Abilities[30];
        public static readonly WeaponAbility MysticArc = Abilities[31];

        private static readonly Dictionary<Mobile, WeaponAbilityContext> m_PlayersTable = new();

        public virtual int BaseMana => 0;

        public virtual int AccuracyBonus => 0;
        public virtual double DamageScalar => 1.0;

        public virtual bool RequiresSE => false;

        public static Dictionary<Mobile, WeaponAbility> Table { get; } = new();

        public virtual bool ValidatesDuringHit => true;

        public virtual void OnHit(Mobile attacker, Mobile defender, int damage)
        {
        }

        public virtual void OnMiss(Mobile attacker, Mobile defender)
        {
        }

        public virtual bool OnBeforeSwing(Mobile attacker, Mobile defender) => true;

        public virtual bool OnBeforeDamage(Mobile attacker, Mobile defender) => true;

        public virtual bool RequiresTactics(Mobile from) => true;

        public virtual bool RequiresSecondarySkill(Mobile from) => false;

        /// <summary>
        /// Return primary skill for ability. Default 70/90
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public virtual double GetRequiredSkill(Mobile from)
        {
            if (from.Weapon is BaseWeapon weapon)
            {
                if (weapon.PrimaryAbility == this || weapon.PrimaryAbility == Bladeweave)
                {
                    return 70.0;
                }

                if (weapon.SecondaryAbility == this || weapon.SecondaryAbility == Bladeweave)
                {
                    return 90.0;
                }
            }

            return 200.0;
        }

        /// <summary>
        /// Returns secondary skill for Ability. Default 50.
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public virtual double GetRequiredSecondarySkill(Mobile from) => 50.0;

        /// <summary>
        /// Return secondary skillName. Default bushido/ninjitsu
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public virtual SkillName GetSecondarySkillName(Mobile from) => from.Skills[SkillName.Ninjitsu].Base > from.Skills[SkillName.Bushido].Base ? SkillName.Ninjitsu : SkillName.Bushido;

        /// <summary>
        /// Returns tactics needed for Ability. Default 70/90.
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public virtual double GetRequiredTactics(Mobile from)
        {
            if (from.Weapon is BaseWeapon weapon)
            {
                if (weapon.PrimaryAbility == this || weapon.PrimaryAbility == Bladeweave)
                {
                    return 70.0;
                }

                if (weapon.SecondaryAbility == this || weapon.SecondaryAbility == Bladeweave)
                {
                    return 90.0;
                }
            }

            return 200.0;
        }

        public virtual int CalculateMana(Mobile from)
        {
            var mana = BaseMana;

            var skillTotal =
                GetSkill(from, SkillName.Swords) +
                GetSkill(from, SkillName.Macing) +
                GetSkill(from, SkillName.Fencing) +
                GetSkill(from, SkillName.Archery) +
                GetSkill(from, SkillName.Parry) +
                GetSkill(from, SkillName.Lumberjacking) +
                GetSkill(from, SkillName.Stealth) +
                GetSkill(from, SkillName.Poisoning) +
                GetSkill(from, SkillName.Bushido) +
                GetSkill(from, SkillName.Ninjitsu);

            if (skillTotal >= 300.0)
            {
                mana -= 10;
            }
            else if (skillTotal >= 200.0)
            {
                mana -= 5;
            }

            var scalar = 1.0;
            if (!MindRotSpell.GetMindRotScalar(from, ref scalar))
            {
                scalar = 1.0;
            }

            // Lower Mana Cost = 40%
            var lmc = Math.Min(AosAttributes.GetValue(from, AosAttribute.LowerManaCost), 40);

            scalar -= (double)lmc / 100;
            mana = (int)(mana * scalar);

            // Using a special move within 3 seconds of the previous special move costs double mana
            if (GetContext(from) != null)
            {
                mana *= 2;
            }

            return mana;
        }

        public virtual bool CheckWeaponSkill(Mobile from)
        {
            if (from.Weapon is not BaseWeapon weapon)
            {
                return false;
            }

            var skill = from.Skills[weapon.Skill];
            var reqSkill = GetRequiredSkill(from);
            var reqTactics = Core.ML && RequiresTactics(from);

            if (reqTactics && from.Skills.Tactics.Base < GetRequiredTactics(from))
            {
                // You need ~1_SKILL_REQUIREMENT~ weapon and tactics skill to perform that attack
                from.SendLocalizedMessage(1079308, reqSkill.ToString());
                return false;
            }

            var reqSecondarySkill = GetRequiredSecondarySkill(from);
            SkillName secondarySkill = GetSecondarySkillName(from);

            if (RequiresSecondarySkill(from) && from.Skills[secondarySkill].Base < reqSecondarySkill)
            {
                int loc = GetSkillLocalization(secondarySkill);

                if (loc == 1060184)
                {
                    from.SendLocalizedMessage(loc);
                }
                else
                {
                    from.SendLocalizedMessage(loc, reqSecondarySkill.ToString());
                }

                return false;
            }

            if (skill?.Base >= reqSkill)
            {
                return true;
            }

            /* <UBWS> */
            if (weapon.WeaponAttributes.UseBestSkill > 0 &&
                (from.Skills.Swords.Base >= reqSkill ||
                 from.Skills.Macing.Base >= reqSkill ||
                 from.Skills.Fencing.Base >= reqSkill)
            )
            {
                return true;
            }
            /* </UBWS> */

            // You need ~1_SKILL_REQUIREMENT~ weapon skill to perform that attack
            from.SendLocalizedMessage(1060182, reqSkill.ToString());

            return false;
        }
        private int GetSkillLocalization(SkillName skill)
        {
            return skill switch
            {
                SkillName.Bushido   => 1063347, // You need ~1_SKILL_REQUIREMENT~ Bushido or Ninjitsu skill to perform that attack!
                SkillName.Ninjitsu  => 1063347, // You need ~1_SKILL_REQUIREMENT~ Bushido or Ninjitsu skill to perform that attack!
                SkillName.Poisoning => 1060184, // You lack the required poisoning to perform that attack
                _                   => 1157351 // You need ~1_SKILL_REQUIREMENT~ weapon and tactics skill to perform that attack
            };
        }


        public virtual bool CheckSkills(Mobile from) => CheckWeaponSkill(from);

        public virtual double GetSkill(Mobile from, SkillName skillName) => from.Skills[skillName]?.Value ?? 0.0;

        public virtual bool CheckMana(Mobile from, bool consume)
        {
            var mana = CalculateMana(from);

            if (from.Mana < mana)
            {
                if (from is BaseCreature creature && creature.HasManaOveride)
                {
                    return true;
                }

                // You need ~1_MANA_REQUIREMENT~ mana to perform that attack
                from.SendLocalizedMessage(1060181, mana.ToString());
                return false;
            }

            if (consume)
            {
                if (GetContext(from) == null)
                {
                    Timer timer = new WeaponAbilityTimer(from);
                    timer.Start();

                    AddContext(from, new WeaponAbilityContext(timer));
                }

                from.Mana -= mana;
            }

            return true;
        }

        public virtual bool Validate(Mobile from)
        {
            if (!from.Player)
            {
                return true;
            }

            var state = from.NetState;

            if (state == null)
            {
                return false;
            }

            if (RequiresSE && !state.SupportsExpansion(Expansion.SE))
            {
                from.SendLocalizedMessage(1063456); // You must upgrade to Samurai Empire in order to use that ability.
                return false;
            }

            if (HonorableExecution.IsUnderPenalty(from) || AnimalForm.UnderTransformation(from))
            {
                from.SendLocalizedMessage(1063024); // You cannot perform this special move right now.
                return false;
            }

            if (Core.ML && from.Spell != null)
            {
                from.SendLocalizedMessage(1063024); // You cannot perform this special move right now.
                return false;
            }

            string option = this switch
            {
                ArmorIgnore _       => "Armor Ignore",
                BleedAttack _       => "Bleed Attack",
                ConcussionBlow _    => "Concussion Blow",
                CrushingBlow _      => "Crushing Blow",
                Disarm _            => "Disarm",
                Dismount _          => "Dismount",
                DoubleStrike _      => "Double Strike",
                InfectiousStrike _  => "Infectious Strike",
                MortalStrike _      => "Mortal Strike",
                MovingShot _        => "Moving Shot",
                ParalyzingBlow _    => "Paralyzing Blow",
                ShadowStrike _      => "Shadow Strike",
                WhirlwindAttack _   => "Whirlwind Attack",
                RidingSwipe _       => "Riding Swipe",
                FrenziedWhirlwind _ => "Frenzied Whirlwind",
                Block _             => "Block",
                DefenseMastery _    => "Defense Mastery",
                NerveStrike _       => "Nerve Strike",
                TalonStrike _       => "Talon Strike",
                Feint _             => "Feint",
                DualWield  _        => "Dual Wield",
                DoubleShot _        => "Double Shot",
                ArmorPierce _       => "Armor Pierce",
                _                   => null
            };

            if (option != null && !DuelContext.AllowSpecialAbility(from, option, true))
            {
                return false;
            }

            return CheckSkills(from);
        }

        public static bool IsWeaponAbility(Mobile m, WeaponAbility a) =>
            a == null || !m.Player || m.Weapon is BaseWeapon weapon &&
            (weapon.PrimaryAbility == a || weapon.SecondaryAbility == a);

        public static WeaponAbility GetCurrentAbility(Mobile m)
        {
            if (!Core.AOS)
            {
                ClearCurrentAbility(m);
                return null;
            }

            Table.TryGetValue(m, out var a);

            if (!IsWeaponAbility(m, a))
            {
                ClearCurrentAbility(m);
                return null;
            }

            if (a?.ValidatesDuringHit == true && (!a.Validate(m) || !a.CheckMana(m, false)))
            {
                ClearCurrentAbility(m);
                return null;
            }

            return a;
        }

        public static bool SetCurrentAbility(Mobile m, WeaponAbility a)
        {
            if (!Core.AOS)
            {
                ClearCurrentAbility(m);
                return false;
            }

            if (!IsWeaponAbility(m, a))
            {
                ClearCurrentAbility(m);
                return false;
            }

            if (a?.Validate(m) == false || a?.CheckMana(m, false) == false)
            {
                ClearCurrentAbility(m);
                return false;
            }

            if (a == null)
            {
                Table.Remove(m);
            }
            else
            {
                SpecialMove.ClearCurrentMove(m);
                Table[m] = a;
            }

            return true;
        }

        public static void ClearCurrentAbility(Mobile m)
        {
            Table.Remove(m);

            if (Core.AOS)
            {
                m.NetState.SendClearWeaponAbility();
            }
        }

        private static void AddContext(Mobile m, WeaponAbilityContext context)
        {
            m_PlayersTable[m] = context;
        }

        private static void RemoveContext(Mobile m)
        {
            if (m_PlayersTable.Remove(m, out var context))
            {
                context.Timer?.Stop();
            }
        }

        private static WeaponAbilityContext GetContext(Mobile m)
        {
            m_PlayersTable.TryGetValue(m, out var context);
            return context;
        }

        private class WeaponAbilityTimer : Timer
        {
            private readonly Mobile m_Mobile;

            public WeaponAbilityTimer(Mobile from) : base(TimeSpan.FromSeconds(3.0)) => m_Mobile = from;

            protected override void OnTick()
            {
                RemoveContext(m_Mobile);
            }
        }

        private class WeaponAbilityContext
        {
            public WeaponAbilityContext(Timer timer) => Timer = timer;

            public Timer Timer { get; }
        }
    }
}
