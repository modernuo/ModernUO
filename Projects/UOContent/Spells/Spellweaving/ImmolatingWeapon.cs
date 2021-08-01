using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Spells.Spellweaving
{
    public class ImmolatingWeaponSpell : ArcanistSpell
    {
        private static readonly SpellInfo m_Info = new(
            "Immolating Weapon",
            "Thalshara",
            -1
        );

        private static readonly Dictionary<BaseWeapon, ImmolatingWeaponTimer> m_Table = new();

        public ImmolatingWeaponSpell(Mobile caster, Item scroll = null)
            : base(caster, scroll, m_Info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

        public override double RequiredSkill => 10.0;
        public override int RequiredMana => 32;

        public override bool CheckCast()
        {
            if (!(Caster.Weapon is BaseWeapon weapon) || weapon is Fists || weapon is BaseRanged)
            {
                Caster.SendLocalizedMessage(1060179); // You must be wielding a weapon to use this ability!
                return false;
            }

            return base.CheckCast();
        }

        public override void OnCast()
        {
            if (!(Caster.Weapon is BaseWeapon weapon) || weapon is Fists || weapon is BaseRanged)
            {
                Caster.SendLocalizedMessage(1060179); // You must be wielding a weapon to use this ability!
            }
            else if (CheckSequence())
            {
                Caster.PlaySound(0x5CA);
                Caster.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);

                if (!IsImmolating(weapon)) // On OSI, the effect is not re-applied
                {
                    var skill = Caster.Skills.Spellweaving.Value;

                    var duration = 10 + (int)(skill / 24) + FocusLevel;
                    var damage = 5 + (int)(skill / 24) + FocusLevel;

                    var t = new ImmolatingWeaponTimer(TimeSpan.FromSeconds(duration), damage, Caster, weapon);
                    m_Table[weapon] = t;
                    t.Start();

                    weapon.InvalidateProperties();
                }
            }

            FinishSequence();
        }

        public static bool IsImmolating(BaseWeapon weapon) => m_Table.ContainsKey(weapon);

        public static int GetImmolatingDamage(BaseWeapon weapon) =>
            m_Table.TryGetValue(weapon, out var entry) ? entry._damage : 0;

        public static void DoEffect(BaseWeapon weapon, Mobile target)
        {
            if (m_Table.Remove(weapon, out var timer))
            {
                timer.Stop();

                Timer.StartTimer(TimeSpan.FromSeconds(0.25), () => FinishEffect(target, timer));
            }
        }

        private static void FinishEffect(Mobile target, ImmolatingWeaponTimer timer)
        {
            AOS.Damage(target, timer._caster, timer._damage, 0, 100, 0, 0, 0);
        }

        public static void StopImmolating(BaseWeapon weapon)
        {
            if (m_Table.Remove(weapon, out var timer))
            {
                timer._caster?.PlaySound(0x27);
                timer.Stop();

                weapon.InvalidateProperties();
            }
        }

        private class ImmolatingWeaponTimer : Timer
        {
            public readonly Mobile _caster;
            public readonly int _damage;
            public readonly BaseWeapon _weapon;

            public ImmolatingWeaponTimer(TimeSpan duration, int damage, Mobile caster, BaseWeapon weapon) : base(duration)
            {
                _damage = damage;
                _caster = caster;
                _weapon = weapon;
            }

            protected override void OnTick()
            {
                StopImmolating(_weapon);
            }
        }
    }
}
