using System;
using System.Collections.Generic;
using Server.Spells;

namespace Server.Items
{
    /// <summary>
    ///     A quick attack to all enemies in range of your weapon that causes damage over time. Requires Bushido or Ninjitsu skill.
    /// </summary>
    public class FrenziedWhirlwind : WeaponAbility
    {
        public override int BaseMana => 30;

        public static Dictionary<Mobile, FrenziedWirlwindTimer> Registry { get; } = new();
        public override bool RequiresTactics(Mobile from) => false;
        public override bool RequiresSecondarySkill(Mobile from) => true;

        public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            var map = attacker.Map;

            if (!(map != null && attacker.Weapon is BaseWeapon weapon))
            {
                return;
            }

            // 5-15 damage
            var amount = (int)(10.0 * ((Math.Max(
                attacker.Skills.Bushido.Value,
                attacker.Skills.Ninjitsu.Value
            ) - 50.0) / 70.0 + 5));

            bool didEffect = false;

            foreach (var m in attacker.GetMobilesInRange(1))
            {
                if (m?.Deleted == false && m != defender && m != attacker &&
                    SpellHelper.ValidIndirectTarget(attacker, m) &&
                    m.Map == attacker.Map && m.Alive && attacker.CanSee(m) && attacker.CanBeHarmful(m) &&
                    attacker.InRange(m, weapon.MaxRange) && attacker.InLOS(m))
                {
                    attacker.DoHarmful(m, true);

                    if (Registry.TryGetValue(m, out var timer))
                    {
                        timer.Stop();
                        Registry.Remove(m);
                    }

                    timer = new FrenziedWirlwindTimer(attacker, m, amount);
                    timer.Start();
                    Registry.Add(m, timer);

                    didEffect = true;
                }
            }

            if (didEffect)
            {
                attacker.FixedEffect(0x3728, 10, 15);
                attacker.PlaySound(0x2A1);
            }

            Timer.StartTimer(TimeSpan.FromSeconds(2.0), () => RepeatEffect(attacker));
        }

        private void RepeatEffect(Mobile attacker)
        {
            attacker.FixedEffect(0x3728, 10, 15);
            attacker.PlaySound(0x2A1);
        }

        public class FrenziedWirlwindTimer : Timer
        {
            private readonly double DamagePerTick;
            private readonly Mobile m_Attacker;
            private readonly Mobile m_Defender;
            private double m_DamageRemaining;
            private double m_DamageToDo;

            public FrenziedWirlwindTimer(Mobile attacker, Mobile defender, int totalDamage)
                : base(
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(0.25),
                    12
                ) // 3 seconds at .25 seconds apart = 12.  Confirm delay in between of .25 each.
            {
                m_Attacker = attacker;
                m_Defender = defender;

                m_DamageRemaining = totalDamage;
                DamagePerTick = (double)totalDamage / 12 + 0.01;
            }

            protected override void OnTick()
            {
                if (!m_Defender.Alive || m_DamageRemaining <= 0)
                {
                    Stop();
                    Registry.Remove(m_Defender);
                    return;
                }

                m_DamageRemaining -= DamagePerTick;
                m_DamageToDo += DamagePerTick;

                if (m_DamageRemaining <= 0 && m_DamageToDo < 1)
                {
                    m_DamageToDo = 1.0; // Confirm this 'round up' at the end
                }

                var damage = (int)m_DamageToDo;

                if (damage > 0)
                {
                    m_Defender.Damage(damage, m_Attacker);
                    m_DamageToDo -= damage;
                }

                if (!m_Defender.Alive || m_DamageRemaining <= 0)
                {
                    Stop();
                    Registry.Remove(m_Defender);
                }
            }
        }
    }
}
