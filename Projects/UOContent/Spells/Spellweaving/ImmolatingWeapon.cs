using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Spells.Spellweaving
{
    public class ImmolatingWeaponSpell : ArcanistSpell
    {
        private static readonly SpellInfo _info = new(
            "Immolating Weapon",
            "Thalshara",
            -1
        );

        private static readonly Dictionary<BaseWeapon, ImmolatingWeaponTimer> _table = new();

        public ImmolatingWeaponSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

        public override double RequiredSkill => 10.0;
        public override int RequiredMana => 32;

        public override bool CheckCast()
        {
            if (Caster.Weapon is not BaseWeapon weapon || weapon is Fists or BaseRanged)
            {
                Caster.SendLocalizedMessage(1060179); // You must be wielding a weapon to use this ability!
                return false;
            }

            return base.CheckCast();
        }

        public override void OnCast()
        {
            if (Caster.Weapon is not BaseWeapon weapon || weapon is Fists or BaseRanged)
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
                    _table[weapon] = t;
                    t.Start();

                    weapon.InvalidateProperties();
                }
            }

            FinishSequence();
        }

        public static bool IsImmolating(BaseWeapon weapon) => _table.ContainsKey(weapon);

        public static int GetImmolatingDamage(BaseWeapon weapon) =>
            _table.TryGetValue(weapon, out var entry) ? entry._damage : 0;

        public static void DoEffect(BaseWeapon weapon, Mobile target)
        {
            if (_table.Remove(weapon, out var timer))
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
            if (_table.Remove(weapon, out var timer))
            {
                timer._caster?.PlaySound(0x27);
                timer.Stop();

                weapon.InvalidateProperties();
            }
        }

        private class ImmolatingWeaponTimer : Timer
        {
            public Mobile _caster;
            public int _damage;
            public BaseWeapon _weapon;

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
