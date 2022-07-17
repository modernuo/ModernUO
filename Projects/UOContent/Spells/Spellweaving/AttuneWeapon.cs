using System;
using System.Collections.Generic;

namespace Server.Spells.Spellweaving
{
    public class AttuneWeaponSpell : ArcanistSpell
    {
        private static readonly SpellInfo _info = new(
            "Attune Weapon",
            "Haeldril",
            -1
        );

        private static readonly Dictionary<Mobile, ExpireTimer> _table = new();

        public AttuneWeaponSpell(Mobile caster, Item scroll = null)
            : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

        public override double RequiredSkill => 0.0;
        public override int RequiredMana => 24;

        public override bool CheckCast()
        {
            if (_table.ContainsKey(Caster))
            {
                Caster.SendLocalizedMessage(501775); // This spell is already in effect.
                return false;
            }

            if (Caster.CanBeginAction<AttuneWeaponSpell>())
            {
                return base.CheckCast();
            }

            Caster.SendLocalizedMessage(1075124); // You must wait before casting that spell again.
            return false;
        }

        public override void OnCast()
        {
            if (CheckSequence())
            {
                Caster.PlaySound(0x5C3);
                Caster.FixedParticles(0x3728, 1, 13, 0x26B8, 0x455, 7, EffectLayer.Waist);
                Caster.FixedParticles(0x3779, 1, 15, 0x251E, 0x3F, 7, EffectLayer.Waist);

                var skill = Caster.Skills.Spellweaving.Value;

                var damageAbsorb = (int)(18 + (skill - 10) / 10 * 3 + FocusLevel * 6);
                Caster.MeleeDamageAbsorb = damageAbsorb;

                var duration = TimeSpan.FromSeconds(60 + FocusLevel * 12);

                var t = new ExpireTimer(Caster, duration);
                t.Start();

                _table[Caster] = t;

                Caster.BeginAction<AttuneWeaponSpell>();

                BuffInfo.AddBuff(
                    Caster,
                    new BuffInfo(BuffIcon.AttuneWeapon, 1075798, duration, Caster, damageAbsorb.ToString())
                );
            }

            FinishSequence();
        }

        public static void TryAbsorb(Mobile defender, ref int damage)
        {
            if (damage == 0 || !IsAbsorbing(defender) || defender.MeleeDamageAbsorb <= 0)
            {
                return;
            }

            var absorbed = Math.Min(damage, defender.MeleeDamageAbsorb);

            damage -= absorbed;
            defender.MeleeDamageAbsorb -= absorbed;

            // ~1_damage~ point(s) of damage have been absorbed. A total of ~2_remaining~ point(s) of shielding remain.
            defender.SendLocalizedMessage(1075127, $"{absorbed}\t{defender.MeleeDamageAbsorb}");

            if (defender.MeleeDamageAbsorb <= 0)
            {
                StopAbsorbing(defender, true);
            }
        }

        public static bool IsAbsorbing(Mobile m) => _table.ContainsKey(m);

        public static void StopAbsorbing(Mobile m, bool message)
        {
            if (_table.TryGetValue(m, out var t))
            {
                t.DoExpire(message);
            }
        }

        private class ExpireTimer : Timer
        {
            private readonly Mobile m_Mobile;

            public ExpireTimer(Mobile m, TimeSpan delay)
                : base(delay) =>
                m_Mobile = m;

            protected override void OnTick()
            {
                DoExpire(true);
            }

            public void DoExpire(bool message)
            {
                Stop();

                m_Mobile.MeleeDamageAbsorb = 0;

                if (message)
                {
                    m_Mobile.SendLocalizedMessage(1075126); // Your attunement fades.
                    m_Mobile.PlaySound(0x1F8);
                }

                _table.Remove(m_Mobile);

                StartTimer(TimeSpan.FromSeconds(120), m_Mobile.EndAction<AttuneWeaponSpell>);
                BuffInfo.RemoveBuff(m_Mobile, BuffIcon.AttuneWeapon);
            }
        }
    }
}
